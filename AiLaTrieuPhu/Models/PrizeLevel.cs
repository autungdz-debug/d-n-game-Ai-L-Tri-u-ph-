using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AiLaTrieuPhu.Models
{
    public class PrizeLevel : INotifyPropertyChanged
    {
        public int Level { get; set; }
        public long Amount { get; set; }
        public string AmountFormatted => $"{Amount:N0} VNĐ";
        public bool IsMilestone { get; set; } // Mốc  5, 10, 15

        private bool _isCurrent;
        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                if (_isCurrent != value)
                {
                    _isCurrent = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isPassed;
        public bool IsPassed
        {
            get => _isPassed;
            set
            {
                if (_isPassed != value)
                {
                    _isPassed = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}