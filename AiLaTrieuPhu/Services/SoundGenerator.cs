using System;
using System.IO;

namespace AiLaTrieuPhu.Services
{
    public static class SoundGenerator
    {
        public static void EnsureSoundsExist(string folder)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string tickPath = Path.Combine(folder, "tick.wav");
                string correctPath = Path.Combine(folder, "correct.wav");
                string wrongPath = Path.Combine(folder, "wrong.wav");
                string selectPath = Path.Combine(folder, "select.wav");
                string lifelinePath = Path.Combine(folder, "lifeline.wav");
                string winPath = Path.Combine(folder, "win.wav");
                string milestonePath = Path.Combine(folder, "milestone.wav");

                if (!File.Exists(tickPath)) GenerateTone(tickPath, 1000, 0.08, 0.5, WaveType.Sine);
                if (!File.Exists(selectPath)) GenerateChord(selectPath, new[] { 440.0, 554.37, 659.25 }, 0.35, 0.6);
                if (!File.Exists(correctPath)) GenerateArpeggio(correctPath, new[] { 523.25, 659.25, 783.99, 1046.50 }, 0.18, 0.8);
                if (!File.Exists(wrongPath)) GenerateDissonance(wrongPath, new[] { 220.0, 207.65, 196.0 }, 0.8, 0.8);
                if (!File.Exists(lifelinePath)) GenerateWhoosh(lifelinePath, 300, 1200, 0.6, 0.7);
                if (!File.Exists(milestonePath)) GenerateFanfare(milestonePath);
                if (!File.Exists(winPath)) GenerateVictoryFanfare(winPath);
            }
            catch { }
        }

        public enum WaveType { Sine, Square, Triangle }

        private static void GenerateTone(string file, double freq, double durationSec, double volume, WaveType type)
        {
            int sampleRate = 44100;
            int numSamples = (int)(sampleRate * durationSec);
            short[] samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double val = 0;
                double envelope = 1.0 - ((double)i / numSamples); // Decay

                switch (type)
                {
                    case WaveType.Sine:
                        val = Math.Sin(2 * Math.PI * freq * t);
                        break;
                    case WaveType.Square:
                        val = Math.Sin(2 * Math.PI * freq * t) >= 0 ? 1.0 : -1.0;
                        break;
                }
                samples[i] = (short)(val * envelope * volume * short.MaxValue);
            }
            WriteWavFile(file, samples, sampleRate);
        }

        private static void GenerateChord(string file, double[] freqs, double durationSec, double volume)
        {
            int sampleRate = 44100;
            int numSamples = (int)(sampleRate * durationSec);
            short[] samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double val = 0;
                double envelope = Math.Exp(-3.0 * t / durationSec);

                foreach (var f in freqs)
                {
                    val += Math.Sin(2 * Math.PI * f * t);
                }
                val /= freqs.Length;
                samples[i] = (short)(val * envelope * volume * short.MaxValue);
            }
            WriteWavFile(file, samples, sampleRate);
        }

        private static void GenerateArpeggio(string file, double[] notes, double noteDurationSec, double volume)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * noteDurationSec * notes.Length);
            short[] samples = new short[totalSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                double f = notes[n];
                int start = (int)(n * noteDurationSec * sampleRate);
                int count = (int)(noteDurationSec * sampleRate);

                for (int i = 0; i < count; i++)
                {
                    int idx = start + i;
                    if (idx >= totalSamples) break;
                    double t = (double)i / sampleRate;
                    double envelope = (1.0 - (double)i / count);
                    double val = Math.Sin(2 * Math.PI * f * t) + 0.3 * Math.Sin(4 * Math.PI * f * t);
                    samples[idx] = (short)(val * 0.7 * envelope * volume * short.MaxValue);
                }
            }
            WriteWavFile(file, samples, sampleRate);
        }

        private static void GenerateDissonance(string file, double[] notes, double durationSec, double volume)
        {
            int sampleRate = 44100;
            int numSamples = (int)(sampleRate * durationSec);
            short[] samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double val = 0;
                double envelope = Math.Exp(-2.0 * t / durationSec);

                foreach (var f in notes)
                {
                    val += (Math.Sin(2 * Math.PI * f * t) + 0.5 * Math.Sin(2 * Math.PI * (f * 1.5) * t));
                }
                val /= notes.Length * 1.5;
                samples[i] = (short)(val * envelope * volume * short.MaxValue);
            }
            WriteWavFile(file, samples, sampleRate);
        }

        private static void GenerateWhoosh(string file, double startFreq, double endFreq, double durationSec, double volume)
        {
            int sampleRate = 44100;
            int numSamples = (int)(sampleRate * durationSec);
            short[] samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double frac = (double)i / numSamples;
                double currentFreq = startFreq + (endFreq - startFreq) * Math.Pow(frac, 2);
                double envelope = Math.Sin(Math.PI * frac); // Smooth bell shape
                double val = Math.Sin(2 * Math.PI * currentFreq * t);
                samples[i] = (short)(val * envelope * volume * short.MaxValue);
            }
            WriteWavFile(file, samples, sampleRate);
        }

        private static void GenerateFanfare(string file)
        {
            double[] notes = new[] { 523.25, 659.25, 783.99, 1046.50, 1318.51 };
            GenerateArpeggio(file, notes, 0.22, 0.85);
        }

        private static void GenerateVictoryFanfare(string file)
        {
            double[] notes = new[] { 523.25, 659.25, 783.99, 1046.50, 783.99, 1046.50, 1318.51, 1567.98 };
            GenerateArpeggio(file, notes, 0.25, 0.9);
        }

        private static void WriteWavFile(string filename, short[] samples, int sampleRate)
        {
            using (var stream = new FileStream(filename, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int byteRate = sampleRate * 2; // 16-bit mono
                int dataLength = samples.Length * 2;

                // RIFF header
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataLength);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16); // subchunk size
                writer.Write((short)1); // PCM
                writer.Write((short)1); // Mono
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)2); // block align
                writer.Write((short)16); // bits per sample

                // data chunk
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(dataLength);

                foreach (var sample in samples)
                {
                    writer.Write(sample);
                }
            }
        }
    }
}