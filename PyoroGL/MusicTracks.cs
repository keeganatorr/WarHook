using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace MonogameTest
{
    // Two-track music player with cross-fade. Tracks load off-thread (MP3/OGG
    // decoding is slow); MediaPlayer handles the formats, and fades are done
    // by ramping MediaPlayer.Volume on the active track.
    sealed class MusicTracks : IDisposable
    {
        public enum Track { None, Menu, Gameplay }

        // Fade durations in seconds.
        public double FadeOutSeconds = 1.2;
        public double FadeInSeconds = 1.2;

        const float TargetVolume = 0.6f;

        enum State { Loading, Playing, FadingOut, FadingIn, Stopped }
        State state = State.Loading;
        Task<(Song menu, Song gameplay)> pending;
        Song menuSong, gameplaySong;
        Track current = Track.None;
        Track requested = Track.None;
        string status = "Loading music";
        double fadeTime;

        public string Status => status;

        public MusicTracks(string assetsDir)
        {
            string menuPath = Path.Combine(assetsDir, "menu.ogg");
            string gamePath = Path.Combine(assetsDir, "gameplay.ogg");
            // Songs are decoded lazily by MediaPlayer; construct off-thread.
            pending = Task.Run(() =>
            {
                var menu = File.Exists(menuPath) ? Song.FromUri("menu", new Uri(menuPath)) : null;
                var game = File.Exists(gamePath) ? Song.FromUri("gameplay", new Uri(gamePath)) : null;
                return (menu, game);
            });
        }

        // Ask for a track; cross-fades if something else is playing.
        public void Request(Track track)
        {
            if (requested == track) return;
            requested = track;
            if (state == State.Loading) return; // applied when loading finishes
            if (current == Track.None)
            {
                // First track: start at full volume. (StartTrack(track, 0)
                // would leave it inaudible because Playing has no fade ramp.)
                StartTrack(track, TargetVolume);
                state = State.Playing;
                return;
            }
            if (track == Track.None)
            {
                state = State.FadingOut;
                fadeTime = 0;
                return;
            }
            state = State.FadingOut;
            fadeTime = 0;
        }

        public void Stop()
        {
            requested = Track.None;
            if (state == State.Loading) return;
            MediaPlayer.Stop();
            current = Track.None;
            state = requested == Track.None ? State.Stopped : State.Playing;
            if (state == State.Playing) StartTrack(requested, 0);
        }

        void StartTrack(Track track, float volume)
        {
            Song song = track == Track.Menu ? menuSong : track == Track.Gameplay ? gameplaySong : null;
            if (song == null) { current = Track.None; return; }
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = volume;
            if (MediaPlayer.State != MediaState.Playing || MediaPlayer.Queue.ActiveSong != song)
                MediaPlayer.Play(song);
            else
                MediaPlayer.Resume();
            current = track;
        }

        public void Update(double seconds)
        {
            if (state == State.Loading)
            {
                if (!pending.IsCompleted) return;
                try
                {
                    (menuSong, gameplaySong) = pending.GetAwaiter().GetResult();
                    status = "Music loaded";
                }
                catch (Exception e)
                {
                    status = "Music load failed: " + e.Message;
                    menuSong = gameplaySong = null;
                }
                state = State.Stopped;
                // Apply whatever was requested while loading.
                if (requested != Track.None) { var r = requested; requested = Track.None; Request(r); }
                return;
            }

            switch (state)
            {
                case State.FadingOut:
                    fadeTime += seconds;
                    float outLevel = MathHelper.Clamp(1 - (float)(fadeTime / FadeOutSeconds), 0, 1) * TargetVolume;
                    MediaPlayer.Volume = outLevel;
                    if (fadeTime >= FadeOutSeconds)
                    {
                        MediaPlayer.Stop();
                        if (requested == Track.None) { current = Track.None; state = State.Stopped; }
                        else { StartTrack(requested, 0); state = State.FadingIn; fadeTime = 0; }
                    }
                    break;
                case State.FadingIn:
                    fadeTime += seconds;
                    float inLevel = MathHelper.Clamp((float)(fadeTime / FadeInSeconds), 0, 1) * TargetVolume;
                    MediaPlayer.Volume = inLevel;
                    if (fadeTime >= FadeInSeconds) { MediaPlayer.Volume = TargetVolume; state = State.Playing; }
                    break;
            }
        }

        public void Dispose()
        {
            MediaPlayer.Stop();
            menuSong?.Dispose();
            gameplaySong?.Dispose();
            menuSong = gameplaySong = null;
        }
    }
}
