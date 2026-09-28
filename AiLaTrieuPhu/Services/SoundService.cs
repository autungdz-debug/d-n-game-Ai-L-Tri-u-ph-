using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace AiLaTrieuPhu.Services
{
    public class SoundService
    {
        private readonly string _soundDir;
        public bool IsMuted { get; set; } = false;

        public SoundService()
        {
            _soundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
            SoundGenerator.EnsureSoundsExist(_soundDir);
        }

        private void Play(string soundFileName)
        {
            if (IsMuted) return;
            Task.Run(() =>
            {
                try
                {
                    string path = Path.Combine(_soundDir, soundFileName);
                    if (File.Exists(path))
                    {
                        using (var player = new SoundPlayer(path))
                        {
                            player.PlaySync();
                        }
                    }
                }
                catch { }
            });
        }

        public void PlayTick() => Play("tick.wav");
        public void PlaySelect() => Play("select.wav");
        public void PlayCorrect() => Play("correct.wav");
        public void PlayWrong() => Play("wrong.wav");
        public void PlayLifeline() => Play("lifeline.wav");
        public void PlayMilestone() => Play("milestone.wav");
        public void PlayVictory() => Play("win.wav");
    }
}