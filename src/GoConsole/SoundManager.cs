using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole;

public static class SoundManager
{
    private static string? _dir;
    private static readonly Dictionary<(string, int), string> _volCache = new();

    public static bool Enabled { get; set; } = true;
    public static double Volume { get; set; } = 0.75;

    public static void Initialize(InitConfig config)
    {
        try
        {
            Enabled = SettingsStore.GetBool("sound.enabled", config.Sound.Enabled);
            var vol = SettingsStore.GetInt("sound.volume", config.Sound.Volume);
            Volume = Math.Clamp(vol / 100.0, 0.0, 1.0);
            _dir = ConfigReader.ResolvePath("system\\sounds");
            Directory.CreateDirectory(_dir);
            GenerateAll();
        }
        catch (Exception ex)
        {
            Logger.Warn($"Sound init: {ex.Message}");
        }
    }

    public static void Play(string name)
    {
        if (!Enabled || Volume <= 0.01 || _dir == null) return;
        try
        {
            var src = Path.Combine(_dir, name + ".wav");
            if (!File.Exists(src)) return;

            var q = (int)(Volume * 10);
            if (q <= 0) return;
            if (!_volCache.TryGetValue((name, q), out var path) || !File.Exists(path))
            {
                path = Path.Combine(_dir, "cache", $"{name}_{q}.wav");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                ScaleWav(src, path, q / 10.0);
                _volCache[(name, q)] = path;
            }
            new SoundPlayer(path).Play();
        }
        catch
        {
            // audio failures are never fatal
        }
    }

    private static void ScaleWav(string src, string dst, double factor)
    {
        var bytes = File.ReadAllBytes(src);
        var header = new byte[44];
        Array.Copy(bytes, header, 44);
        var outBuf = new byte[bytes.Length];
        Array.Copy(header, outBuf, 44);
        for (int i = 44; i + 1 < bytes.Length; i += 2)
        {
            var sample = (short)(bytes[i] | (bytes[i + 1] << 8));
            sample = (short)(sample * factor);
            outBuf[i] = (byte)(sample & 0xFF);
            outBuf[i + 1] = (byte)((sample >> 8) & 0xFF);
        }
        File.WriteAllBytes(dst, outBuf);
    }

