using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;

namespace MonogameTest
{
    // Trim-safe JSON contract for beam-audio.json (source generated at build).
    [JsonSerializable(typeof(BeamAudio.Settings))]
    internal sealed partial class BeamAudioJsonContext : JsonSerializerContext
    {
    }

    // PCM is generated off the game thread; only audio-device operations run on it.
    sealed class BeamAudio : IDisposable
    {
        public sealed class Voice
        {
            public double Duration { get; set; } = 6;
            public double Gain { get; set; } = 0.5;
            public double StartHz { get; set; } = 220;
            public double SweepOctaves { get; set; } = 1.5;
            public double Attack { get; set; } = 0.025;
            public double Release { get; set; } = 0.04;
            public double PullRise { get; set; } = 0.12;
            public double RampDecay { get; set; } = 1.5;
        }
        public sealed class Settings
        {
            public double MasterVolume { get; set; } = 0.65;
            public int SampleRate { get; set; } = 44100;
            public double SubHz { get; set; } = 45;
            public double SubLevel { get; set; } = 0.4;
            public double SubHarmonicHz { get; set; } = 90.3;
            public double SubHarmonicLevel { get; set; } = 0.2;
            public double TremoloHz { get; set; } = 1.7;
            public double TremoloDepth { get; set; } = 0.3;
            public double VibratoHz { get; set; } = 9;
            public double VibratoDepth { get; set; } = 0.03;
            public double BeamLevel { get; set; } = 0.5;
            public double SecondHarmonic { get; set; } = 0.5;
            public double ThirdHarmonic { get; set; } = 0.25;
            public double ShimmerRatio { get; set; } = 4.02;
            public double ShimmerLevel { get; set; } = 0.12;
            public double ShimmerHz { get; set; } = 13;
            public double NoiseLevel { get; set; } = 0.08;
            public double TransitionSeconds { get; set; } = 0.035;
            public Voice Fire { get; set; } = new Voice { Duration=.16, Gain=.7, StartHz=110, SweepOctaves=1, Attack=.008, Release=.07, PullRise=.015 };
            public Voice Extending { get; set; } = new Voice();
            public Voice Catch { get; set; } = new Voice { Duration=.2, Gain=.8, StartHz=660, SweepOctaves=-.8, Attack=.005, Release=.12, PullRise=.01, RampDecay=.5 };
            public Voice Returning { get; set; } = new Voice { Duration=.65, Gain=.55, StartHz=440, SweepOctaves=-1.5, Attack=.015, Release=.05, PullRise=.02, RampDecay=.3 };
            // Mega Man-style item pickup: quick bright two-note arpeggio.
            public double MenuGain { get; set; } = 0.5;
            public double MenuFirstHz { get; set; } = 1318.5;  // E6
            public double MenuSecondHz { get; set; } = 1975.5; // B6
            public double MenuNoteSeconds { get; set; } = 0.06;
            public double MenuTailSeconds { get; set; } = 0.05;
            // Confirm sound: rising "ba-LEAP" — low note sweeping up to a held
            // high note, slightly longer than the navigation blip.
            public double ConfirmGain { get; set; } = 0.55;
            public double ConfirmLowHz { get; set; } = 659.3;   // E5
            public double ConfirmHighHz { get; set; } = 1975.5; // B6
            public double ConfirmSweepSeconds { get; set; } = 0.07;
            public double ConfirmHoldSeconds { get; set; } = 0.09;
            public double ConfirmTailSeconds { get; set; } = 0.08;
            // Synth explosion: filtered noise burst with a downward pitch
            // sweep and a sub-bass thump, like a retro console boom.
            public double ExplosionGain { get; set; } = 0.7;
            public double ExplosionSeconds { get; set; } = 0.35;
            public double ExplosionStartHz { get; set; } = 900;   // noise band top
            public double ExplosionEndHz { get; set; } = 60;     // noise band bottom
            public double ExplosionThumpHz { get; set; } = 55;   // sub thump pitch
            public double ExplosionThumpLevel { get; set; } = 0.9;
            public double ExplosionNoiseLevel { get; set; } = 1.0;
            public double ExplosionCrackleLevel { get; set; } = 0.35;
            // Parachute drop: a friendly descending "bwoop" with a soft
            // airy whoosh — no menace, reads as a supply drop.
            public double ParachuteGain { get; set; } = 0.5;
            public double ParachuteSeconds { get; set; } = 0.5;
            public double ParachuteStartHz { get; set; } = 700;
            public double ParachuteEndHz { get; set; } = 180;
            public double ParachuteWhooshLevel { get; set; } = 0.25;
            // Tank movement: a low continuous rumble loop played while the
            // tank drives (treads + engine drone).
            public double TankMoveGain { get; set; } = 0.45;
            public double TankMoveSeconds { get; set; } = 0.8;
            public double TankMoveHz { get; set; } = 42;
            public double TankMoveRumbleHz { get; set; } = 11;
            public double TankMoveRumbleLevel { get; set; } = 0.7;
        }
        readonly string path;
        Task<(Settings settings, byte[][] pcm)> pending;
        Settings settings;
        SoundEffect[] sounds;
        SoundEffectInstance[] voices;
        bool wasActive, wasCaught, suspended;
        static readonly int[] LoopIndices = { 1, 3 };
        Voice[] specs;
        // User sound-effect volume (0..1) from the options menu, multiplied
        // into every voice's playback volume on top of MasterVolume.
        public float VolumeScale { get; set; } = 1f;
        public string Status { get; private set; } = "Loading";
        public BeamAudio(string path) { this.path = path; Reload(); }
        public void Reload()
        {
            if (pending != null) return;
            pending = Task.Run(() =>
            {
                var config = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path),
                    new BeamAudioJsonContext(
                        new JsonSerializerOptions { ReadCommentHandling=JsonCommentHandling.Skip, AllowTrailingCommas=true }).Settings)
                    ?? throw new InvalidDataException("Empty beam audio settings.");
                Validate(config);
                var specs = new[] { config.Fire, config.Extending, config.Catch, config.Returning };
                var pcm = new byte[9][];
                for (int i=0; i<4; i++) pcm[i] = Generate(config, specs[i]);
                pcm[4] = GenerateMenuBlip(config);
                pcm[5] = GenerateConfirm(config);
                pcm[6] = GenerateExplosion(config);
                pcm[7] = GenerateParachute(config);
                pcm[8] = GenerateTankMove(config);
                return (config, pcm);
            });
        }
        static void Validate(Settings s)
        {
            if (s.SampleRate != 44100 && s.SampleRate != 48000) throw new InvalidDataException("SampleRate must be 44100 or 48000.");
            foreach (object o in new object[] { s, s.Fire, s.Extending, s.Catch, s.Returning })
            {
                if (o == null) throw new InvalidDataException("Each audio phase needs settings.");
                foreach (var p in o.GetType().GetProperties())
                    if (p.PropertyType == typeof(double) && !double.IsFinite((double)p.GetValue(o)))
                        throw new InvalidDataException(p.Name + " must be finite.");
            }
            if (s.MasterVolume < 0 || s.MasterVolume > 1 || s.TransitionSeconds < .005 || s.TransitionSeconds > 1)
                throw new InvalidDataException("MasterVolume: 0–1; TransitionSeconds: 0.005–1.");
            foreach (var v in new[] { s.Fire, s.Extending, s.Catch, s.Returning })
                if (v.Duration < .02 || v.Duration > 30 || v.Gain < 0 || v.Gain > 1 ||
                    v.StartHz < 1 || v.StartHz > 5000 || Math.Abs(v.SweepOctaves)>5 ||
                    v.Attack < 0 || v.Release < 0 || v.PullRise < 0 || v.RampDecay < 0 || v.RampDecay>10)
                    throw new InvalidDataException("Invalid phase duration, gain, frequency, sweep, or envelope.");
            if (s.ExplosionSeconds < .05 || s.ExplosionSeconds > 3 || s.ExplosionGain < 0 || s.ExplosionGain > 1 ||
                s.ExplosionEndHz < 1 || s.ExplosionEndHz > s.ExplosionStartHz || s.ExplosionStartHz > 8000 ||
                s.ExplosionThumpHz < 1 || s.ExplosionThumpHz > 500 ||
                s.ExplosionThumpLevel < 0 || s.ExplosionThumpLevel > 2 ||
                s.ExplosionNoiseLevel < 0 || s.ExplosionNoiseLevel > 2 ||
                s.ExplosionCrackleLevel < 0 || s.ExplosionCrackleLevel > 2)
                throw new InvalidDataException("Invalid explosion settings.");
            if (s.ParachuteSeconds < .05 || s.ParachuteSeconds > 2 || s.ParachuteGain < 0 || s.ParachuteGain > 1 ||
                s.ParachuteEndHz < 1 || s.ParachuteEndHz > s.ParachuteStartHz || s.ParachuteStartHz > 4000 ||
                s.ParachuteWhooshLevel < 0 || s.ParachuteWhooshLevel > 2)
                throw new InvalidDataException("Invalid parachute settings.");
            if (s.TankMoveSeconds < .2 || s.TankMoveSeconds > 4 || s.TankMoveGain < 0 || s.TankMoveGain > 1 ||
                s.TankMoveHz < 1 || s.TankMoveHz > 200 || s.TankMoveRumbleHz < 1 || s.TankMoveRumbleHz > 100 ||
                s.TankMoveRumbleLevel < 0 || s.TankMoveRumbleLevel > 2)
                throw new InvalidDataException("Invalid tank movement settings.");
        }
        internal static byte[] Generate(Settings s, Voice v)
        {
            int count = (int)(s.SampleRate*v.Duration);
            var values = new double[count];
            var random = new Random(715);
            double phase=0, shimmerPhase=0, previousNoise=0, peak=0;
            double sine(double cycles) => Math.Sin(2*Math.PI*cycles);
            for (int i=0; i<count; i++)
            {
                double t=(double)i/s.SampleRate, pos=t/v.Duration;
                double frequency=v.StartHz*Math.Pow(2,v.SweepOctaves*pos);
                phase += Math.Clamp(frequency*(1+s.VibratoDepth*sine(s.VibratoHz*t)), 1, s.SampleRate/8.0)/s.SampleRate;
                shimmerPhase += Math.Clamp(frequency*s.ShimmerRatio, 1, s.SampleRate*.45)/s.SampleRate;
                double ramp=Math.Pow(1-pos,v.RampDecay);
                double sub=(sine(s.SubHz*t)*s.SubLevel+sine(s.SubHarmonicHz*t)*s.SubHarmonicLevel)
                    *(1-s.TremoloDepth+s.TremoloDepth*sine(s.TremoloHz*t));
                double beam=(sine(phase)+s.SecondHarmonic*Math.Sin(4*Math.PI*phase+.7)
                    +s.ThirdHarmonic*Math.Sin(6*Math.PI*phase+1.3))*s.BeamLevel*ramp;
                double shimmer=sine(shimmerPhase)*s.ShimmerLevel*(.6+.4*sine(s.ShimmerHz*t))*ramp;
                double noise=Math.Sqrt(-2*Math.Log(Math.Max(1e-12,random.NextDouble())))*sine(random.NextDouble());
                double crackle=(noise-previousNoise)/8*s.NoiseLevel*ramp;
                previousNoise=noise;
                double env=Math.Min(1,t/Math.Max(.001,v.Attack))*Math.Min(1,(count-1-i)/(s.SampleRate*Math.Max(.001,v.Release)));
                double value=(sub+(beam+shimmer+crackle)*Math.Min(1,t/Math.Max(.001,v.PullRise)))*env;
                if (!double.IsFinite(value)) throw new InvalidDataException("Audio settings produced non-finite samples.");
                values[i]=value; peak=Math.Max(peak,Math.Abs(value));
            }
            var pcm=new byte[count*2];
            for (int i=0; i<count; i++)
            {
                short sample=(short)(values[i]/Math.Max(peak,1e-9)*.9*32767);
                pcm[i*2]=(byte)(sample & 255); pcm[i*2+1]=(byte)((sample >> 8)&255);
            }
            return pcm;
        }
        // Mega Man-style item pickup: two quick bright square-wave notes
        // (E6 -> B6) with a short decay tail. Mono 16-bit PCM like the rest.
        internal static byte[] GenerateMenuBlip(Settings s)
        {
            int rate=s.SampleRate;
            int noteLen=(int)(rate*s.MenuNoteSeconds);
            int tailLen=(int)(rate*s.MenuTailSeconds);
            int count=noteLen*2+tailLen;
            var pcm=new byte[count*2];
            for (int i=0;i<count;i++)
            {
                double hz = i < noteLen ? s.MenuFirstHz : s.MenuSecondHz;
                double t=(double)i/rate;
                // Square wave, softened with a simple 2-term harmonic sum.
                double wave=Math.Sign(Math.Sin(2*Math.PI*hz*t));
                wave=wave*0.7+0.3*Math.Sin(4*Math.PI*hz*t);
                // Envelope: fast attack, exp decay within each note, quick tail fade.
                double env;
                if (i < noteLen) env=Math.Min(1,t/0.002)*Math.Exp(-6.0*(t%s.MenuNoteSeconds)/s.MenuNoteSeconds);
                else env=Math.Exp(-9.0*((double)(i-noteLen))/tailLen);
                short sample=(short)(Math.Clamp(wave*env,-1,1)*s.MenuGain*.9*32767);
                pcm[i*2]=(byte)(sample & 255); pcm[i*2+1]=(byte)((sample >> 8)&255);
            }
            return pcm;
        }
        // Confirm "ba-LEAP": a low note that sweeps up into a held, slightly
        // brighter high note — reads as a positive "choose this" chirp.
        internal static byte[] GenerateConfirm(Settings s)
        {
            int rate=s.SampleRate;
            int sweepLen=(int)(rate*s.ConfirmSweepSeconds);
            int holdLen=(int)(rate*s.ConfirmHoldSeconds);
            int tailLen=(int)(rate*s.ConfirmTailSeconds);
            int count=sweepLen+holdLen+tailLen;
            var pcm=new byte[count*2];
            double phase=0;
            for (int i=0;i<count;i++)
            {
                double t=(double)i/rate;
                // Frequency: low note, exponential glide to high during sweep,
                // then hold. Exponential sounds natural for pitch jumps.
                double hz;
                if (i < sweepLen)
                {
                    double k=(double)i/sweepLen;
                    hz=s.ConfirmLowHz*Math.Pow(s.ConfirmHighHz/s.ConfirmLowHz, k);
                }
                else hz=s.ConfirmHighHz;
                phase+=hz/rate;
                // Square-ish timbre softened with harmonics, like the blip.
                double wave=Math.Sign(Math.Sin(2*Math.PI*phase));
                wave=wave*0.65+0.35*Math.Sin(4*Math.PI*phase);
                // Envelope: quick attack through the sweep, full during hold,
                // exponential tail.
                double env;
                if (i < sweepLen) env=Math.Min(1,t/0.002)*(0.55+0.45*((double)i/sweepLen));
                else if (i < sweepLen+holdLen) env=1;
                else env=Math.Exp(-7.0*((double)(i-sweepLen-holdLen))/tailLen);
                short sample=(short)(Math.Clamp(wave*env,-1,1)*s.ConfirmGain*.9*32767);
                pcm[i*2]=(byte)(sample & 255); pcm[i*2+1]=(byte)((sample >> 8)&255);
            }
            return pcm;
        }
        // Synth explosion (index 6): a burst of noise whose band sweeps down
        // from ExplosionStartHz to ExplosionEndHz, layered over a decaying sub
        // thump and a crackle tail. Mono 16-bit PCM like the rest.
        internal static byte[] GenerateExplosion(Settings s)
        {
            int rate=s.SampleRate;
            int count=(int)(rate*s.ExplosionSeconds);
            var pcm=new byte[count*2];
            var random=new Random(9176);
            double phase=0;
            double previousNoise=0;
            for (int i=0;i<count;i++)
            {
                double t=(double)i/rate, pos=t/s.ExplosionSeconds;
                // Noise band centre sweeps down exponentially over the burst.
                double hz=s.ExplosionStartHz*Math.Pow(s.ExplosionEndHz/s.ExplosionStartHz, Math.Min(1, pos*1.6));
                // Ring-modulated noise reads as a pitched boom rather than hiss.
                double noise=random.NextDouble()*2-1;
                double boom=noise*Math.Sin(2*Math.PI*hz*t);
                // Crackle: differentiated noise, harsher early in the burst.
                double crackle=(noise-previousNoise)*s.ExplosionCrackleLevel*(1-pos);
                previousNoise=noise;
                // Sub thump: exponential pitch drop, fast decay.
                double thumpHz=s.ExplosionThumpHz*(1+2.5*Math.Exp(-t*30));
                phase+=thumpHz/rate;
                double thump=Math.Sin(2*Math.PI*phase)*Math.Exp(-t*9)*s.ExplosionThumpLevel;
                // Envelope: instant attack, exponential decay over the burst.
                double env=Math.Exp(-3.5*pos);
                double value=(boom*s.ExplosionNoiseLevel+crackle)*env+thump*env;
                short sample=(short)(Math.Clamp(value,-1,1)*s.ExplosionGain*.9*32767);
                pcm[i*2]=(byte)(sample & 255); pcm[i*2+1]=(byte)((sample >> 8)&255);
            }
            return pcm;
        }
        // Parachute drop (index 7): friendly descending tone with an airy
        // noise whoosh. Mono 16-bit PCM like the rest.
        internal static byte[] GenerateParachute(Settings s)
        {
            int rate=s.SampleRate;
            int count=(int)(rate*s.ParachuteSeconds);
            var pcm=new byte[count*2];
            var random=new Random(4242);
            double phase=0;
            double previousNoise=0;
            for (int i=0;i<count;i++)
            {
                double t=(double)i/rate, pos=t/s.ParachuteSeconds;
                // Tone glides gently down like a soft "bwoop".
                double hz=s.ParachuteStartHz*Math.Pow(s.ParachuteEndHz/s.ParachuteStartHz, pos);
                phase+=hz/rate;
                // Soft sine (no harsh harmonics — friendly).
                double tone=Math.Sin(2*Math.PI*phase);
                // Airy whoosh: smoothed noise, swells then fades.
                double noise=random.NextDouble()*2-1;
                double whoosh=(noise+previousNoise)*0.5*s.ParachuteWhooshLevel*Math.Sin(Math.PI*pos);
                previousNoise=noise;
                // Envelope: quick soft attack, smooth decay.
                double env=Math.Min(1,t/0.03)*Math.Exp(-2.2*pos);
                double value=(tone+whoosh)*env;
                short sample=(short)(Math.Clamp(value,-1,1)*s.ParachuteGain*.9*32767);
                pcm[i*2]=(byte)(sample & 255); pcm[i*2+1]=(byte)((sample >> 8)&255);
            }
            return pcm;
        }
        // Tank movement (index 8): low engine drone + tread rumble loop.
        internal static byte[] GenerateTankMove(Settings s)
        {
            int rate=s.SampleRate;
            int count=(int)(rate*s.TankMoveSeconds);
            var pcm=new byte[count*2];
            var random=new Random(8642);
            double previousNoise=0;
            double peak=0;
            var values=new double[count];
            for (int i=0;i<count;i++)
            {
                double t=(double)i/rate, pos=t/s.TankMoveSeconds;
                // Engine: low sine whose pitch wobbles slowly (idling drone).
                double hz=s.TankMoveHz*(1+0.08*Math.Sin(2*Math.PI*s.TankMoveRumbleHz*t));
                double engine=Math.Sin(2*Math.PI*hz*t);
                // Treads: low-passed noise bumping at the rumble rate.
                double noise=random.NextDouble()*2-1;
                double smoothed=(noise+previousNoise)*0.5;
                previousNoise=noise;
                double tread=smoothed*s.TankMoveRumbleLevel*(0.6+0.4*Math.Sin(2*Math.PI*s.TankMoveRumbleHz*t));
                double value=(engine+tread)*0.5;
                values[i]=value; peak=Math.Max(peak,Math.Abs(value));
            }
            // Crossfade the loop ends so it repeats without a click.
            int fade=(int)(rate*0.05);
            for (int i=0;i<count;i++)
            {
                double value=values[i]/Math.Max(peak,1e-9)*.9;
                if (i<fade) value=value*(double)i/fade+values[count-1-(fade-1-i)]/Math.Max(peak,1e-9)*.9*(double)(fade-1-i)/fade;
                short sample=(short)(Math.Clamp(value,-1,1)*s.TankMoveGain*32767);
                pcm[i*2]=(byte)(sample & 255); pcm[i*2+1]=(byte)((sample >> 8)&255);
            }
            return pcm;
        }
        // Fire the parachute drop (index 7). Safe to call before audio loads.
        public void PlayParachute()
        {
            if (voices == null || voices.Length < 8 || voices[7] == null) return;
            var v=voices[7];
            v.Stop();
            v.Volume=(float)Math.Clamp(settings.MasterVolume*settings.ParachuteGain*VolumeScale,0,1);
            v.Play();
        }
        // Tank movement loop (index 8). Start/stop from the movement code.
        public void SetTankMove(bool moving)
        {
            if (voices == null || voices.Length < 9 || voices[8] == null) return;
            var v=voices[8];
            if (moving)
            {
                if (v.State != SoundState.Playing)
                {
                    v.IsLooped=true;
                    v.Volume=(float)Math.Clamp(settings.MasterVolume*settings.TankMoveGain*VolumeScale,0,1);
                    v.Play();
                }
            }
            else if (v.State == SoundState.Playing) v.Stop();
        }
        // Fire the menu blip (index 4). Safe to call before audio finishes loading.
        public void PlayMenuBlip()
        {
            if (voices == null || voices.Length < 6 || voices[4] == null) return;
            var v=voices[4];
            v.Stop();
            v.Volume=(float)Math.Clamp(settings.MenuGain*VolumeScale,0,1);
            v.Play();
        }
        // Fire the synth explosion (index 6). Safe to call before audio loads.
        public void PlayExplosion()
        {
            if (voices == null || voices.Length < 7 || voices[6] == null) return;
            var v=voices[6];
            v.Stop();
            v.Volume=(float)Math.Clamp(settings.MasterVolume*settings.ExplosionGain*VolumeScale,0,1);
            v.Play();
        }
        // Fire the confirm "ba-LEAP" (index 5). Safe to call before audio loads.
        public void PlayMenuConfirm()
        {
            if (voices == null || voices.Length < 6 || voices[5] == null) return;
            var v=voices[5];
            v.Stop();
            v.Volume=(float)Math.Clamp(settings.ConfirmGain*VolumeScale,0,1);
            v.Play();
        }
        void PollReload()
        {
            if (pending == null || !pending.IsCompleted) return;
            var replacements=new SoundEffect[9];
            var instances=new SoundEffectInstance[9];
            try
            {
                var result=pending.GetAwaiter().GetResult();
                for (int i=0;i<replacements.Length;i++)
                {
                    replacements[i]=new SoundEffect(result.pcm[i],result.settings.SampleRate,AudioChannels.Mono);
                    instances[i]=replacements[i].CreateInstance();
                    instances[i].IsLooped=i==1 || i==3 || i==8;
                    instances[i].Volume=0;
                }
                sounds=replacements; voices=instances; settings=result.settings;
                specs=new[] { settings.Fire,settings.Extending,settings.Catch,settings.Returning };
                wasActive=wasCaught=suspended=false;
                Status="Loaded " + path;
            }
            catch (Exception e)
            {
                foreach (var instance in instances) instance?.Dispose();
                foreach (var sound in replacements) sound?.Dispose();
                Status="Beam audio reload failed: " + e.Message;
            }
            pending=null;
            Console.WriteLine(Status);
        }
        public void Update(bool active, bool returning, bool caught, bool paused, double seconds)
        {
            PollReload();
            if (voices == null) return;
            if (paused)
            {
                if (!suspended) foreach (var v in voices) if (v!=null && v.State==SoundState.Playing) v.Pause();
                suspended=true; return;
            }
            if (suspended)
            {
                foreach (var v in voices) if (v!=null && v.State==SoundState.Paused) v.Resume();
                suspended=false;
            }
            void pulse(int index)
            {
                voices[index].Stop(); voices[index].Volume=(float)(settings.MasterVolume*specs[index].Gain*VolumeScale); voices[index].Play();
            }
            if (active && !wasActive) pulse(0);
            if (active && caught && !wasCaught) pulse(2);
            int desiredLoop=active ? (returning ? 3 : 1) : -1;
            foreach (int i in LoopIndices)
            {
                float target=i==desiredLoop ? (float)(settings.MasterVolume*specs[i].Gain*VolumeScale) : 0;
                float step=(float)(seconds/settings.TransitionSeconds);
                voices[i].Volume=Math.Clamp(voices[i].Volume+Math.Clamp(target-voices[i].Volume,-step,step),0,1);
                if (target>0 && voices[i].State==SoundState.Stopped) voices[i].Play();
                if (target==0 && voices[i].Volume==0) voices[i].Stop();
            }
            wasActive=active; wasCaught=active && caught;
        }
        public void Stop()
        {
            if (voices != null) foreach (var voice in voices) { if (voice==null) continue; voice.Stop(); voice.Volume=0; }
            wasActive=wasCaught=suspended=false;
        }
        // Stop only the beam voices (0..3), leaving the menu blip (4) alone so
        // menu sounds can play while the beam loop is silenced on menus.
        public void StopBeamVoices()
        {
            if (voices == null) return;
            for (int i=0;i<4 && i<voices.Length;i++) { voices[i].Stop(); voices[i].Volume=0; }
            wasActive=wasCaught=suspended=false;
        }        void DisposeVoices()
        {
            Stop();
            if (voices != null) foreach (var v in voices) v.Dispose();
            if (sounds != null) foreach (var s in sounds) s.Dispose();
        }
        public void Dispose() => DisposeVoices();
    }
}
