using System.Windows;
using YJ_AutoUnClamp.ViewModels;

namespace YJ_AutoUnClamp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindow_ViewModel();
        }
    }
}