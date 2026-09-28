using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Services;

namespace AiLaTrieuPhu.Views
{
    public partial class HighScoreWindow : Window
    {
        private List<HighScore> _allScores = new();
        private string _currentTab = "PlayerStats"; // "PlayerStats" or "GameScores"
        private bool _isLoaded = false;

        public HighScoreWindow()
        {
            InitializeComponent();
            _isLoaded = true;
            LoadData();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void LoadData()
        {
            _allScores = HighScoreService.LoadHighScores();
            UpdateStatsCards();
            ApplyFilter();
        }

        private void UpdateStatsCards()
        {
            if (!_isLoaded) return;

            // 1. Kỷ lục người vượt qua 15 câu nhiều nhất
            HighScoreService.GetTop15QuestionsChampion(out string championName, out int timesPassed);
            if (timesPassed > 0)
            {
                txtTop15Player.Text = championName;
                txtTop15Count.Text = $"{timesPassed} lần vượt qua 15/15 câu";
            }
            else
            {
                txtTop15Player.Text = "Chưa có ai vượt qua";
                txtTop15Count.Text = "Đang chờ tân Triệu Phú";
            }

            // 2. Kỷ lục người có câu trả lời đúng nhiều nhất
            HighScoreService.GetTopCorrectAnswersPlayer(out string correctPlayer, out int maxCorrect, out int totalCorrect);
            if (!string.IsNullOrEmpty(correctPlayer) && correctPlayer != "Chưa có")
            {
                txtTopCorrectPlayer.Text = correctPlayer;
                txtTopCorrectCount.Text = $"Kỷ lục {maxCorrect} câu | Tổng {totalCorrect} câu đúng";
            }
            else
            {
                txtTopCorrectPlayer.Text = "Chưa có dữ liệu";
                txtTopCorrectCount.Text = "0 câu";
            }

            // 3. Tiền thưởng kỷ lục
            var topPrizeScore = _allScores.OrderByDescending(x => x.PrizeMoney).FirstOrDefault();
            if (topPrizeScore != null && topPrizeScore.PrizeMoney > 0)
            {
                txtTopPrizePlayer.Text = topPrizeScore.PlayerName;
                txtTopPrize.Text = topPrizeScore.PrizeFormatted;
            }
            else
            {
                txtTopPrizePlayer.Text = "Chưa có kỷ lục";
                txtTopPrize.Text = "0 VNĐ";
            }

            // 4. Tổng quan số lần phá đảo 15 câu & tổng số người chơi
            int totalPassed = HighScoreService.GetTotal15QuestionsPassedCount();
            int uniquePlayers = _allScores.Select(x => x.PlayerName.Trim().ToLower()).Distinct().Count();

            txtTotal15Passed.Text = $"{totalPassed} lần chinh phục 15/15";
            txtTotalPlayers.Text = $"{uniquePlayers} người chơi ({_allScores.Count} ván)";
        }

        private void ApplyFilter()
        {
            if (!_isLoaded || dgPlayerStats == null || dgGameScores == null) return;

            string keyword = txtSearch?.Text?.Trim().ToLower() ?? "";
            int sortIndex = cbSort?.SelectedIndex ?? 0;

            if (_currentTab == "PlayerStats")
            {
                string sortKey = sortIndex switch
                {
                    0 => "15Questions",
                    1 => "CorrectAnswers",
                    2 => "TotalCorrect",
                    3 => "Prize",
                    _ => "15Questions"
                };

                var stats = HighScoreService.GetPlayerStats(sortKey);
                var filteredStats = stats.Where(s =>
                    string.IsNullOrEmpty(keyword) || s.PlayerName.ToLower().Contains(keyword)
                ).ToList();

                for (int i = 0; i < filteredStats.Count; i++)
                {
                    filteredStats[i].Rank = i + 1;
                }

                dgPlayerStats.ItemsSource = filteredStats;
                dgPlayerStats.Visibility = Visibility.Visible;
                dgGameScores.Visibility = Visibility.Collapsed;
            }
            else
            {
                var sortedScores = sortIndex switch
                {
                    0 => _allScores.OrderByDescending(x => x.QuestionsAnswered >= 15 ? 1 : 0)
                                   .ThenByDescending(x => x.QuestionsAnswered)
                                   .ThenByDescending(x => x.PrizeMoney)
                                   .ToList(),
                    1 => _allScores.OrderByDescending(x => x.QuestionsAnswered)
                                   .ThenByDescending(x => x.PrizeMoney)
                                   .ToList(),
                    2 => _allScores.OrderByDescending(x => x.QuestionsAnswered)
                                   .ThenByDescending(x => x.DateAchieved)
                                   .ToList(),
                    3 => _allScores.OrderByDescending(x => x.PrizeMoney)
                                   .ThenByDescending(x => x.QuestionsAnswered)
                                   .ToList(),
                    _ => _allScores.ToList()
                };

                var filteredScores = sortedScores.Where(s =>
                    string.IsNullOrEmpty(keyword) || s.PlayerName.ToLower().Contains(keyword)
                ).ToList();

                for (int i = 0; i < filteredScores.Count; i++)
                {
                    filteredScores[i].Rank = i + 1;
                }

                dgGameScores.ItemsSource = filteredScores;
                dgGameScores.Visibility = Visibility.Visible;
                dgPlayerStats.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnTabPlayerStats_Click(object sender, RoutedEventArgs e)
        {
            _currentTab = "PlayerStats";
            btnTabPlayerStats.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A"));
            btnTabPlayerStats.Foreground = Brushes.White;
            btnTabPlayerStats.FontWeight = FontWeights.Bold;

            btnTabGameScores.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#131F35"));
            btnTabGameScores.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            btnTabGameScores.FontWeight = FontWeights.SemiBold;

            ApplyFilter();
        }

        private void BtnTabGameScores_Click(object sender, RoutedEventArgs e)
        {
            _currentTab = "GameScores";
            btnTabGameScores.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A"));
            btnTabGameScores.Foreground = Brushes.White;
            btnTabGameScores.FontWeight = FontWeights.Bold;

            btnTabPlayerStats.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#131F35"));
            btnTabPlayerStats.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            btnTabPlayerStats.FontWeight = FontWeights.SemiBold;

            ApplyFilter();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void CbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Bạn có chắc muốn khôi phục danh sách Bảng vàng về dữ liệu mẫu ban đầu?",
                "Xác nhận khôi phục",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                _allScores = HighScoreService.ResetDefaults();
                LoadData();
                MessageBox.Show("Đã khôi phục bảng vàng mẫu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnClearScores_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa TOÀN BỘ dữ liệu Bảng vàng không? Thao tác này không thể hoàn tác!",
                "Cảnh báo xóa dữ liệu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                HighScoreService.ClearScores();
                _allScores.Clear();
                LoadData();
                MessageBox.Show("Đã xóa sạch dữ liệu Bảng vàng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void dgPlayerStats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}