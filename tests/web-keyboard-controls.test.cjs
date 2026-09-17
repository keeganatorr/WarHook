// Run with: node --test tests/web-keyboard-controls.test.cjs
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

function controls() {
    const listeners = {};
    const window = { addEventListener: (name, handler) => { listeners[name] = handler; } };
    const document = { visibilityState: 'visible', addEventListener: window.addEventListener };
    vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../WarHookWeb/wwwroot/keyboard-controls.js'), 'utf8'), { window, document });
    return {
        state: window.warhookControls.physicalKeys,
        send: (name, event = {}) => listeners[name]({ preventDefault() {}, ...event }),
        document
    };
}

test('physical X/Z work with Dvorak and non-Latin characters', () => {
    const c = controls();
    c.send('keydown', { code: 'KeyX', key: 'q', keyCode: 81 });
    c.send('keydown', { code: 'KeyZ', key: 'я', keyCode: 0 });
    assert.equal(c.state(), 16 | 32);
    c.send('keyup', { code: 'KeyX', key: 'Q', keyCode: 81 });
    c.send('keyup', { code: 'KeyZ', key: 'Я', keyCode: 0 });
    assert.equal(c.state(), 0);
});

test('AZERTY physical W with a printed Z only maps to movement', () => {
    const c = controls();
    c.send('keydown', { code: 'KeyW', key: 'z', keyCode: 90 });
    assert.equal(c.state(), 4);
    c.send('keydown', { code: 'KeyZ', key: 'w', keyCode: 87 });
    assert.equal(c.state(), 4 | 32);
});

test('repeats and simultaneous keys do not lose releases', () => {
    const c = controls();
    c.send('keydown', { code: 'KeyX', key: 'x' });
    c.send('keydown', { code: 'KeyX', key: 'x', repeat: true });
    c.send('keydown', { code: 'KeyA', key: 'a' });
    c.send('keyup', { code: 'KeyX', key: 'x' });
    assert.equal(c.state(), 1);
});

test('losing focus or hiding the tab releases every physical control', () => {
    const c = controls();
    c.send('keydown', { code: 'KeyZ', key: 'z' });
    c.send('blur');
    assert.equal(c.state(), 0);
    c.send('keydown', { code: 'KeyX', key: 'x' });
    c.document.visibilityState = 'hidden';
    c.send('visibilitychange');
    assert.equal(c.state(), 0);
});

test('browser shortcuts and editable HTML fields do not trigger controls', () => {
    const c = controls();
    for (const modifier of ['ctrlKey', 'altKey', 'metaKey'])
        c.send('keydown', { code: 'KeyX', key: 'x', [modifier]: true });
    c.send('keydown', { code: 'KeyZ', key: 'z', target: { tagName: 'INPUT' } });
    c.send('keydown', { code: 'KeyZ', key: 'z', target: { isContentEditable: true } });
    assert.equal(c.state(), 0);
});

test('virtual keyboards without a code retain letter-key support', () => {
    const c = controls();
    c.send('keydown', { code: '', key: 'X' });
    assert.equal(c.state(), 16);
    c.send('keyup', { code: '', key: 'x' });
    assert.equal(c.state(), 0);
});

test('movement, fire and suction remain held through repeated events', () => {
    const c = controls();
    c.send('keydown', { code: 'ArrowRight', key: 'ArrowRight' });
    c.send('keydown', { code: 'KeyX', key: 'x' });
    c.send('keydown', { code: 'KeyZ', key: 'z' });
    for (let i = 0; i < 100; i++) {
        c.send('keydown', { code: 'KeyZ', key: 'z', repeat: true });
        assert.equal(c.state(), 1024 | 16 | 32);
    }
    c.send('keyup', { code: 'ArrowRight', key: 'ArrowRight' });
    assert.equal(c.state(), 16 | 32);
    c.send('keyup', { code: 'KeyX', key: 'x' });
    assert.equal(c.state(), 32);
    c.send('keyup', { code: 'KeyZ', key: 'z' });
    assert.equal(c.state(), 0);
});

test('Space and either Shift have independent held states', () => {
    const c = controls();
    for (const code of ['Space', 'ShiftLeft', 'ShiftRight']) c.send('keydown', { code });
    assert.equal(c.state(), 64 | 128 | 256);
    c.send('keyup', { code: 'ShiftLeft', key: 'Shift' });
    assert.equal(c.state(), 64 | 256);
    c.send('blur');
    assert.equal(c.state(), 0);
});
