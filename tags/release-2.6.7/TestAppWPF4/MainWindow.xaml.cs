using System.Windows;

namespace TestAppWPF4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            automaticUpdater1.MenuItem = mnuCheckForUpdates;
        }
    }
}