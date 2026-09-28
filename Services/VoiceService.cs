using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace AiLaTrieuPhu.Services
{
    public class VoiceService
    {
        private readonly MediaPlayer _player;
        private readonly string _cacheDir;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cts;
        private readonly object _lock = new();
        private readonly Random _random = new();

        public bool IsVoiceEnabled { get; set; } = true;

        public VoiceService()
        {
            _player = new MediaPlayer();
            _cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "VoiceCache");
            if (!Directory.Exists(_cacheDir))
            {
                try { Directory.CreateDirectory(_cacheDir); } catch { }
            }

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public void Stop()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts = null;
            }

            try
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    _player.Stop();
                    _player.Close();
                });
            }
            catch { }
        }

        public void Speak(string text)
        {
            if (!IsVoiceEnabled || string.IsNullOrWhiteSpace(text)) return;
            PlaySequenceAsync(new[] { text });
        }

        /// <summary>
        /// MC đọc câu hỏi và 4 phương án A, B, C, D bằng giọng tiếng Việt chuẩn tự nhiên
        /// </summary>
        public void SpeakQuestion(int level, string content, List<string> options)
        {
            if (!IsVoiceEnabled || options.Count < 4) return;

            var phrases = new List<string>
            {
                $"Câu hỏi số {level}: {content}.",
                $"Phương án A: {options[0]}.",
                $"Phương án B: {options[1]}.",
                $"Phương án C: {options[2]}.",
                $"Phương án D: {options[3]}."
            };

            PlaySequenceAsync(phrases);
        }

        private void PlaySequenceAsync(IEnumerable<string> phrases)
        {
            CancellationToken ct;
            lock (_lock)
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                ct = _cts.Token;
            }

            Task.Run(async () =>
            {
                foreach (var phrase in phrases)
                {
                    if (ct.IsCancellationRequested) break;

                    string? filePath = await GetOrDownloadAudioAsync(phrase, ct);
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;
                    if (ct.IsCancellationRequested) break;

                    await PlayAudioFileAsync(filePath, ct);
                }
            }, ct);
        }

        private async Task<string?> GetOrDownloadAudioAsync(string text, CancellationToken ct)
        {
            try
            {
                string hash = ComputeHash(text.Trim().ToLower());
                string filePath = Path.Combine(_cacheDir, $"{hash}.mp3");

                if (File.Exists(filePath)) return filePath;

                string url = $"https://translate.google.com/translate_tts?ie=UTF-8&tl=vi&client=tw-ob&q={Uri.EscapeDataString(text)}";
                var response = await _httpClient.GetAsync(url, ct);
                if (response.IsSuccessStatusCode)
                {
                    byte[] data = await response.Content.ReadAsByteArrayAsync(ct);
                    string tmpFile = filePath + ".tmp";
                    await File.WriteAllBytesAsync(tmpFile, data, ct);
                    if (File.Exists(tmpFile))
                    {
                        File.Move(tmpFile, filePath, true);
                    }
                    return filePath;
                }
            }
            catch { }

            return null;
        }

        private async Task PlayAudioFileAsync(string filePath, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null)
                {
                    await dispatcher.InvokeAsync(() =>
                    {
                        if (ct.IsCancellationRequested)
                        {
                            tcs.TrySetCanceled();
                            return;
                        }

                        EventHandler? mediaEndedHandler = null;
                        EventHandler<ExceptionEventArgs>? mediaFailedHandler = null;

                        mediaEndedHandler = (s, e) =>
                        {
                            _player.MediaEnded -= mediaEndedHandler;
                            _player.MediaFailed -= mediaFailedHandler;
                            tcs.TrySetResult(true);
                        };

                        mediaFailedHandler = (s, e) =>
                        {
                            _player.MediaEnded -= mediaEndedHandler;
                            _player.MediaFailed -= mediaFailedHandler;
                            tcs.TrySetResult(false);
                        };

                        _player.MediaEnded += mediaEndedHandler;
                        _player.MediaFailed += mediaFailedHandler;

                        _player.Open(new Uri(filePath, UriKind.Absolute));
                        _player.Play();
                    });
                }

                using (ct.Register(() =>
                {
                    try
                    {
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            _player.Stop();
                            _player.Close();
                        });
                    }
                    catch { }
                    tcs.TrySetCanceled();
                }))
                {
                    // Chờ phát xong hoặc timeout tối đa 15 giây mỗi câu
                    await Task.WhenAny(tcs.Task, Task.Delay(15000, ct));
                }
            }
            catch { }
        }

        private string ComputeHash(string input)
        {
            using var md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public void SpeakConfirming(int answerIndex)
        {
            if (!IsVoiceEnabled) return;
            char optLetter = (char)('A' + answerIndex);
            Speak($"Bạn đã chọn phương án {optLetter}. Hãy cùng chờ kết quả!");
        }

        public void SpeakCorrect(int level)
        {
            if (!IsVoiceEnabled) return;

            string[] phrases = new[]
            {
                "Chính xác! Câu trả lời hoàn toàn chính xác!",
                "Rất xuất sắc! Xin chúc mừng bạn!",
                "Hoàn toàn chuẩn xác! Bạn đang thể hiện một phong độ rất tuyệt vời!",
                "Chính xác rồi! Bạn đã tiến thêm một bước nữa!",
                "Rất tự tin và rất chính xác! Xin chúc mừng bạn!"
            };

            Speak(phrases[_random.Next(phrases.Length)]);
        }

        public void SpeakMilestone(int level, string playerName, long prize)
        {
            if (!IsVoiceEnabled) return;

            string speech = level switch
            {
                5 => "Tuyệt vời! Chúc mừng bạn đã xuất sắc vượt qua mốc số 5, chắc chắn bảo toàn 2 triệu đồng tiền thưởng!",
                10 => "Thật phi thường! Xin nhiệt liệt chúc mừng bạn đã chinh phục mốc số 10 cực kỳ quan trọng, bảo toàn 22 triệu đồng!",
                15 => "Kỳ tích đã xuất hiện tại trường quay! Xin nhiệt liệt chúc mừng Tân Triệu Phú đã xuất sắc vượt qua trọn vẹn 15 câu hỏi và rinh về giải thưởng 150 triệu đồng!",
                _ => $"Chúc mừng bạn đã hoàn thành câu hỏi số {level}!"
            };

            Speak(speech);
        }

        public void SpeakWrong(int level, string playerName, long guaranteedPrize)
        {
            if (!IsVoiceEnabled) return;

            string speech = guaranteedPrize > 0
                ? "Rất tiếc đáp án bạn vừa chọn chưa chính xác. Nhưng bạn vẫn xuất sắc bảo toàn được số tiền thưởng của chương trình!"
                : "Rất tiếc đáp án bạn vừa chọn chưa chính xác. Xin cảm ơn bạn đã tham gia chương trình và chúc bạn may mắn hơn ở lần chơi tiếp theo!";

            Speak(speech);
        }

        public void SpeakTimeOut(string playerName, long guaranteedPrize)
        {
            if (!IsVoiceEnabled) return;

            string speech = "Đã hết thời gian suy nghĩ! Rất tiếc bạn phải dừng cuộc chơi tại đây.";
            Speak(speech);
        }

        public void SpeakWalkAway(string playerName, long prize)
        {
            if (!IsVoiceEnabled) return;

            string speech = "Một quyết định dừng cuộc chơi rất bản lĩnh và sáng suốt! Xin chúc mừng bạn!";
            Speak(speech);
        }
    }
}
