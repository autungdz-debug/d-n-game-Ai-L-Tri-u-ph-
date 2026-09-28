using System;
using System.Windows;
using System.Windows.Media.Animation;
using AiLaTrieuPhu.Services;

namespace AiLaTrieuPhu.Views
{
    public partial class AudienceDialog : Window
    {
        public AudienceDialog(AudienceVoteResult result)
        {
            InitializeComponent();
            Loaded += (s, e) => AnimateBars(result);
        }

        private void AnimateBars(AudienceVoteResult result)
        {
            txtPercentA.Text = $"{result.PercentA}%";
            txtPercentB.Text = $"{result.PercentB}%";
            txtPercentC.Text = $"{result.PercentC}%";
            txtPercentD.Text = $"{result.PercentD}%";

            double maxAvailableHeight = 160;
            AnimateHeight(barA, (result.PercentA / 100.0) * maxAvailableHeight);
            AnimateHeight(barB, (result.PercentB / 100.0) * maxAvailableHeight);
            AnimateHeight(barC, (result.PercentC / 100.0) * maxAvailableHeight);
            AnimateHeight(barD, (result.PercentD / 100.0) * maxAvailableHeight);
        }

        private void AnimateHeight(FrameworkElement element, double targetHeight)
        {
            var anim = new DoubleAnimation
            {
                From = 5,
                To = Math.Max(5, targetHeight),
                Duration = TimeSpan.FromMilliseconds(900),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            element.BeginAnimation(FrameworkElement.HeightProperty, anim);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}