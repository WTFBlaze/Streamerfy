using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace Streamerfy.Windows
{
    public partial class InfoDialog : Window
    {
        private bool _isClosing = false;

        public InfoDialog(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Play fade-in animation
            var fadeIn = (Storyboard)FindResource("FadeInStoryboard");
            fadeIn.Begin(this);

            // Auto-close after 10 seconds
            await Task.Delay(TimeSpan.FromSeconds(10));
            if (!_isClosing) CloseWithAnimation();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            CloseWithAnimation();
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseWithAnimation()
        {
            _isClosing = true;
            var fadeOut = (Storyboard)FindResource("FadeOutStoryboard");
            fadeOut.Begin(this);
        }

        private void FadeOutStoryboard_Completed(object? sender, EventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                CloseWithAnimation();
            }
        }
    }
}
