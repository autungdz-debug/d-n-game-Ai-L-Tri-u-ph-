using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using AiLaTrieuPhu.Models;

namespace AiLaTrieuPhu.Services
{
    public enum GameStatus
    {
        Menu,
        QuestionActive,
        SuspenseSelection,
        ResultRevealed,
        MilestoneCelebration,
        GameOver,
        Victory
    }

    public class AudienceVoteResult
    {
        public int PercentA { get; set; }
        public int PercentB { get; set; }
        public int PercentC { get; set; }
        public int PercentD { get; set; }
    }

    public class PhoneFriendResult
    {
        public string HelperName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string SuggestedAnswer { get; set; } = string.Empty;
        public int Confidence { get; set; }
        public string Dialogue { get; set; } = string.Empty;
    }

    public class ExpertAdvice
    {
        public string ExpertName { get; set; } = string.Empty;
        public string Expertise { get; set; } = string.Empty;
        public string SuggestedAnswer { get; set; } = string.Empty;
        public string Reasoning { get; set; } = string.Empty;
    }

    public class GameEngine
    {
        public string PlayerName { get; set; } = "Người chơi";
        public int CurrentLevel { get; private set; } = 1; // 1 to 15
        public Question? CurrentQuestion { get; private set; }
        public ObservableCollection<PrizeLevel> PrizeLadder { get; private set; } = new();

        public GameStatus Status { get; private set; } = GameStatus.Menu;
        public int RemainingTime { get; private set; } = 30;
        public bool IsTimerEnabled { get; set; } = true;

        public Dictionary<LifelineType, bool> LifelinesAvailable { get; private set; } = new();
        public HashSet<int> HiddenOptionIndices { get; private set; } = new();

        private readonly QuestionService _questionService;
        private readonly SoundService _soundService;
        private readonly DispatcherTimer _timer;
        private static readonly HashSet<int> _historyUsedQuestionIds = new();
        private readonly HashSet<int> _sessionQuestionIds = new();
        private List<Question> _currentGameQuestions = new();
        private readonly Random _random = new();

        // Events
        public event Action? OnStateChanged;
        public event Action<int>? OnTimerTick;
        public event Action? OnTimeOut;
        public event Action<Question>? OnNewQuestion;
        public event Action<int, bool>? OnAnswerResult; // (selectedIndex, isCorrect)
        public event Action<long>? OnGameFinished;

        public GameEngine(QuestionService questionService, SoundService soundService)
        {
            _questionService = questionService;
            _soundService = soundService;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            InitializePrizeLadder();
        }

        private void InitializePrizeLadder()
        {
            long[] prizes = {
                200000, 400000, 600000, 1000000, 2000000,
                3000000, 6000000, 10000000, 14000000, 22000000,
                30000000, 40000000, 60000000, 85000000, 150000000
            };

            PrizeLadder.Clear();
            // Stored in descending order (Level 15 on top, Level 1 at bottom for display)
            for (int i = 15; i >= 1; i--)
            {
                PrizeLadder.Add(new PrizeLevel
                {
                    Level = i,
                    Amount = prizes[i - 1],
                    IsMilestone = (i == 5 || i == 10 || i == 15),
                    IsCurrent = false,
                    IsPassed = false
                });
            }
        }

        public void StartNewGame(string playerName, bool withTimer = true)
        {
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Người chơi" : playerName.Trim();
            IsTimerEnabled = withTimer;
            CurrentLevel = 1;
            HiddenOptionIndices.Clear();
            _sessionQuestionIds.Clear();

            // Nạp lại dữ liệu câu hỏi mới nhất từ ngân hàng
            _questionService.ReloadQuestions();

            // Khởi tạo một bộ 15 câu hỏi hoàn toàn mới không trùng lặp các câu hỏi đã làm ở các ván trước
            _currentGameQuestions = _questionService.Generate15QuestionsSet(_historyUsedQuestionIds);

            foreach (var q in _currentGameQuestions)
            {
                _sessionQuestionIds.Add(q.Id);
            }

            LifelinesAvailable = new Dictionary<LifelineType, bool>
            {
                { LifelineType.FiftyFifty, true },
                { LifelineType.PhoneFriend, true },
                { LifelineType.AskAudience, true },
                { LifelineType.AskExperts, true },
                { LifelineType.SwitchQuestion, true }
            };

            UpdatePrizeLadderDisplay();
            LoadQuestionForCurrentLevel();
        }