    // Mixed Xbox Series X + PlayStation 5 soundset. Each cue layers an Xbox-style
    // crisp electronic element with a PS5-style soft, airy, reverbed layer.
    private static void GenerateAll()
    {
        Ensure("boot", Make(1100, m =>
        {
            m.Tone(220, 750, 0.26, Wave.Saw, 0, 200, 260, 3);
            m.Tone(440, 550, 0.12, Wave.Saw, 0, 150, 190, 2);
            m.Pluck(523.25, 200, 0.22, 200);
            m.Pluck(783.99, 280, 0.20, 360);
            m.Echo(0.22, 44, 3);
        }));
        Ensure("nav", Make(110, m =>
        {
            m.Tone(880, 20, 0.13, Wave.Square, 0, 2, 6);
            m.Pluck(1046.5, 45, 0.11, 0);
            m.Noise(14, 0.02, 8000, 0);
        }));
        Ensure("select", Make(240, m =>
        {
            m.Pluck(1046.5, 110, 0.30, 0);
            m.Tone(740, 30, 0.16, Wave.Square, 8, 2, 8);
            m.Pluck(1567.98, 70, 0.10, 12);
            m.Noise(25, 0.02, 6000, 0);
            m.Echo(0.22, 34);
        }));
        Ensure("back", Make(210, m =>
        {
            m.Slide(587.33, 349.23, 90, 0.26, Wave.Sine, 0, 4, 20);
            m.Tone(392, 24, 0.16, Wave.Square, 30, 2, 8);
            m.Pluck(523.25, 60, 0.12, 6);
            m.Echo(0.2, 30);
        }));
        Ensure("error", Make(370, m =>
        {
            m.Slide(220, 110, 260, 0.32, Wave.Saw, 0, 8, 60);
            m.Tone(196, 260, 0.18, Wave.Sine, 0, 10, 70, 6);
            m.Slide(440, 220, 200, 0.10, Wave.Square, 30, 5, 40);
            m.Echo(0.18, 40);
        }));
        Ensure("notify", Make(430, m =>
        {
            m.Pluck(987.77, 120, 0.30, 0);
            m.Pluck(1318.51, 170, 0.30, 110);
            m.Tone(1567.98, 26, 0.16, Wave.Square, 0, 2, 6);
            m.Noise(20, 0.02, 7000, 0);
            m.Echo(0.24, 40, 3);
        }));
        Ensure("toggle", Make(190, m =>
        {
            m.Slide(440, 880, 80, 0.28, Wave.Sine, 0, 4, 30);
            m.Tone(987.77, 26, 0.18, Wave.Square, 70, 2, 7);
            m.Pluck(1318.51, 60, 0.12, 20);
        }));
        Ensure("key", Make(50, m =>
        {
            m.Noise(10, 0.07, 5000, 0, 1, 4);
            m.Tone(1500, 12, 0.06, Wave.Square, 2, 1, 5);
        }));
        Ensure("launch", Make(1500, m =>
        {
            m.Tone(220, 900, 0.28, Wave.Saw, 0, 260, 300, 3);
            m.Tone(440, 700, 0.14, Wave.Saw, 0, 200, 220, 2);
            m.Pluck(523.25, 240, 0.22, 180);
            m.Pluck(659.25, 260, 0.22, 300);
            m.Pluck(783.99, 300, 0.22, 420);
            m.Pluck(1046.5, 340, 0.18, 540);
            m.Noise(300, 0.06, 3000, 100, 120, 180);
            m.Echo(0.22, 45, 3);
        }));
        Ensure("install", Make(520, m =>
        {
            m.Pluck(523.25, 120, 0.26, 0);
            m.Pluck(659.25, 140, 0.26, 110);
            m.Pluck(783.99, 200, 0.28, 220);
            m.Tone(1567.98, 30, 0.18, Wave.Square, 320, 2, 7);
            m.Echo(0.22, 38, 2);
        }));
        Ensure("uninstall", Make(540, m =>
        {
            m.Pluck(783.99, 150, 0.26, 0);
            m.Pluck(659.25, 160, 0.26, 120);
            m.Pluck(523.25, 220, 0.24, 240);
            m.Echo(0.2, 40, 2);
        }));
        Ensure("success", Make(440, m =>
        {
            m.Pluck(783.99, 120, 0.26, 0);
            m.Pluck(987.77, 200, 0.28, 110);
            m.Tone(1318.51, 40, 0.16, Wave.Square, 200, 2, 8);
            m.Echo(0.24, 38, 3);
        }));
        Ensure("screenshot", Make(150, m =>
        {
            m.Noise(8, 0.30, 9000, 0, 1, 3);
            m.Tone(120, 30, 0.20, Wave.Sine, 0, 2, 20);
            m.Noise(8, 0.30, 9000, 60, 1, 3);
            m.Tone(160, 24, 0.12, Wave.Square, 58, 2, 8);
        }));
        Ensure("achievement", Make(900, m =>
        {
            m.Pluck(659.25, 160, 0.28, 0);
            m.Pluck(783.99, 180, 0.28, 120);
            m.Pluck(987.77, 220, 0.30, 240);
            m.Pluck(1318.51, 260, 0.22, 380);
            m.Tone(1567.98, 60, 0.14, Wave.Square, 360, 3, 10);
            m.Echo(0.24, 42, 3);
        }));
    }

    private static void Ensure(string name, short[] samples)
    {
        var path = Path.Combine(_dir!, name + ".wav");
        if (File.Exists(path)) return;
        try
        {
            WriteWav(path, samples);
            Logger.Info($"Generated sound: {name}.wav");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Sound generate {name}: {ex.Message}");
        }
    }

    private enum Wave { Sine, Square, Saw, Triangle }

    private const int SampleRate = 44100;

    private static short[] Make(int ms, Action<Master> build)
    {
        var m = new Master(SampleRate * ms / 1000);
        build(m);
        return m.Finalize();
    }

    // Additive synthesis engine: Xbox element (crisp electronic waves) + PS5
    // element (soft plucks, airy filtered noise, echo tails) summed and mixed.
    private sealed class Master
    {
        private readonly float[] _buf;
        private readonly Random _rnd = new(42);

        public Master(int len) => _buf = new float[len];

        public void Tone(double freq, int ms, double vol, Wave wave, int off = 0, double atk = 3, double rel = 14, double vib = 0)
        {
            var start = off * SampleRate / 1000;
            var to = Math.Min(_buf.Length, start + ms * SampleRate / 1000);
            double phase = 0, vph = 0;
            for (var i = Math.Max(0, start); i < to; i++)
            {
                var tMs = (i - start) * 1000.0 / SampleRate;
                var f = freq * (1 + Math.Sin(vph) * vib);
                vph += 2 * Math.PI * vib / SampleRate;
                _buf[i] += (float)(WaveValue(phase, wave) * AttackDecay(tMs, atk, rel) * vol);
                phase += 2 * Math.PI * f / SampleRate;
            }
        }

