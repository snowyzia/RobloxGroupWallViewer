using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace GroupWallViewer.View.UserControls
{
    public partial class WallPost : UserControl, INotifyPropertyChanged
    {
        public WallPost()
        {
            DataContext = this;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private string username;

        public string Username
        {
            get { return username; }
            set { username = value; OnPropertyChanged(); }
        }

        private string wallText;

        public string WallText
        {
            get { return wallText; }
            set { wallText = value; OnPropertyChanged(); }
        }

        private string additionalInformation;

        public string AdditionalInformation
        {
            get { return additionalInformation; }
            set { additionalInformation = value; OnPropertyChanged(); }
        }


        private void OnPropertyChanged( [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
