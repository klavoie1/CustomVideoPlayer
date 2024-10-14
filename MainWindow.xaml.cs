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

namespace CustomVideoPlayer
{
    public partial class MainWindow : Window
    {

        private DispatcherTimer timer; // Timer to update the duration slider
        private bool isSeeking = false; // Track if user is interacting with the slider
        private bool isTimerUpdate = false; // Track if the slider update is from the timer

        private bool isFullscreen = false; // Track fullscreen state
        private WindowStyle previousWindowStyle;
        private WindowState previousWindowState;

        public MainWindow()
        {
            InitializeComponent();
            volumeSlider.Value = 0.3; // Set default volume to 30%
            mediaElement.Volume = volumeSlider.Value; // Sync initial volume

            // Initialize DispatcherTimer for updating the duration slider
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500); // Update every 500ms
            timer.Tick += Timer_Tick;
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
                timer.Start(); // Start updating the duration slider
            }
            else
            {
                MessageBox.Show("That file is not in the accessible formats!");
            }
        }
        private void btnPlay_Click(Object sender, RoutedEventArgs e)
        {
            mediaElement.Play();
        }

        private void btnPause_Click(Object sender, RoutedEventArgs e)
        {
            mediaElement.Pause();
        }

        private void volumeSlider_Click(Object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Adjust volume based on Slider value
            mediaElement.Volume = volumeSlider.Value;
        }

        private void durationSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isSeeking = true; // Stop the timer updates while the user interacts with the slider
        }

        private void durationSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isSeeking = false; // Resume the timer updates
            mediaElement.Position = TimeSpan.FromSeconds(durationSlider.Value); // Seek to new position
        }

        private void durationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Prevent recursion: Only handle user-initiated value changes
            if (!isSeeking && !isTimerUpdate && mediaElement.NaturalDuration.HasTimeSpan)
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
        }

        private void btnFullscreen_Click(object sender, RoutedEventArgs e)
        {
            EnterFullscreen();
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
    }

}