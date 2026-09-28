using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using AiLaTrieuPhu.Models;

namespace AiLaTrieuPhu.Services
{
    public class QuestionService
    {
        private string _filePath;
        private List<Question> _questions = new();
        private readonly Random _random = new();

        public QuestionService()
        {
            _filePath = ResolveQuestionsFilePath();
            LoadQuestions();
        }

        /// <summary>
        /// Tìm đường dẫn file questions.json tốt nhất (ưu tiên file có nhiều câu hỏi nhất).
        /// </summary>
        private string ResolveQuestionsFilePath()
        {
            var candidates = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "questions.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "questions.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Resources", "questions.json"),
                @"D:\BÁO CÁO_ GAME_AI_LÀ_TRIỆU_PHÚ\AiLaTrieuPhu\Resources\questions.json",
                @"D:\BÁO CÁO_ GAME_AI_LÀ_TRIỆU_PHÚ\AiLaTrieuPhu\AiLaTrieuPhu\Resources\questions.json"
            };

            string bestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "questions.json");
            int maxCount = -1;

            foreach (var p in candidates)
            {
                try
                {
                    string fullPath = Path.GetFullPath(p);
                    if (File.Exists(fullPath))
                    {
                        string json = File.ReadAllText(fullPath);
                        var list = JsonSerializer.Deserialize<List<Question>>(json);
                        if (list != null && list.Count > maxCount)
                        {
                            maxCount = list.Count;
                            bestPath = fullPath;
                        }
                    }
                }
                catch { }
            }

