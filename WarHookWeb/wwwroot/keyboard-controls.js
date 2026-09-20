// Gameplay letter controls use physical QWERTY positions, not the character
// produced by the active keyboard layout. Text entry remains with KNI.
(function () {
    const bindings = new Map([
        ['KeyA', 1], ['KeyD', 2], ['KeyW', 4], ['KeyS', 8],
        ['KeyX', 16], ['KeyZ', 32], ['Space', 64],
        ['ShiftLeft', 128], ['ShiftRight', 256],
        ['ArrowLeft', 512], ['ArrowRight', 1024], ['ArrowUp', 2048], ['ArrowDown', 4096],
        ['Backslash', 8192], ['IntlBackslash', 8192],
        ['Minus', 16384], ['Equal', 32768], ['NumpadAdd', 65536]
    ]);
    const held = new Set();

    function keyCode(event) {
        // Some virtual keyboards omit code; retain a letter fallback there.
        if (event.code) return event.code;
        if (event.key === ' ') return 'Space';
        if (event.key === '-') return 'Minus';
        if (event.key === '+' || event.key === '=') return 'Equal';
        if (event.key === '\\' || event.key === '|') return 'Backslash';
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
        const code = keyCode(event);
        held.delete(code);
        // Virtual keyboards may omit code on release after reporting one of
        // the physical backslash positions on keydown.
        if (code === 'Backslash' || code === 'IntlBackslash') {
            held.delete('Backslash');
            held.delete('IntlBackslash');
        }
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
