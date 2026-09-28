using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AiLaTrieuPhu.Models;

namespace AiLaTrieuPhu.Services
{
    public class HighScoreService
    {
        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "highscores.json");

        public static List<HighScore> LoadHighScores()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var list = JsonSerializer.Deserialize<List<HighScore>>(json);
                    if (list != null && list.Count > 0)
                    {
                        var sorted = list.OrderByDescending(x => x.PrizeMoney)
                                         .ThenByDescending(x => x.QuestionsAnswered)
                                         .ThenByDescending(x => x.DateAchieved)
                                         .Take(50)
                                         .ToList();
                        AssignRanks(sorted);
                        return sorted;
                    }
                }
            }
            catch { }

            return ResetDefaults();
        }

        public static void SaveHighScores(List<HighScore> scores)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(scores, options);
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }

        public static void AddScore(string playerName, long prizeMoney, int questionsAnswered)
        {
            var scores = LoadHighScores();
            scores.Add(new HighScore
            {
                PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Người chơi" : playerName.Trim(),
                PrizeMoney = prizeMoney,
                QuestionsAnswered = questionsAnswered,
                DateAchieved = DateTime.Now
            });

            var topScores = scores.OrderByDescending(x => x.PrizeMoney)
                                  .ThenByDescending(x => x.QuestionsAnswered)
                                  .ThenByDescending(x => x.DateAchieved)
                                  .Take(50)
                                  .ToList();
            AssignRanks(topScores);
            SaveHighScores(topScores);
        }

        /// <summary>
        /// Thống kê tổng hợp theo từng người chơi để xác định:
        /// - Ai có những câu trả lời đúng nhiều nhất (kỷ lục ván & tổng câu đúng)
        /// - Số lần vượt qua thử thách 15 câu hỏi của mỗi người
        /// </summary>
        public static List<PlayerStat> GetPlayerStats(string sortBy = "15Questions")
        {
            var scores = LoadHighScores();
            var groups = scores.GroupBy(s => s.PlayerName.Trim(), StringComparer.OrdinalIgnoreCase);

            var stats = new List<PlayerStat>();

            foreach (var g in groups)
            {
                int times15 = g.Count(x => x.QuestionsAnswered >= 15 || x.PrizeMoney >= 150000000);
                int maxCorrect = g.Max(x => x.QuestionsAnswered);
                int totalCorrect = g.Sum(x => x.QuestionsAnswered);
                long highestPrize = g.Max(x => x.PrizeMoney);
                int totalGames = g.Count();
                DateTime lastPlayed = g.Max(x => x.DateAchieved);

                stats.Add(new PlayerStat
                {
                    PlayerName = g.Key,
                    TimesPassed15Questions = times15,
                    MaxCorrectAnswers = maxCorrect,
                    TotalCorrectAnswers = totalCorrect,
                    TotalGamesPlayed = totalGames,
                    HighestPrize = highestPrize,
                    LastPlayed = lastPlayed
                });
            }

            stats = sortBy switch
            {
                "CorrectAnswers" => stats.OrderByDescending(x => x.MaxCorrectAnswers)
                                         .ThenByDescending(x => x.TotalCorrectAnswers)
                                         .ThenByDescending(x => x.TimesPassed15Questions)
                                         .ThenByDescending(x => x.HighestPrize)
                                         .ToList(),

                "TotalCorrect" => stats.OrderByDescending(x => x.TotalCorrectAnswers)
                                       .ThenByDescending(x => x.MaxCorrectAnswers)
                                       .ThenByDescending(x => x.TimesPassed15Questions)
                                       .ToList(),

                "Prize" => stats.OrderByDescending(x => x.HighestPrize)
                                .ThenByDescending(x => x.TimesPassed15Questions)
                                .ThenByDescending(x => x.MaxCorrectAnswers)
                                .ToList(),

                _ => stats.OrderByDescending(x => x.TimesPassed15Questions)
                          .ThenByDescending(x => x.MaxCorrectAnswers)
                          .ThenByDescending(x => x.TotalCorrectAnswers)
                          .ThenByDescending(x => x.HighestPrize)
                          .ToList()
            };

            for (int i = 0; i < stats.Count; i++)
            {
                stats[i].Rank = i + 1;
            }

            return stats;
        }

        /// <summary>
        /// Lấy thông tin người vượt qua thử thách 15 câu nhiều nhất.
        /// </summary>
        public static void GetTop15QuestionsChampion(out string playerName, out int timesPassed)
        {
            var stats = GetPlayerStats("15Questions");
            var top = stats.FirstOrDefault(x => x.TimesPassed15Questions > 0);
            if (top != null)
            {
                playerName = top.PlayerName;
                timesPassed = top.TimesPassed15Questions;
            }
            else
            {
                playerName = "Chưa có";
                timesPassed = 0;
            }
        }

        /// <summary>
        /// Lấy thông tin người có số câu trả lời đúng nhiều nhất (kỷ lục cao nhất).
        /// </summary>
        public static void GetTopCorrectAnswersPlayer(out string playerName, out int maxCorrect, out int totalCorrect)
        {
            var stats = GetPlayerStats("CorrectAnswers");
            var top = stats.FirstOrDefault();
            if (top != null)
            {
                playerName = top.PlayerName;
                maxCorrect = top.MaxCorrectAnswers;
                totalCorrect = top.TotalCorrectAnswers;
            }
            else
            {
                playerName = "Chưa có";
                maxCorrect = 0;
                totalCorrect = 0;
            }
        }

        /// <summary>
        /// Tổng số lần thử thách 15 câu đã được chinh phục trong lịch sử.
        /// </summary>
        public static int GetTotal15QuestionsPassedCount()
        {
            var scores = LoadHighScores();
            return scores.Count(x => x.QuestionsAnswered >= 15 || x.PrizeMoney >= 150000000);
        }

        public static List<HighScore> ResetDefaults()
        {
            var defaults = new List<HighScore>
            {
                new HighScore { PlayerName = "Âu Nguyễn Quang Tùng", PrizeMoney = 150000000, QuestionsAnswered = 15, DateAchieved = DateTime.Now.AddDays(-10) },
                new HighScore { PlayerName = "Âu Nguyễn Quang Tùng", PrizeMoney = 150000000, QuestionsAnswered = 15, DateAchieved = DateTime.Now.AddDays(-5) },
                new HighScore { PlayerName = "Lê Hoàng Minh", PrizeMoney = 150000000, QuestionsAnswered = 15, DateAchieved = DateTime.Now.AddDays(-3) },
                new HighScore { PlayerName = "Trịnh Bách Tuấn", PrizeMoney = 40000000, QuestionsAnswered = 12, DateAchieved = DateTime.Now.AddDays(-2) },
                new HighScore { PlayerName = "Phạm Thu Trang", PrizeMoney = 10000000, QuestionsAnswered = 8, DateAchieved = DateTime.Now.AddDays(-1) },
                new HighScore { PlayerName = "Đỗ Hải Đăng", PrizeMoney = 2000000, QuestionsAnswered = 5, DateAchieved = DateTime.Now }
            };
            AssignRanks(defaults);
            SaveHighScores(defaults);
            return defaults;
        }

        public static void ClearScores()
        {
            var empty = new List<HighScore>();
            SaveHighScores(empty);
        }

        private static void AssignRanks(List<HighScore> scores)
        {
            for (int i = 0; i < scores.Count; i++)
            {
                scores[i].Rank = i + 1;
            }
        }
    }
}