        public void Slide(double from, double to, int ms, double vol, Wave wave, int off = 0, double atk = 3, double rel = 16, double vib = 0)
        {
            var start = off * SampleRate / 1000;
            var end = Math.Min(_buf.Length, start + ms * SampleRate / 1000);
            double phase = 0, vph = 0;
            for (var i = Math.Max(0, start); i < end; i++)
            {
                var tMs = (i - start) * 1000.0 / SampleRate;
                var t = Math.Min(1.0, tMs / ms);
                var f = (from + (to - from) * t) * (1 + Math.Sin(vph) * vib);
                vph += 2 * Math.PI * vib / SampleRate;
                _buf[i] += (float)(WaveValue(phase, wave) * AttackDecay(tMs, atk, rel) * vol);
                phase += 2 * Math.PI * f / SampleRate;
            }
        }

        // PS5-style dreamy pluck: soft sine with long exponential ring and
        // a faint octave partial for airiness.
        public void Pluck(double freq, int decayMs, double vol, int off = 0, double bend = 0.015)
        {
            var start = off * SampleRate / 1000;
            var to = Math.Min(_buf.Length, start + decayMs * 3 * SampleRate / 1000);
            double phase = 0, phase2 = 0;
            for (var i = Math.Max(0, start); i < to; i++)
            {
                var tMs = (i - start) * 1000.0 / SampleRate;
                var env = Math.Exp(-tMs / decayMs);
                var f = freq * (1 - bend * (tMs / (decayMs * 3)));
                _buf[i] += (float)((Math.Sin(phase) + 0.22 * Math.Sin(phase2)) * env * vol);
                phase += 2 * Math.PI * f / SampleRate;
                phase2 += 2 * Math.PI * f * 2.1 / SampleRate;
            }
        }

        // Soft filtered noise "air" (PS5 whooshes / transients).
        public void Noise(int ms, double vol, double cutoff, int off = 0, double atk = 2, double rel = 10)
        {
            var start = off * SampleRate / 1000;
            var to = Math.Min(_buf.Length, start + ms * SampleRate / 1000);
            var alpha = 1.0 / (1.0 + SampleRate / (2 * Math.PI * cutoff));
            double lp = 0;
            for (var i = Math.Max(0, start); i < to; i++)
            {
                lp += alpha * ((_rnd.NextDouble() * 2 - 1) - lp);
                var tMs = (i - start) * 1000.0 / SampleRate;
                _buf[i] += (float)(lp * AttackDecay(tMs, atk, rel) * vol);
            }
        }

        // Simple multi-tap echo for the airy PS5 reverb tail.
        public void Echo(double feedback, int spacingMs, int taps = 2)
        {
            var delay = spacingMs * SampleRate / 1000;
            var src = (float[])_buf.Clone();
            for (var tap = 1; tap <= taps; tap++)
            {
                var d = delay * tap;
                var g = Math.Pow(feedback, tap);
                for (var i = d; i < _buf.Length; i++)
                    _buf[i] += (float)(src[i - d] * g);
            }
        }

        private static double WaveValue(double phase, Wave wave) => wave switch
        {
            Wave.Sine => Math.Sin(phase),
            Wave.Square => Math.Sin(phase) >= 0 ? 1 : -1,
            Wave.Saw => 2 * (phase / (2 * Math.PI) % 1.0) - 1,
            Wave.Triangle => 2 * Math.Asin(Math.Sin(phase)) / Math.PI,
            _ => 0
        };

        private static double AttackDecay(double tMs, double atkMs, double relMs)
        {
            if (tMs <= 0) return 0;
            if (tMs < atkMs) return Math.Min(1.0, tMs / atkMs);
            return Math.Exp(-(tMs - atkMs) / relMs);
        }

        public short[] Finalize()
        {
            var fade = Math.Min(_buf.Length, SampleRate * 6 / 1000);
            for (var i = 0; i < fade; i++)
                _buf[_buf.Length - 1 - i] *= (float)i / fade;

            float peak = 0;
            for (var i = 0; i < _buf.Length; i++)
                peak = Math.Max(peak, Math.Abs(_buf[i]));
            var scale = peak > 0.9f ? 0.9f / peak : 1f;

            var outS = new short[_buf.Length];
            for (var i = 0; i < _buf.Length; i++)
            {
                var v = Math.Clamp(_buf[i] * scale, -1f, 1f);
                outS[i] = (short)(v * short.MaxValue * 0.88);
            }
            return outS;
        }
    }

    private static void WriteWav(string path, short[] samples)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write("RIFF"u8.ToArray());
        bw.Write(36 + samples.Length * 2);
        bw.Write("WAVE"u8.ToArray());
        bw.Write("fmt "u8.ToArray());
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(SampleRate);
        bw.Write(SampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write("data"u8.ToArray());
        bw.Write(samples.Length * 2);
        foreach (var s in samples)
            bw.Write(s);
        bw.Flush();
        File.WriteAllBytes(path, ms.ToArray());
    }
}