        private void UpdatePrizeLadderDisplay()
        {
            foreach (var item in PrizeLadder)
            {
                item.IsCurrent = (item.Level == CurrentLevel);
                item.IsPassed = (item.Level < CurrentLevel);
            }
        }

        private void LoadQuestionForCurrentLevel()
        {
            HiddenOptionIndices.Clear();

            // Lấy câu hỏi ứng với cấp độ hiện tại trong bộ 15 câu đã khởi tạo
            CurrentQuestion = _currentGameQuestions.FirstOrDefault(q => q.Level == CurrentLevel);

            if (CurrentQuestion == null)
            {
                CurrentQuestion = _questionService.GetQuestionForLevel(CurrentLevel, _sessionQuestionIds);
                if (CurrentQuestion == null)
                {
                    CurrentQuestion = new Question
                    {
                        Id = 9999,
                        Level = CurrentLevel,
                        Content = $"Câu hỏi dự phòng số {CurrentLevel}?",
                        Options = new List<string> { "Phương án A", "Phương án B", "Phương án C", "Phương án D" },
                        CorrectIndex = 0
                    };
                }
                _sessionQuestionIds.Add(CurrentQuestion.Id);
            }

            Status = GameStatus.QuestionActive;
            UpdatePrizeLadderDisplay();

            ResetTimer();
            if (IsTimerEnabled)
            {
                _timer.Start();
            }

            OnNewQuestion?.Invoke(CurrentQuestion);
            OnStateChanged?.Invoke();
        }

        private void ResetTimer()
        {
            RemainingTime = CurrentLevel <= 5 ? 30 : (CurrentLevel <= 10 ? 45 : 60);
            OnTimerTick?.Invoke(RemainingTime);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (Status != GameStatus.QuestionActive) return;

            RemainingTime--;
            OnTimerTick?.Invoke(RemainingTime);
            if (RemainingTime <= 5 && RemainingTime > 0)
            {
                _soundService.PlayTick();
            }

            if (RemainingTime <= 0)
            {
                _timer.Stop();
                _soundService.PlayWrong();
                Status = GameStatus.GameOver;
                long guaranteed = GetGuaranteedPrize();
                HighScoreService.AddScore(PlayerName, guaranteed, CurrentLevel - 1);
                OnTimeOut?.Invoke();
                OnGameFinished?.Invoke(guaranteed);
                OnStateChanged?.Invoke();
            }
        }

        public void PauseTimer()
        {
            _timer.Stop();
        }

        public void ResumeTimer()
        {
            if (IsTimerEnabled && Status == GameStatus.QuestionActive)
            {
                _timer.Start();
            }
        }

        public void SelectAnswer(int answerIndex)
        {
            if (Status != GameStatus.QuestionActive) return;
            if (HiddenOptionIndices.Contains(answerIndex)) return;

            _timer.Stop();
            Status = GameStatus.SuspenseSelection;
            _soundService.PlaySelect();
            OnStateChanged?.Invoke();
        }

        public bool ConfirmAnswer(int answerIndex)
        {
            if (CurrentQuestion == null) return false;

            bool isCorrect = (answerIndex == CurrentQuestion.CorrectIndex);
            Status = GameStatus.ResultRevealed;

            if (isCorrect)
            {
                _soundService.PlayCorrect();
                OnAnswerResult?.Invoke(answerIndex, true);

                if (CurrentLevel == 15)
                {
                    // WON GRAND PRIZE!
                    Status = GameStatus.Victory;
                    _soundService.PlayVictory();
                    long wonPrize = GetPrizeForLevel(15);
                    HighScoreService.AddScore(PlayerName, wonPrize, 15);
                    OnGameFinished?.Invoke(wonPrize);
                }
                else if (CurrentLevel == 5 || CurrentLevel == 10)
                {
                    Status = GameStatus.MilestoneCelebration;
                    _soundService.PlayMilestone();
                }
            }
            else
            {
                _soundService.PlayWrong();
                Status = GameStatus.GameOver;
                long guaranteed = GetGuaranteedPrize();
                HighScoreService.AddScore(PlayerName, guaranteed, CurrentLevel - 1);
                OnAnswerResult?.Invoke(answerIndex, false);
                OnGameFinished?.Invoke(guaranteed);
            }

            OnStateChanged?.Invoke();
            return isCorrect;
        }

        public void AdvanceToNextLevel()
        {
            if (CurrentLevel < 15)
            {
                CurrentLevel++;
                LoadQuestionForCurrentLevel();
            }
        }

