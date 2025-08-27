using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GroupWallViewer.View.Windows
{
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool displayUserIcons = Properties.Settings.Default.DisplayUserIcons;
        public bool DisplayUserIcons
        {
            get { return displayUserIcons; }
            set { displayUserIcons = value; OnPropertyChanged(); }
        }
        public SettingsWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void DisplayUserIconsOn(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DisplayUserIcons = true;
            Properties.Settings.Default.Save();
        }
        private void DisplayUserIconsOff(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DisplayUserIcons = false;
            Properties.Settings.Default.Save();
        }
    }
}
