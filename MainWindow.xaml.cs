using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;

namespace CustomVideoPlayer
{

    public static class MediaElementExtensions
    {
        // This helper method checks if the media is currently playing
        public static bool IsPlaying(this MediaElement mediaElement)
        {
            // We assume media is playing if its Position is advancing and it's not paused.
            return mediaElement.Clock != null || mediaElement.LoadedBehavior == MediaState.Play;
        }
    }



    public partial class MainWindow : Window
    {

        private bool isSeeking = false; // Track if user is interacting with the slider
        private bool isTimerUpdate = false; // Track if the slider update is from the timer

        private bool isFullscreen = false; // Track fullscreen state
        private WindowStyle previousWindowStyle;
        private WindowState previousWindowState;

        private bool isPlaying = false; // Starts the video not playing 

        private DispatcherTimer durationTimer; // Timer for updating duration slider
        private DispatcherTimer hideControlsTimer; // Timer for hiding the controls overlay

        private DispatcherTimer durationLabelTimer;


        public MainWindow()
        {
            InitializeComponent();
            volumeSlider.Value = 0.3; // Set default volume to 30%
            mediaElement.Volume = volumeSlider.Value; // Sync initial volume

            // Initialize DispatcherTimer for updating the duration slider
            durationTimer = new DispatcherTimer();
            durationTimer.Interval = TimeSpan.FromMilliseconds(500); // Update every 500ms
            durationTimer.Tick += Timer_Tick;

            // Initialize the timer to hide controls
            hideControlsTimer = new DispatcherTimer();
            hideControlsTimer.Interval = TimeSpan.FromSeconds(1); // Hide after 1 seconds of inactivity
            hideControlsTimer.Tick += HideControlsTimer_Tick;

            // Start listening for mouse events to show/hide controls
            mediaElement.MouseMove += MediaElement_MouseMove;
            overlayPanel.MouseEnter += OverlayPanel_MouseEnter;
            overlayPanel.MouseLeave += OverlayPanel_MouseLeave;

            durationLabelTimer = new DispatcherTimer();
            durationLabelTimer.Interval = TimeSpan.FromSeconds(1);
            durationLabelTimer.Tick += Timer_Tick;
        }

        private void btnOpen_Click(Object sender, RoutedEventArgs e)
        {
            // Opens file dialong to select the file
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Video Files | *.mp4; *.avi; *.mkv; *.wmv; *.mov",
                Title = "Select a Video File"
            };

            if (ofd.ShowDialog() == true)
            {
                mediaElement.Source = new Uri(ofd.FileName);
                mediaElement.Play();
                durationTimer.Start(); // Start updating the duration slider
                ShowControls(); // Show controls when a new video starts
            }
        }



        private void btnPause_Click(Object sender, RoutedEventArgs e)
        {
            mediaElement.Pause();
            durationLabelTimer?.Stop();
        }

        private void volumeSlider_Click(Object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Adjust volume based on Slider value
            mediaElement.Volume = volumeSlider.Value;
        }

        private void durationSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isSeeking = true; // Stop the timer updates while the user interacts with the slider
            durationLabelTimer.Stop();
        }

        private void durationSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isSeeking = false; // Resume the timer updates
            mediaElement.Position = TimeSpan.FromSeconds(durationSlider.Value); // Seek to new position
            durationLabelTimer.Start();
        }

        private void durationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan && durationSlider.IsMouseOver)
            {
                mediaElement.Position = TimeSpan.FromSeconds(durationSlider.Value);
            }

            
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan && !isSeeking)
            {
                // Update the slider from the timer
                isTimerUpdate = true;
                durationSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                durationSlider.Value = mediaElement.Position.TotalSeconds;
                isTimerUpdate = false;
            }

            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                // Update the elapsed time
                TimeSpan currentTime = mediaElement.Position;
                elapsedTime.Text = FormatTime(currentTime);

                // Update the remaining time
                TimeSpan remaining = mediaElement.NaturalDuration.TimeSpan - currentTime;
                remainingTime.Text = $"-{FormatTime(remaining)}";

                // Update the slider value based on the current position
                durationSlider.Value = currentTime.TotalSeconds;
                durationSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            }
        }

        // Helper method to format the time (hh:mm:ss)
        private string FormatTime(TimeSpan time)
        {
            return time.ToString(time.Hours > 0 ? @"hh\:mm\:ss" : @"mm\:ss");
        }

        private void btnFullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (isFullscreen == true)
            {
                ExitFullscreen();
            }
            else
            {
                EnterFullscreen();
            }
        }

        private void EnterFullscreen()
        {
            if (!isFullscreen)
            {
                // Save the window state and make it fullscreen
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                isFullscreen = true;
            }
        }

        private void ExitFullscreen()
        {
            if (isFullscreen)
            {
                // Restore the window to normal state
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
                isFullscreen = false;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && isFullscreen)
            {
                ExitFullscreen();
            }
        }

        private void btnPlay_Click(Object sender, RoutedEventArgs e)
        {
            mediaElement.Play();
            durationLabelTimer.Start();
        }


        // Show controls when the mouse moves over the video
        private void MediaElement_MouseMove(object sender, MouseEventArgs e)
        {
            ShowControls();
            hideControlsTimer.Stop(); // Reset the hide timer
            hideControlsTimer.Start(); // Start the hide timer again
        }

        // Ensure controls stay visible when interacting with them
        private void OverlayPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            hideControlsTimer.Stop(); // Stop the timer while hovering on the controls
        }

        private void OverlayPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            hideControlsTimer.Start(); // Restart the timer when leaving the controls
        }

        // Hide the controls when the timer elapses
        private void HideControlsTimer_Tick(object sender, EventArgs e)
        {
            overlayPanel.Visibility = Visibility.Collapsed;
            hideControlsTimer.Stop(); // Stop the timer until the next interaction
        }

        // Helper to show the controls
        private void ShowControls()
        {
            overlayPanel.Visibility = Visibility.Visible;
        }

        
        













    }
}