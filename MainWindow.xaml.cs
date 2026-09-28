using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Services;
using AiLaTrieuPhu.Views;

namespace AiLaTrieuPhu
{
    public partial class MainWindow : Window
    {
        private readonly QuestionService _questionService;
        private readonly SoundService _soundService;
        private readonly VoiceService _voiceService;
        private readonly GameEngine _engine;

        private Button[] _answerButtons = Array.Empty<Button>();
        private TextBlock[] _answerTexts = Array.Empty<TextBlock>();

        public MainWindow()
        {
            InitializeComponent();

            _questionService = new QuestionService();
            _soundService = new SoundService();
            _voiceService = new VoiceService();
            _engine = new GameEngine(_questionService, _soundService);

            _engine.OnTimerTick += Engine_OnTimerTick;
            _engine.OnTimeOut += Engine_OnTimeOut;
            _engine.OnNewQuestion += Engine_OnNewQuestion;
            _engine.OnAnswerResult += Engine_OnAnswerResult;
            _engine.OnGameFinished += Engine_OnGameFinished;
            _engine.OnStateChanged += Engine_OnStateChanged;

            icPrizeLadder.ItemsSource = _engine.PrizeLadder;

            Loaded += MainWindow_Loaded;
            Closing += (s, e) => _voiceService.Stop();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _answerButtons = new[] { btnAnswerA, btnAnswerB, btnAnswerC, btnAnswerD };
            _answerTexts = new[] { txtAnswerA, txtAnswerB, txtAnswerC, txtAnswerD };
        }

        // --- MENU ACTIONS ---

        private void BtnStartGame_Click(object sender, RoutedEventArgs e)
        {
            string name = txtPlayerName.Text?.Trim() ?? "Người chơi";
            bool withTimer = chkTimerEnabled.IsChecked ?? true;

            pnlMenu.Visibility = Visibility.Collapsed;
            pnlGame.Visibility = Visibility.Visible;
            pnlOverlayModal.Visibility = Visibility.Collapsed;

            _voiceService.Stop();
            _engine.StartNewGame(name, withTimer);
        }

        private void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            _soundService.IsMuted = !_soundService.IsMuted;
            txtMuteIcon.Text = _soundService.IsMuted ? "🔇 Âm thanh: Tắt" : "🔊 Âm thanh: Bật";
        }

        private void BtnToggleVoice_Click(object sender, RoutedEventArgs e)
        {
            _voiceService.IsVoiceEnabled = !_voiceService.IsVoiceEnabled;
            txtVoiceIcon.Text = _voiceService.IsVoiceEnabled ? "🎙️ Giọng đọc MC: Bật" : "🎙️ Giọng đọc MC: Tắt";
            txtVoiceIcon.Foreground = _voiceService.IsVoiceEnabled ? (Brush)FindResource("CyanBrush") : (Brush)FindResource("RedBrush");
            if (!_voiceService.IsVoiceEnabled)
            {
                _voiceService.Stop();
            }
        }

        private void BtnHowToPlay_Click(object sender, RoutedEventArgs e)
        {
            _engine.PauseTimer();
            var dlg = new HowToPlayDialog { Owner = this };
            dlg.ShowDialog();
            _engine.ResumeTimer();
        }

        private void BtnHighScores_Click(object sender, RoutedEventArgs e)
        {
            _engine.PauseTimer();
            var dlg = new HighScoreWindow { Owner = this };
            dlg.ShowDialog();
            _engine.ResumeTimer();
        }

        private void BtnQuestionManager_Click(object sender, RoutedEventArgs e)
        {
            _engine.PauseTimer();
            var dlg = new QuestionManagerWindow(_questionService) { Owner = this };
            dlg.ShowDialog();
            _engine.ResumeTimer();
        }

        // --- GAME ENGINE EVENTS ---

