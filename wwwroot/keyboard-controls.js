// Gameplay letter controls use physical QWERTY positions, not the character
// produced by the active keyboard layout. Text entry remains with KNI.
(function () {
    const bindings = new Map([
        ['KeyA', 1], ['KeyD', 2], ['KeyW', 4], ['KeyS', 8],
        ['KeyX', 16], ['KeyZ', 32], ['Space', 64],
        ['ShiftLeft', 128], ['ShiftRight', 256],
        ['ArrowLeft', 512], ['ArrowRight', 1024], ['ArrowUp', 2048], ['ArrowDown', 4096]
    ]);
    const held = new Set();

    function keyCode(event) {
        // Some virtual keyboards omit code; retain a letter fallback there.
        if (event.code) return event.code;
        if (event.key === ' ') return 'Space';
        if (event.key === 'Shift') return event.location === 2 ? 'ShiftRight' : 'ShiftLeft';
        if (event.key && event.key.startsWith('Arrow')) return event.key;
        return event.key && event.key.length === 1 ? 'Key' + event.key.toUpperCase() : '';
    }
    function acceptsGameInput(event) {
        const target = event.target;
        return !target || (!target.isContentEditable
            && !['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName));
    }
    window.addEventListener('keydown', function (event) {
        const code = keyCode(event);
        if (!bindings.has(code) || !acceptsGameInput(event)
            || event.ctrlKey || event.metaKey || event.altKey) return;
        held.add(code);
        event.preventDefault();
    });
    window.addEventListener('keyup', function (event) {
        held.delete(keyCode(event));
    });
    // A release may happen outside the iframe/tab. Never leave a control held.
    window.addEventListener('blur', function () { held.clear(); });
    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState !== 'visible') held.clear();
    });

    window.warhookControls = {
        physicalKeys: function () {
            let mask = 0;
            for (const code of held) mask |= bindings.get(code);
            return mask;
        }
    };
})();