        public long WalkAway()
        {
            _timer.Stop();
            Status = GameStatus.GameOver;
            long walkAwayPrize = CurrentLevel > 1 ? GetPrizeForLevel(CurrentLevel - 1) : 0;
            HighScoreService.AddScore(PlayerName, walkAwayPrize, CurrentLevel - 1);
            OnGameFinished?.Invoke(walkAwayPrize);
            OnStateChanged?.Invoke();
            return walkAwayPrize;
        }

        public long GetPrizeForLevel(int level)
        {
            var match = PrizeLadder.FirstOrDefault(x => x.Level == level);
            return match?.Amount ?? 0;
        }

        public long GetCurrentPotentialPrize()
        {
            return GetPrizeForLevel(CurrentLevel);
        }

        public long GetGuaranteedPrize()
        {
            if (CurrentLevel > 10) return GetPrizeForLevel(10); // 22,000,000
            if (CurrentLevel > 5) return GetPrizeForLevel(5);   // 2,000,000
            return 0;
        }

        // --- LIFELINES ---

        public List<int> UseFiftyFifty()
        {
            if (!LifelinesAvailable.GetValueOrDefault(LifelineType.FiftyFifty, false)) return new();
            if (CurrentQuestion == null) return new();

            LifelinesAvailable[LifelineType.FiftyFifty] = false;
            _soundService.PlayLifeline();

            int correct = CurrentQuestion.CorrectIndex;
            var wrongIndices = new List<int> { 0, 1, 2, 3 };
            wrongIndices.Remove(correct);

            // Randomly select 2 wrong options to hide
            var toHide = wrongIndices.OrderBy(_ => _random.Next()).Take(2).ToList();
            foreach (var idx in toHide)
            {
                HiddenOptionIndices.Add(idx);
            }

            OnStateChanged?.Invoke();
            return toHide;
        }

        public AudienceVoteResult UseAskAudience()
        {
            if (!LifelinesAvailable.GetValueOrDefault(LifelineType.AskAudience, false))
                return new AudienceVoteResult();

            LifelinesAvailable[LifelineType.AskAudience] = false;
            _soundService.PlayLifeline();

            int correct = CurrentQuestion?.CorrectIndex ?? 0;

            // Accuracy depends on question level
            int correctPercent;
            if (CurrentLevel <= 5)
            {
                correctPercent = _random.Next(68, 88);
            }
            else if (CurrentLevel <= 10)
            {
                correctPercent = _random.Next(50, 72);
            }
            else
            {
                correctPercent = _random.Next(38, 56);
            }

            int remaining = 100 - correctPercent;
            int[] otherShares = new int[3];
            otherShares[0] = _random.Next(0, remaining / 2);
            otherShares[1] = _random.Next(0, remaining - otherShares[0]);
            otherShares[2] = remaining - otherShares[0] - otherShares[1];

            int[] finalP = new int[4];
            int otherIdx = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i == correct)
                {
                    finalP[i] = correctPercent;
                }
                else
                {
                    // If option was removed by 50:50, give it 0 or minimal votes
                    if (HiddenOptionIndices.Contains(i))
                    {
                        finalP[i] = 0;
                    }
                    else
                    {
                        finalP[i] = otherShares[otherIdx++];
                    }
                }
            }

            // Normalize sum to 100
            int sum = finalP.Sum();
            if (sum != 100 && sum > 0)
            {
                finalP[correct] += (100 - sum);
            }

            OnStateChanged?.Invoke();