        private void Engine_OnNewQuestion(Question q)
        {
            Dispatcher.Invoke(() =>
            {
                _voiceService.Stop();

                txtCurrentLevelBadge.Text = $"{_engine.CurrentLevel} / 15";
                txtCurrentPrizeBadge.Text = $"{_engine.GetCurrentPotentialPrize():N0} đ";

                txtCategory.Text = $"Lĩnh vực: {q.Category}";
                txtQuestionMeta.Text = _engine.CurrentLevel switch
                {
                    5 => "⭐ MỐC QUAN TRỌNG SỐ 1 (BẢO TOÀN 2.000.000 Đ)",
                    10 => "⭐ MỐC QUAN TRỌNG SỐ 2 (BẢO TOÀN 22.000.000 Đ)",
                    15 => "🏆 CÂU HỎI CUỐI CÙNG - GIẢI THƯỞNG 150.000.000 Đ!",
                    _ => $"Câu hỏi số {_engine.CurrentLevel} hướng tới giải {_engine.GetCurrentPotentialPrize():N0} đ"
                };

                txtQuestionContent.Text = q.Content;

                for (int i = 0; i < 4; i++)
                {
                    _answerTexts[i].Text = (i < q.Options.Count) ? q.Options[i] : "";
                    ResetButtonAppearance(_answerButtons[i]);
                    _answerButtons[i].IsEnabled = true;
                }

                txtStatusMessage.Text = "Hãy chọn phương án bạn cho là chính xác...";
                btnNextQuestion.Visibility = Visibility.Collapsed;
                btnWalkAway.IsEnabled = true;

                UpdateLifelineButtons();

                // MC đọc câu hỏi và 4 phương án
                _voiceService.SpeakQuestion(_engine.CurrentLevel, q.Content, q.Options);
            });
        }

        private void Engine_OnTimerTick(int remainingSeconds)
        {
            Dispatcher.Invoke(() =>
            {
                pbTimer.Maximum = _engine.CurrentLevel <= 5 ? 30 : (_engine.CurrentLevel <= 10 ? 45 : 60);
                pbTimer.Value = remainingSeconds;
                txtTimer.Text = $"{remainingSeconds}s";

                if (remainingSeconds <= 5)
                {
                    pbTimer.Foreground = (Brush)FindResource("RedBrush");
                    txtTimer.Foreground = (Brush)FindResource("RedBrush");
                }
                else
                {
                    pbTimer.Foreground = (Brush)FindResource("GreenBrush");
                    txtTimer.Foreground = Brushes.White;
                }
            });
        }

        private void Engine_OnTimeOut()
        {
            Dispatcher.Invoke(() =>
            {
                txtStatusMessage.Text = "Hết thời gian! Rất tiếc bạn đã phải dừng cuộc chơi.";
                long guaranteed = _engine.GetGuaranteedPrize();
                _voiceService.SpeakTimeOut(_engine.PlayerName, guaranteed);
            });
        }

