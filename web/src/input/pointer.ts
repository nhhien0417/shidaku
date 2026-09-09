/**
 * Pointer input, and the handful of mobile-browser defaults that would otherwise ruin a
 * drag-only game.
 *
 * Pointer Events only - never touch* and mouse* side by side, which is how you end up
 * handling the same gesture twice in an in-app WebView. Only the FIRST pointer is tracked:
 * a second finger landing mid-drag must not hijack the rectangle.
 */

export interface PointerTarget {
  /** Return true to begin a drag. False means "treat this as a tap". */
  onDown(x: number, y: number): boolean;
  onMove(x: number, y: number): void;
  onUp(x: number, y: number): void;
  /** Pointer lost (cancelled by the OS, a system gesture, the app backgrounding). */
  onCancel(): void;
  onTap(x: number, y: number): void;
}

export function attachPointer(el: HTMLElement, target: PointerTarget): () => void {
  let activeId: number | null = null;
  let dragging = false;

  const local = (e: PointerEvent): [number, number] => {
    const rect = el.getBoundingClientRect();
    return [e.clientX - rect.left, e.clientY - rect.top];
  };

  const down = (e: PointerEvent): void => {
    if (activeId !== null) return; // already tracking a finger
    activeId = e.pointerId;
    const [x, y] = local(e);
    dragging = target.onDown(x, y);
    if (!dragging) return;
    e.preventDefault();
    try {
      el.setPointerCapture(e.pointerId);
    } catch {
      /* capture is a nicety, not a requirement */
    }
  };

  const move = (e: PointerEvent): void => {
    if (e.pointerId !== activeId || !dragging) return;
    e.preventDefault();
    const [x, y] = local(e);
    target.onMove(x, y);
  };

  const up = (e: PointerEvent): void => {
    if (e.pointerId !== activeId) return;
    const [x, y] = local(e);
    activeId = null;
    if (dragging) {
      dragging = false;
      e.preventDefault();
      target.onUp(x, y);
    } else {
      target.onTap(x, y);
    }
  };

  const cancel = (e: PointerEvent): void => {
    if (e.pointerId !== activeId) return;
    activeId = null;
    if (dragging) {
      dragging = false;
      // Releasing outside the element is a normal way to finish a drag on a phone, so this
      // resolves the rectangle rather than throwing the move away.
      target.onCancel();
    }
  };

  /**
   * Last-resort release. A pointerup is not guaranteed: the OS can swallow the gesture (a
   * system edge-swipe, the notification shade, a call arriving), or the app can background
   * mid-drag. Without this the drag stays latched, activeId never clears, and every
   * subsequent touch is ignored - the game looks frozen and the session is lost. Cheap
   * insurance for the environment this actually runs in.
   */
  const forceRelease = (): void => {
    if (activeId === null && !dragging) return;
    activeId = null;
    if (dragging) {
      dragging = false;
      target.onCancel();
    }
  };

  const onVisibility = (): void => {
    if (document.visibilityState === 'hidden') forceRelease();
  };

  el.addEventListener('pointerdown', down, { passive: false });
  el.addEventListener('pointermove', move, { passive: false });
  el.addEventListener('pointerup', up, { passive: false });
  el.addEventListener('pointercancel', cancel);
  el.addEventListener('lostpointercapture', forceRelease);
  window.addEventListener('blur', forceRelease);
  document.addEventListener('visibilitychange', onVisibility);
  // A long-press context menu in the middle of a drag is pure noise here.
  const blockContext = (e: Event): void => e.preventDefault();
  el.addEventListener('contextmenu', blockContext);

  return () => {
    el.removeEventListener('pointerdown', down);
    el.removeEventListener('pointermove', move);
    el.removeEventListener('pointerup', up);
    el.removeEventListener('pointercancel', cancel);
    el.removeEventListener('lostpointercapture', forceRelease);
    window.removeEventListener('blur', forceRelease);
    document.removeEventListener('visibilitychange', onVisibility);
    el.removeEventListener('contextmenu', blockContext);
  };
}