            return bestPath;
        }

        public void LoadQuestions()
        {
            try
            {
                _filePath = ResolveQuestionsFilePath();

                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<Question>>(json);
                    if (list != null && list.Count > 0)
                    {
                        _questions = list;

                        // Đồng bộ file sang thư mục chạy AppDomain nếu đang đọc từ file nguồn ngoài
                        string runtimePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "questions.json");
                        if (!string.Equals(Path.GetFullPath(_filePath), Path.GetFullPath(runtimePath), StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                string dir = Path.GetDirectoryName(runtimePath)!;
                                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                                File.WriteAllText(runtimePath, json);
                            }
                            catch { }
                        }
                        return;
                    }
                }
            }
            catch { }

            // Nếu file thiếu hoặc lỗi thì nạp danh sách dự phòng
            _questions = GetFallbackQuestions();
            SaveQuestions();
        }

        public void ReloadQuestions()
        {
            LoadQuestions();
        }

        public void SaveQuestions()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(_questions, options);

                // Lưu vào file hiện tại
                string dir = Path.GetDirectoryName(_filePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_filePath, json);

                // Đồng bộ lưu thêm vào runtime folder nếu khác
                string runtimePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "questions.json");
                if (!string.Equals(Path.GetFullPath(_filePath), Path.GetFullPath(runtimePath), StringComparison.OrdinalIgnoreCase))
                {
                    string rDir = Path.GetDirectoryName(runtimePath)!;
                    if (!Directory.Exists(rDir)) Directory.CreateDirectory(rDir);
                    File.WriteAllText(runtimePath, json);
                }

                // Đồng bộ lưu thêm vào thư mục dự án nguồn nếu tồn tại
                string devPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "questions.json");
                if (File.Exists(devPath))
                {
                    File.WriteAllText(devPath, json);
                }
            }
            catch { }
        }

        public List<Question> GetAllQuestions() => _questions;

        /// <summary>
        /// Khởi tạo một bộ 15 câu hỏi hoàn toàn mới (cấp độ 1 đến 15) từ ngân hàng câu hỏi tổng thể.
        /// Đảm bảo luân phiên không trùng lặp với các câu hỏi người chơi đã làm ở các ván trước đó.
        /// </summary>
        public List<Question> Generate15QuestionsSet(HashSet<int> historyUsedIds)
        {
            var newSet = new List<Question>();

            for (int level = 1; level <= 15; level++)
            {
                var candidates = _questions.Where(q => q.Level == level).ToList();

                if (candidates.Count == 0)
                {
                    newSet.Add(new Question
                    {
                        Id = 10000 + level,
                        Level = level,
                        Content = $"Câu hỏi dự phòng cấp độ {level}?",
                        Options = new List<string> { "Phương án A", "Phương án B", "Phương án C", "Phương án D" },
                        CorrectIndex = 0,
                        Category = "Tổng hợp"
                    });
                    continue;
                }

                // Lọc những câu hỏi mà người chơi CHƯA từng làm ở các ván trước
                var unused = candidates.Where(q => !historyUsedIds.Contains(q.Id)).ToList();

                // Nếu tất cả câu hỏi của cấp độ này đã được làm hết qua các ván trước:
                // Tự động làm mới chu kỳ câu hỏi cho cấp độ này!
                if (unused.Count == 0)
                {
                    foreach (var c in candidates)
                    {
                        historyUsedIds.Remove(c.Id);
                    }
                    unused = candidates.ToList();
                }

                int rndIndex = _random.Next(unused.Count);
                var picked = unused[rndIndex].Clone();
                newSet.Add(picked);
                historyUsedIds.Add(picked.Id);
            }

            return newSet;
        }

        /// <summary>
        /// Lấy một câu hỏi thay thế cùng cấp độ khi người chơi dùng quyền trợ giúp Đổi câu hỏi.
        /// </summary>
        public Question? GetSwitchQuestion(int level, HashSet<int> excludedIds)
        {
            var candidates = _questions.Where(q => q.Level == level && !excludedIds.Contains(q.Id)).ToList();
            if (candidates.Count == 0)
            {
                // Nếu tất cả câu hỏi đã nằm trong phiên, lấy ngẫu nhiên 1 câu khác
                candidates = _questions.Where(q => q.Level == level).ToList();
            }

            if (candidates.Count == 0) return null;
            return candidates[_random.Next(candidates.Count)].Clone();
        }

        public Question? GetQuestionForLevel(int level, HashSet<int>? usedIds = null)
        {
            var candidates = _questions.Where(q => q.Level == level).ToList();
            if (candidates.Count == 0) return null;

            if (usedIds != null)
            {
                var unused = candidates.Where(q => !usedIds.Contains(q.Id)).ToList();
                if (unused.Count > 0)
                {
                    int index = _random.Next(unused.Count);
                    return unused[index].Clone();
                }
            }

            int rnd = _random.Next(candidates.Count);
            return candidates[rnd].Clone();
        }

        public void AddQuestion(Question question)
        {
            int maxId = _questions.Count > 0 ? _questions.Max(q => q.Id) : 0;
            question.Id = maxId + 1;
            _questions.Add(question);
            SaveQuestions();
        }

        public bool DeleteQuestion(int id)
        {
            var q = _questions.FirstOrDefault(x => x.Id == id);
            if (q != null)
            {
                _questions.Remove(q);
                SaveQuestions();
                return true;
            }
            return false;
        }

        public bool UpdateQuestion(Question question)
        {
            var idx = _questions.FindIndex(x => x.Id == question.Id);
            if (idx >= 0)
            {
                _questions[idx] = question;
                SaveQuestions();
                return true;
            }
            return false;
        }

        private List<Question> GetFallbackQuestions()
        {
            return new List<Question>
            {
                new Question { Id = 1, Level = 1, Content = "Thủ đô của Việt Nam là gì?", Options = new List<string> { "Hà Nội", "TP Hồ Chí Minh", "Đà Nẵng", "Huế" }, CorrectIndex = 0, Category = "Địa lý" },
                new Question { Id = 2, Level = 2, Content = "Thánh Gióng đánh tan giặc nào?", Options = new List<string> { "Giặc Ân", "Giặc Tống", "Giặc Minh", "Giặc Xiêm" }, CorrectIndex = 0, Category = "Lịch sử" },
                new Question { Id = 3, Level = 3, Content = "Bánh chưng hình gì?", Options = new List<string> { "Hình tròn", "Hình vuông", "Hình tam giác", "Hình lục giác" }, CorrectIndex = 1, Category = "Ẩm thực" },
                new Question { Id = 4, Level = 4, Content = "Chí Phèo là sáng tác của ai?", Options = new List<string> { "Nam Cao", "Ngô Tất Tố", "Vũ Trọng Phụng", "Xuân Diệu" }, CorrectIndex = 0, Category = "Văn học" },
                new Question { Id = 5, Level = 5, Content = "Ngày Quốc khánh Việt Nam là ngày nào?", Options = new List<string> { "02/09/1945", "19/08/1945", "30/04/1975", "07/05/1954" }, CorrectIndex = 0, Category = "Lịch sử" },
                new Question { Id = 6, Level = 6, Content = "Fansipan cao bao nhiêu mét?", Options = new List<string> { "3.143 m", "3.000 m", "2.800 m", "3.500 m" }, CorrectIndex = 0, Category = "Địa lý" },
                new Question { Id = 7, Level = 7, Content = "Đảo tự nhiên lớn nhất thế giới là đảo nào?", Options = new List<string> { "Greenland", "Borneo", "Madagascar", "New Guinea" }, CorrectIndex = 0, Category = "Địa lý" },
                new Question { Id = 8, Level = 8, Content = "Khởi nghĩa Hai Bà Trưng nổ ra năm nào?", Options = new List<string> { "Năm 40 SCN", "Năm 43 SCN", "Năm 938 SCN", "Năm 248 SCN" }, CorrectIndex = 0, Category = "Lịch sử" },
                new Question { Id = 9, Level = 9, Content = "Vận tốc ánh sáng trong chân không xấp xỉ?", Options = new List<string> { "300.000 km/s", "150.000 km/s", "500.000 km/s", "100.000 km/s" }, CorrectIndex = 0, Category = "Vật lý" },
                new Question { Id = 10, Level = 10, Content = "Dạ cổ hoài lang ra đời tại tỉnh nào?", Options = new List<string> { "Bạc Liêu", "Cà Mau", "Tiền Giang", "Bến Tre" }, CorrectIndex = 0, Category = "Văn hóa" },
                new Question { Id = 11, Level = 11, Content = "Nam quốc sơn hà xuất hiện trong cuộc kháng chiến nào?", Options = new List<string> { "Chống Tống (1077)", "Chống Nam Hán (938)", "Chống Nguyên Mông (1285)", "Chống Minh (1427)" }, CorrectIndex = 0, Category = "Lịch sử" },
                new Question { Id = 12, Level = 12, Content = "Kim loại có nhiệt độ nóng chảy cao nhất?", Options = new List<string> { "Wolfram", "Titan", "Bạch kim", "Sắt" }, CorrectIndex = 0, Category = "Hóa học" },
                new Question { Id = 13, Level = 13, Content = "Rãnh biển sâu nhất Trái Đất là gì?", Options = new List<string> { "Mariana", "Java", "Puerto Rico", "Philippine" }, CorrectIndex = 0, Category = "Địa chất" },
                new Question { Id = 14, Level = 14, Content = "Hiệp định Genève ký kết năm nào?", Options = new List<string> { "1954", "1945", "1973", "1975" }, CorrectIndex = 0, Category = "Lịch sử" },
                new Question { Id = 15, Level = 15, Content = "Trạng nguyên 13 tuổi nhỏ nhất lịch sử nước ta?", Options = new List<string> { "Nguyễn Hiền", "Lương Thế Vinh", "Mạc Đĩnh Chi", "Lê Văn Hưu" }, CorrectIndex = 0, Category = "Lịch sử" }
            };
        }
    }
}