        private void Engine_OnAnswerResult(int selectedIndex, bool isCorrect)
        {
            Dispatcher.Invoke(() =>
            {
                if (isCorrect)
                {
                    SetButtonCorrect(_answerButtons[selectedIndex]);

                    if (_engine.CurrentLevel == 5)
                    {
                        txtStatusMessage.Text = "⭐ XUẤT SẮC! BẠN ĐÃ VƯỢT QUA MỐC SỐ 5 (BẢO TOÀN 2.000.000 Đ)!";
                        _voiceService.SpeakMilestone(5, _engine.PlayerName, 2000000);
                    }
                    else if (_engine.CurrentLevel == 10)
                    {
                        txtStatusMessage.Text = "⭐ TUYỆT VỜI! BẠN ĐÃ CHINH PHỤC MỐC SỐ 10 (BẢO TOÀN 22.000.000 Đ)!";
                        _voiceService.SpeakMilestone(10, _engine.PlayerName, 22000000);
                    }
                    else if (_engine.CurrentLevel < 15)
                    {
                        txtStatusMessage.Text = "CHÍNH XÁC! Chúc mừng bạn!";
                        _voiceService.SpeakCorrect(_engine.CurrentLevel);
                    }

                    if (_engine.CurrentLevel < 15)
                    {
                        btnNextQuestion.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    SetButtonWrong(_answerButtons[selectedIndex]);
                    if (_engine.CurrentQuestion != null)
                    {
                        SetButtonCorrect(_answerButtons[_engine.CurrentQuestion.CorrectIndex]);
                    }
                    txtStatusMessage.Text = "RẤT TIẾC! Đáp án chưa chính xác. Xin chia buồn cùng bạn!";
                }
            });
        }

        private void Engine_OnGameFinished(long finalPrize)
        {
            Dispatcher.Invoke(() =>
            {
                if (_engine.Status == GameStatus.Victory)
                {
                    txtModalIcon.Text = "🏆";
                    txtModalTitle.Text = "🎉 BẠN ĐÃ TRỞ THÀNH TÂN TRIỆU PHÚ!";
                    txtModalSubtitle.Text = $"Xin nhiệt liệt chúc mừng {_engine.PlayerName} đã xuất sắc chinh phục trọn vẹn 15 câu hỏi của Ai Là Triệu Phú!";
                    _voiceService.SpeakMilestone(15, _engine.PlayerName, 150000000);
                }
                else
                {
                    txtModalIcon.Text = "💔";
                    txtModalTitle.Text = "RẤT TIẾC - CHIA BUỒN CÙNG BẠN!";
                    if (finalPrize > 0)
                    {
                        txtModalSubtitle.Text = $"Phương án bạn chọn chưa chính xác. Xin chia buồn cùng {_engine.PlayerName}, nhưng bạn đã bảo toàn được giải thưởng an toàn của chương trình!";
                    }
                    else
                    {
                        txtModalSubtitle.Text = $"Phương án bạn chọn chưa chính xác. Xin chia buồn cùng {_engine.PlayerName} và chúc bạn may mắn hơn ở lần chơi tiếp theo!";
                    }
                    _voiceService.SpeakWrong(_engine.CurrentLevel, _engine.PlayerName, finalPrize);
                }

                txtModalPrize.Text = $"{finalPrize:N0} đ";
                pnlOverlayModal.Visibility = Visibility.Visible;
            });
        }

        private void Engine_OnStateChanged()
        {
            Dispatcher.Invoke(() =>
            {
                UpdateLifelineButtons();
            });
        }

        // --- ANSWER SELECTION & SUSPENSE FLOW ---

        private async void BtnAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int chosenIndex = int.Parse(btn.Tag.ToString()!);
                if (_engine.HiddenOptionIndices.Contains(chosenIndex)) return;

                // Lock all answers
                foreach (var b in _answerButtons) b.IsEnabled = false;
                btnWalkAway.IsEnabled = false;

                // Step 1: Select suspense state (amber/orange)
                _voiceService.Stop();
                _engine.SelectAnswer(chosenIndex);
                SetButtonPending(btn);
                txtStatusMessage.Text = $"Bạn đã chọn phương án {((char)('A' + chosenIndex))}! Chờ kết quả chốt...";
                _voiceService.SpeakConfirming(chosenIndex);

                // Suspense wait (2 seconds like TV show!)
                await Task.Delay(2000);

                // Step 2: Confirm answer
                _engine.ConfirmAnswer(chosenIndex);
            }
        }

        private void BtnNextQuestion_Click(object sender, RoutedEventArgs e)
        {
            _voiceService.Stop();
            _engine.AdvanceToNextLevel();
        }

        private void BtnWalkAway_Click(object sender, RoutedEventArgs e)
        {
            _voiceService.Stop();
            long prizeToKeep = _engine.CurrentLevel > 1 ? _engine.GetPrizeForLevel(_engine.CurrentLevel - 1) : 0;
            var confirm = MessageBox.Show($"Bạn có chắc chắn muốn dừng cuộc chơi tại câu số {_engine.CurrentLevel} để bảo toàn số tiền {prizeToKeep:N0} đ?",
                "Xác nhận dừng cuộc chơi", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                long walkPrize = _engine.WalkAway();
                _voiceService.SpeakWalkAway(_engine.PlayerName, walkPrize);
            }
        }

        // --- LIFELINES ACTIONS ---

        private void UpdateLifelineButtons()
        {
            btnLifeline5050.IsEnabled = _engine.LifelinesAvailable.GetValueOrDefault(LifelineType.FiftyFifty, false);
            btnLifelinePhone.IsEnabled = _engine.LifelinesAvailable.GetValueOrDefault(LifelineType.PhoneFriend, false);
            btnLifelineAudience.IsEnabled = _engine.LifelinesAvailable.GetValueOrDefault(LifelineType.AskAudience, false);
            btnLifelineExperts.IsEnabled = _engine.LifelinesAvailable.GetValueOrDefault(LifelineType.AskExperts, false);
            btnLifelineSwitch.IsEnabled = _engine.LifelinesAvailable.GetValueOrDefault(LifelineType.SwitchQuestion, false);

            // Hide eliminated 50:50 options
            foreach (int hiddenIdx in _engine.HiddenOptionIndices)
            {
                _answerButtons[hiddenIdx].IsEnabled = false;
                _answerButtons[hiddenIdx].Opacity = 0.15;
            }
        }

        private void BtnLifeline5050_Click(object sender, RoutedEventArgs e)
        {
            var hidden = _engine.UseFiftyFifty();
            foreach (int idx in hidden)
            {
                _answerButtons[idx].IsEnabled = false;
                _answerButtons[idx].Opacity = 0.15;
            }
            txtStatusMessage.Text = "Máy tính đã loại bỏ 2 phương án sai!";
        }

        private void BtnLifelinePhone_Click(object sender, RoutedEventArgs e)
        {
            _engine.PauseTimer();
            var dlg = new PhoneDialog(_engine) { Owner = this };
            dlg.ShowDialog();
            _engine.ResumeTimer();
        }

        private void BtnLifelineAudience_Click(object sender, RoutedEventArgs e)
        {
            _engine.PauseTimer();
            var result = _engine.UseAskAudience();
            var dlg = new AudienceDialog(result) { Owner = this };
            dlg.ShowDialog();
            _engine.ResumeTimer();
        }

        private void BtnLifelineExperts_Click(object sender, RoutedEventArgs e)
        {
            _engine.PauseTimer();
            var experts = _engine.UseAskExperts();
            var dlg = new ExpertsDialog(experts) { Owner = this };
            dlg.ShowDialog();
            _engine.ResumeTimer();
        }

        private void BtnLifelineSwitch_Click(object sender, RoutedEventArgs e)
        {
            var newQ = _engine.UseSwitchQuestion();
            if (newQ != null)
            {
                txtStatusMessage.Text = "Đã đổi sang câu hỏi mới cùng cấp độ!";
            }
        }

        // --- VISUAL STYLING HELPERS ---

        private void ResetButtonAppearance(Button btn)
        {
            btn.Background = (Brush)FindResource("AnswerDefaultBrush");
            btn.BorderBrush = (Brush)FindResource("CyanBrush");
            btn.Opacity = 1.0;
        }

        private void SetButtonPending(Button btn)
        {
            btn.Background = (Brush)FindResource("OrangeBrush");
            btn.BorderBrush = (Brush)FindResource("GoldLightBrush");
        }

        private void SetButtonCorrect(Button btn)
        {
            btn.Background = (Brush)FindResource("GreenGradientBrush");
            btn.BorderBrush = (Brush)FindResource("GreenBrush");
        }

        private void SetButtonWrong(Button btn)
        {
            btn.Background = (Brush)FindResource("RedGradientBrush");
            btn.BorderBrush = (Brush)FindResource("RedBrush");
        }

        // --- MODAL ACTIONS ---

        private void BtnPlayAgain_Click(object sender, RoutedEventArgs e)
        {
            _voiceService.Stop();
            pnlOverlayModal.Visibility = Visibility.Collapsed;
            string name = txtPlayerName.Text?.Trim() ?? "Người chơi";
            bool withTimer = chkTimerEnabled.IsChecked ?? true;
            _engine.StartNewGame(name, withTimer);
        }

        private void BtnBackToMenu_Click(object sender, RoutedEventArgs e)
        {
            _voiceService.Stop();
            pnlOverlayModal.Visibility = Visibility.Collapsed;
            pnlGame.Visibility = Visibility.Collapsed;
            pnlMenu.Visibility = Visibility.Visible;
        }
    }
}