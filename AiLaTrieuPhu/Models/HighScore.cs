using System;

namespace AiLaTrieuPhu.Models
{
    public class HighScore
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; } = "Người chơi";
        public long PrizeMoney { get; set; }
        public int QuestionsAnswered { get; set; }
        public DateTime DateAchieved { get; set; } = DateTime.Now;

        public string PrizeFormatted => $"{PrizeMoney:N0} VNĐ";
        public string DateFormatted => DateAchieved.ToString("dd/MM/yyyy HH:mm");
        public string QuestionsFormatted => $"{QuestionsAnswered} / 15";
        public bool IsVictory => QuestionsAnswered >= 15;

        public string RankDisplay => Rank switch
        {
            1 => "🥇 Quán Quân",
            2 => "🥈 Á Quân",
            3 => "🥉 Hạng Ba",
            _ => $"#{Rank}"
        };

        public string AchievementBadge => QuestionsAnswered switch
        {
            15 => "🏆 TRIỆU PHÚ",
            >= 10 => "⭐ Mốc 10 (22 Tr)",
            >= 5 => "✨ Mốc 5 (2 Tr)",
            _ => "Khởi động"
        };
    }

    /// <summary>
    /// Thống kê chi tiết thành tích tổng hợp của từng người chơi:
    /// - Số lần vượt qua thử thách 15 câu hỏi (Vô địch)
    /// - Kỷ lục số câu trả lời đúng nhiều nhất trong 1 ván
    /// - Tổng số câu trả lời đúng tích lũy
    /// - Tổng số ván chơi & giải thưởng cao nhất
    /// </summary>
    public class PlayerStat
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int TimesPassed15Questions { get; set; } // Số lần vượt qua thử thách 15 câu
        public int MaxCorrectAnswers { get; set; }      // Kỷ lục câu đúng nhiều nhất trong 1 ván
        public int TotalCorrectAnswers { get; set; }    // Tổng số câu trả lời đúng tích lũy
        public int TotalGamesPlayed { get; set; }       // Tổng số lượt chơi
        public long HighestPrize { get; set; }          // Tiền thưởng cao nhất đạt được
        public DateTime LastPlayed { get; set; }        // Thời gian chơi gần nhất

        public string RankDisplay => Rank switch
        {
            1 => "🥇 Kỷ Lục Gia",
            2 => "🥈 Á Vương",
            3 => "🥉 Hạng Ba",
            _ => $"#{Rank}"
        };

        public string HighestPrizeFormatted => $"{HighestPrize:N0} VNĐ";
        public string LastPlayedFormatted => LastPlayed.ToString("dd/MM/yyyy HH:mm");
        public string MaxCorrectFormatted => $"{MaxCorrectAnswers} / 15 câu";
        public string TimesPassedFormatted => $"{TimesPassed15Questions} lần";

        public string AchievementBadge => TimesPassed15Questions switch
        {
            >= 3 => "👑 Huyền Thoại Triệu Phú",
            >= 2 => "🏆 Đại Kiện Tướng",
            1 => "⭐ Tân Triệu Phú",
            _ when MaxCorrectAnswers >= 10 => "✨ Bậc Thầy Tri Thức",
            _ when MaxCorrectAnswers >= 5 => "🎯 Trí Tuệ Vững Vàng",
            _ => "🌱 Người Chơi Tiềm Năng"
        };
    }
}