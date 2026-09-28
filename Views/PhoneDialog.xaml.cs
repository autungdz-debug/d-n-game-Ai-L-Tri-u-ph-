using System;
using System.Windows;
using System.Windows.Controls;
using AiLaTrieuPhu.Services;

namespace AiLaTrieuPhu.Views
{
    public partial class PhoneDialog : Window
    {
        private readonly GameEngine _engine;
        public PhoneDialog(GameEngine engine)
        {
            InitializeComponent();
            _engine = engine;
        }

        private void Contact_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string tag = btn.Tag.ToString()!;
                string name = tag switch
                {
                    "1" => "Bác sĩ Lê Hoàng Long",
                    "2" => "GS. Trần Văn Khoa",
                    "3" => "Kỹ sư Phạm Thu Hằng",
                    _ => "Minh Tuấn (Bạn thân)"
                };

                string role = tag switch
                {
                    "1" => "Bác sĩ",
                    "2" => "Giáo sư",
                    "3" => "Kỹ sư",
                    _ => "Bạn thân"
                };

                var result = _engine.UsePhoneFriend(name, role);

                pnlSelectContact.Visibility = Visibility.Collapsed;
                pnlInCall.Visibility = Visibility.Visible;

                txtCallingName.Text = result.HelperName;
                txtDialogue.Text = result.Dialogue;
                txtSuggested.Text = result.SuggestedAnswer;
                txtConfidence.Text = $"{result.Confidence}%";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}