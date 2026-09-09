/** Minimal typed event emitter. ~20 lines beats a dependency. */
export class Emitter<E extends object> {
  private handlers: { [K in keyof E]?: Array<(payload: E[K]) => void> } = {};

  on<K extends keyof E>(event: K, handler: (payload: E[K]) => void): () => void {
    const list = (this.handlers[event] ??= []);
    list.push(handler);
    return () => {
      const i = list.indexOf(handler);
      if (i >= 0) list.splice(i, 1);
    };
  }

  emit<K extends keyof E>(event: K, payload: E[K]): void {
    const list = this.handlers[event];
    if (!list) return;
    // Copy: a handler is allowed to unsubscribe itself.
    for (const handler of list.slice()) {
      try {
        handler(payload);
      } catch (err) {
        // A broken listener (analytics, audio) must never break the game loop.
        console.error(`[emitter] handler for "${String(event)}" threw`, err);
      }
    }
  }
}
