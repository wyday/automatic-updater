using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TestAppWPF
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();

            automaticUpdater2.MenuItem = mnuCheckForUpdates;
        }
    }
}
