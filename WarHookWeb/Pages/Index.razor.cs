using System;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;
using MonogameTest;

namespace WarHookWeb.Pages
{
    public partial class Index
    {
        Game _game;
        bool _audioUnlocked;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public void TickDotNet()
        {
            // init game
            if (_game == null)
            {
                _game = new Game1();
                _game.Run();
                if (_audioUnlocked)
                    (_game as Game1).UnlockAudio();
            }

            // run gameloop
            _game.Tick();
        }

        // The browser blocks audio until a user gesture. index.html resumes
        // the WebAudio context on the first click/keypress and notifies us so
        // the active music track can be restarted.
        [JSInvokable]
        public void UnlockAudioDotNet()
        {
            _audioUnlocked = true;
            (_game as Game1)?.UnlockAudio();
        }
    }
}
