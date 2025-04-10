using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Streamerfy.Windows
{
    public partial class ConfirmDialog : Window
    {
        public bool Result { get; private set; } = false;
        private bool _isClosing = false;
        private DispatcherTimer _autoCloseTimer;
        private TimeSpan _autoCloseDelay = TimeSpan.FromSeconds(10);

        public ConfirmDialog(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = (Storyboard)FindResource("FadeInStoryboard");
            fadeIn.Begin(this);

            // Start auto-close countdown
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = _autoCloseDelay
            };
            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
            _autoCloseTimer.Start();
        }

        private void AutoCloseTimer_Tick(object? sender, EventArgs e)
        {
            _autoCloseTimer.Stop();
            Result = false; // Default to "No" if no input
            CloseWithAnimation();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer.Stop();
            Result = true;
            CloseWithAnimation();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer.Stop();
            Result = false;
            CloseWithAnimation();
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

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
