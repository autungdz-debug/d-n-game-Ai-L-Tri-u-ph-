using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Services;

namespace AiLaTrieuPhu.Views
{
    public partial class QuestionManagerWindow : Window
    {
        private readonly QuestionService _questionService;
        private List<Question> _allQuestions = new();

        public QuestionManagerWindow(QuestionService questionService)
        {
            InitializeComponent();
            _questionService = questionService;

            InitializeFilters();
            RefreshGrid();
        }

        private void InitializeFilters()
        {
            cbLevelFilter.Items.Add("Tất cả cấp độ");
            for (int i = 1; i <= 15; i++)
            {
                cbLevelFilter.Items.Add($"Câu hỏi số {i}");
                cbNewLevel.Items.Add(i);
            }
            cbLevelFilter.SelectedIndex = 0;
            cbNewLevel.SelectedIndex = 0;
        }

        private void RefreshGrid()
        {
            _allQuestions = _questionService.GetAllQuestions();
            txtTotalCount.Text = $"(Tổng cộng: {_allQuestions.Count} câu)";

            string search = txtSearch.Text?.Trim().ToLower() ?? "";
            int selectedLevel = cbLevelFilter.SelectedIndex;

            var filtered = _allQuestions.Where(q =>
            {
                bool matchLevel = (selectedLevel == 0) || (q.Level == selectedLevel);
                bool matchSearch = string.IsNullOrEmpty(search) ||
                                   q.Content.ToLower().Contains(search) ||
                                   q.Category.ToLower().Contains(search);
                return matchLevel && matchSearch;
            }).ToList();

            dgQuestions.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => RefreshGrid();

        private void CbLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshGrid();

        private void BtnAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            pnlAddForm.Visibility = Visibility.Visible;
        }

        private void BtnCancelAdd_Click(object sender, RoutedEventArgs e)
        {
            pnlAddForm.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveNewQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewContent.Text) ||
                string.IsNullOrWhiteSpace(txtOptA.Text) ||
                string.IsNullOrWhiteSpace(txtOptB.Text) ||
                string.IsNullOrWhiteSpace(txtOptC.Text) ||
                string.IsNullOrWhiteSpace(txtOptD.Text))
            {
                MessageBox.Show("Vui lòng điền đầy đủ câu hỏi và 4 phương án A, B, C, D!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int level = cbNewLevel.SelectedItem is int lv ? lv : 1;
            int correctIndex = cbNewCorrect.SelectedIndex;

            var newQ = new Question
            {
                Content = txtNewContent.Text.Trim(),
                Options = new List<string> { txtOptA.Text.Trim(), txtOptB.Text.Trim(), txtOptC.Text.Trim(), txtOptD.Text.Trim() },
                CorrectIndex = correctIndex,
                Level = level,
                Category = string.IsNullOrWhiteSpace(txtNewCategory.Text) ? "Tổng hợp" : txtNewCategory.Text.Trim()
            };

            _questionService.AddQuestion(newQ);
            MessageBox.Show("Đã thêm câu hỏi thành công vào ngân hàng!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            txtNewContent.Clear();
            txtOptA.Clear();
            txtOptB.Clear();
            txtOptC.Clear();
            txtOptD.Clear();
            pnlAddForm.Visibility = Visibility.Collapsed;

            RefreshGrid();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Question q)
            {
                var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa câu hỏi: \"{q.Content}\"?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.Yes)
                {
                    _questionService.DeleteQuestion(q.Id);
                    RefreshGrid();
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}