using System.Windows;

namespace AiLaTrieuPhu.Views
{
    public partial class HowToPlayDialog : Window
    {
        public HowToPlayDialog()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}