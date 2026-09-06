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
        public enum Track { None, Menu, Gameplay1, Gameplay2, Gameplay3, Gameplay4, Gameplay5, Gameover }

        // File name for each track (index = enum value).
        static readonly string[] TrackFiles =
        {
            null, "menu.ogg", "gameplay1.ogg", "gameplay2.ogg", "gameplay3.ogg",
            "gameplay4.ogg", "gameplay5.ogg", "gameover.ogg"
        };
        static readonly int TrackCount = TrackFiles.Length;

        // Fade durations in seconds.
        public double FadeOutSeconds = 1.2;
        public double FadeInSeconds = 1.2;

        const float TargetVolume = 0.6f;

        // User music volume (0..1) from the options menu, scaled on top of
        // TargetVolume everywhere it is applied.
        float volume = 0.8f;
        public float Volume
        {
            get => volume;
            set
            {
                volume = MathHelper.Clamp(value, 0, 1);
                // Re-apply immediately so the slider affects the track that is
                // already playing, not just future starts/fades.
                if (state == State.Playing)
                    MediaPlayer.Volume = EffectiveTarget;
            }
        }
        float EffectiveTarget => TargetVolume * volume;

        enum State { Loading, Playing, FadingOut, FadingIn, Stopped }
        State state = State.Loading;
        Task<Song[]> pending;
        Song[] songs;
        Track current = Track.None;
        Track requested = Track.None;
        string status = "Loading music";
        double fadeTime;

        public string Status => status;

        public MusicTracks(string assetsDir)
        {
            // Songs are decoded lazily by MediaPlayer; construct off-thread.
            pending = Task.Run(() =>
            {
                var loaded = new Song[TrackCount];
                for (int i = 1; i < TrackCount; i++)
                {
                    string path = Path.Combine(assetsDir, TrackFiles[i]);
                    if (File.Exists(path)) loaded[i] = Song.FromUri(TrackFiles[i], new Uri(path));
                }
                return loaded;
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
                StartTrack(track, EffectiveTarget);
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

        // Rapid fade used when the tank is destroyed — much faster than the
        // normal crossfade so the music cuts out on the death hit.
        public void StartFastFadeOut()
        {
            if (state == State.Loading || current == Track.None) return;
            requested = Track.None;
            state = State.FadingOut;
            fadeTime = 0;
            FadeOutSeconds = 0.45;
        }

        void StartTrack(Track track, float volume)
        {
            int index = (int)track;
            Song song = index > 0 && index < TrackCount ? songs[index] : null;
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
                    songs = pending.GetAwaiter().GetResult();
                    status = "Music loaded";
                }
                catch (Exception e)
                {
                    status = "Music load failed: " + e.Message;
                    songs = new Song[TrackCount];
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
                    float outLevel = MathHelper.Clamp(1 - (float)(fadeTime / FadeOutSeconds), 0, 1) * EffectiveTarget;
                    MediaPlayer.Volume = outLevel;
                    if (fadeTime >= FadeOutSeconds)
                    {
                        MediaPlayer.Stop();
                        FadeOutSeconds = 1.2; // restore default for future fades
                        if (requested == Track.None) { current = Track.None; state = State.Stopped; }
                        else { StartTrack(requested, 0); state = State.FadingIn; fadeTime = 0; }
                    }
                    break;
                case State.FadingIn:
                    fadeTime += seconds;
                    float inLevel = MathHelper.Clamp((float)(fadeTime / FadeInSeconds), 0, 1) * EffectiveTarget;
                    MediaPlayer.Volume = inLevel;
                    if (fadeTime >= FadeInSeconds) { MediaPlayer.Volume = EffectiveTarget; state = State.Playing; }
                    break;
            }
        }

        public void Dispose()
        {
            MediaPlayer.Stop();
            if (songs != null) foreach (var s in songs) s?.Dispose();
            songs = null;
        }
    }
}
