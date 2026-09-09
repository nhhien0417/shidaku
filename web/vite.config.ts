import { execSync } from 'node:child_process';
import { defineConfig, type Plugin } from 'vite';

/** Short git sha, injected at compile time and attached to EVERY analytics event. */
function gitSha(): string {
  try {
    return execSync('git rev-parse --short HEAD', { encoding: 'utf8' }).trim();
  } catch {
    return 'nogit';
  }
}

/**
 * Inlines the single built CSS file into index.html as a <style> tag and drops the
 * <link>. Reason: the first-load request budget is <= 4 (html, js, catalog.json,
 * picture.json) and the subset font lives inside that CSS as a base64 data: URI, so
 * shipping CSS as its own file would cost two extra round trips on 4G.
 */
function inlineCss(): Plugin {
  return {
    name: 'shidaku-inline-css',
    enforce: 'post',
    apply: 'build',
    generateBundle(_options, bundle) {
      const cssFiles = Object.keys(bundle).filter((f) => f.endsWith('.css'));
      if (cssFiles.length === 0) return;

      let css = '';
      for (const file of cssFiles) {
        const asset = bundle[file];
        if (asset && asset.type === 'asset') {
          css += typeof asset.source === 'string' ? asset.source : Buffer.from(asset.source).toString('utf8');
          delete bundle[file];
        }
      }

      for (const file of Object.keys(bundle)) {
        if (!file.endsWith('.html')) continue;
        const asset = bundle[file];
        if (!asset || asset.type !== 'asset') continue;
        let html = typeof asset.source === 'string' ? asset.source : Buffer.from(asset.source).toString('utf8');
        html = html.replace(/<link[^>]+rel="stylesheet"[^>]*>/g, '');
        html = html.replace('</head>', `<style>${css}</style></head>`);
        asset.source = html;
      }
    },
  };
}

export default defineConfig(({ mode }) => ({
  root: '.',
  base: '/',
  publicDir: 'public',
  plugins: [inlineCss()],
  define: {
    __BUILD_VER__: JSON.stringify(gitSha()),
    // Which puzzle set the build is allowed to load. "internal" contains third-party
    // copyrighted characters (see docs/Plan-Web-Prototype-Shidaku.md B.11) and must never
    // reach a public build - check-budget.mjs fails the build if a production bundle
    // declares it.
    __CONTENT_SET__: JSON.stringify(process.env.CONTENT_SET ?? (mode === 'production' ? 'safe' : 'internal')),
  },
  build: {
    target: 'es2019',
    outDir: 'dist',
    emptyOutDir: true,
    assetsInlineLimit: 40 * 1024, // inline the subset font (~18KB) rather than fetch it
    cssCodeSplit: false,
    cssMinify: true,
    minify: 'esbuild',
    sourcemap: false,
    reportCompressedSize: true,
    rollupOptions: {
      output: {
        // One JS chunk. Code splitting would only add round trips on a page this small.
        manualChunks: undefined,
        inlineDynamicImports: true,
        entryFileNames: 'assets/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash][extname]',
      },
    },
  },
  server: { port: 5173, host: true },
  preview: { port: 4173, host: true },
}));
