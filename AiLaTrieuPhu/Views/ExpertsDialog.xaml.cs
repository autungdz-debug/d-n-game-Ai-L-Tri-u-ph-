using System.Collections.Generic;
using System.Windows;
using AiLaTrieuPhu.Services;

namespace AiLaTrieuPhu.Views
{
    public partial class ExpertsDialog : Window
    {
        public ExpertsDialog(List<ExpertAdvice> experts)
        {
            InitializeComponent();
            icExperts.ItemsSource = experts;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}