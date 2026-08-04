using System.Windows;

namespace TeamActivity.Agent.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new AgentViewModel();
    }
}
