using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
#if WEB
        // Bit order matches wwwroot/keyboard-controls.js. Replacing these keys
        // avoids AZERTY's physical W / printed Z activating movement AND beam.
        static readonly Keys[] PhysicalControlKeys = {
            Keys.A, Keys.D, Keys.W, Keys.S, Keys.X, Keys.Z, Keys.Space,
            Keys.LeftShift, Keys.RightShift, Keys.Left, Keys.Right, Keys.Up, Keys.Down,
            // The ordinary US '\\' key is OemPipe in XNA; OemBackslash is
            // the separate ISO-102 key and remains accepted directly too.
            Keys.OemPipe
        };
#endif

        KeyboardState ReadGameKeyboard()
        {
            KeyboardState keyboard = Keyboard.GetState();
#if WEB
            // Initials use letters produced by the player's layout, not the
            // physical controls used during play and menu navigation.
            if (screen == MenuScreen.Scores && enteringInitials) return keyboard;
            int physical = WebInterop.PhysicalControlKeys();
            var keys = new List<Keys>();
            foreach (Keys key in keyboard.GetPressedKeys())
                if (System.Array.IndexOf(PhysicalControlKeys, key) < 0) keys.Add(key);
            for (int i = 0; i < PhysicalControlKeys.Length; i++)
                if ((physical & (1 << i)) != 0) keys.Add(PhysicalControlKeys[i]);
            return new KeyboardState(keys.ToArray());
#else
            return keyboard;
#endif
        }
    }

#if WEB
    static partial class WebInterop
    {
        [System.Runtime.InteropServices.JavaScript.JSImport("globalThis.warhookControls.physicalKeys")]
        internal static partial int PhysicalControlKeys();
    }
#endif
}