            return new AudienceVoteResult
            {
                PercentA = finalP[0],
                PercentB = finalP[1],
                PercentC = finalP[2],
                PercentD = finalP[3]
            };
        }

        public PhoneFriendResult UsePhoneFriend(string helperName, string role)
        {
            if (!LifelinesAvailable.GetValueOrDefault(LifelineType.PhoneFriend, false))
                return new PhoneFriendResult();

            LifelinesAvailable[LifelineType.PhoneFriend] = false;
            _soundService.PlayLifeline();

            int correct = CurrentQuestion?.CorrectIndex ?? 0;
            string[] letterNames = { "A", "B", "C", "D" };

            // Determine if friend knows the answer
            int accuracyRate = CurrentLevel <= 5 ? 95 : (CurrentLevel <= 10 ? 80 : 60);
            bool givesCorrect = _random.Next(100) < accuracyRate;

            int chosenIndex = correct;
            if (!givesCorrect)
            {
                var candidates = new List<int> { 0, 1, 2, 3 };
                candidates.Remove(correct);
                candidates.RemoveAll(x => HiddenOptionIndices.Contains(x));
                if (candidates.Count > 0)
                {
                    chosenIndex = candidates[_random.Next(candidates.Count)];
                }
            }

            string suggested = letterNames[chosenIndex];
            string answerText = CurrentQuestion != null && chosenIndex < CurrentQuestion.Options.Count ? CurrentQuestion.Options[chosenIndex] : "";

            int confidence = givesCorrect ? _random.Next(85, 99) : _random.Next(50, 75);
            string dialogue;

            if (CurrentLevel <= 5)
            {
                dialogue = $"\"Chào bạn! Câu này dễ quá, theo mình chắc chắn là phương án {suggested}: '{answerText}'. Bạn cứ tự tin chọn nhé!\"";
            }
            else if (CurrentLevel <= 10)
            {
                dialogue = $"\"Alo! Mình nhớ câu này nằm trong tài liệu mình từng đọc. Mình nghiêng khoảng {confidence}% về phương án {suggested}: '{answerText}'. Hãy cân nhắc nhé!\"";
            }
            else
            {
                dialogue = $"\"Câu hỏi này tương đối hóc búa! Tuy nhiên dựa theo phân tích của mình, khả năng cao là {suggested}: '{answerText}'. Mình không chắc 100% nhưng nếu phải chọn thì mình chọn đáp án đó.\"";
            }

            OnStateChanged?.Invoke();

            return new PhoneFriendResult
            {
                HelperName = helperName,
                Role = role,
                SuggestedAnswer = suggested,
                Confidence = confidence,
                Dialogue = dialogue
            };
        }

        public List<ExpertAdvice> UseAskExperts()
        {
            if (!LifelinesAvailable.GetValueOrDefault(LifelineType.AskExperts, false))
                return new List<ExpertAdvice>();

            LifelinesAvailable[LifelineType.AskExperts] = false;
            _soundService.PlayLifeline();

            int correct = CurrentQuestion?.CorrectIndex ?? 0;
            string[] letters = { "A", "B", "C", "D" };

            var experts = new List<ExpertAdvice>
            {
                new ExpertAdvice { ExpertName = "TS. Nguyễn Minh Đức", Expertise = "Nhà nghiên cứu Lịch sử - Xã hội" },
                new ExpertAdvice { ExpertName = "ThS. Lê Thúy Hằng", Expertise = "Chuyên gia Khoa học tự nhiên" },
                new ExpertAdvice { ExpertName = "Nhà báo Trần Anh Vũ", Expertise = "Biên tập viên Văn hóa & Thể thao" }
            };

            foreach (var exp in experts)
            {
                // High level of expertise (85-95% correct)
                bool isRight = _random.Next(100) < 88;
                int pick = isRight ? correct : (correct + 1) % 4;
                if (HiddenOptionIndices.Contains(pick)) pick = correct;

                string optText = CurrentQuestion?.Options[pick] ?? "";
                exp.SuggestedAnswer = letters[pick];
                exp.Reasoning = isRight
                    ? $"Tôi đồng ý với phương án {letters[pick]} ({optText}). Các luận cứ khoa học và tài liệu lịch sử đều xác nhận điều này."
                    : $"Theo góc nhìn của tôi thì phương án {letters[pick]} khá hợp lý, tuy nhiên người chơi nên kết hợp phán đoán của mình.";
            }

            OnStateChanged?.Invoke();
            return experts;
        }

        public Question? UseSwitchQuestion()
        {
            if (!LifelinesAvailable.GetValueOrDefault(LifelineType.SwitchQuestion, false))
                return null;

            LifelinesAvailable[LifelineType.SwitchQuestion] = false;
            _soundService.PlayLifeline();

            var replacement = _questionService.GetSwitchQuestion(CurrentLevel, _sessionQuestionIds);
            if (replacement != null)
            {
                _sessionQuestionIds.Add(replacement.Id);
                _historyUsedQuestionIds.Add(replacement.Id);
                CurrentQuestion = replacement;

                int idx = _currentGameQuestions.FindIndex(q => q.Level == CurrentLevel);
                if (idx >= 0) _currentGameQuestions[idx] = replacement;

                HiddenOptionIndices.Clear();
                ResetTimer();
                if (IsTimerEnabled) _timer.Start();

                OnNewQuestion?.Invoke(CurrentQuestion);
                OnStateChanged?.Invoke();
            }

            return CurrentQuestion;
        }
    }
}