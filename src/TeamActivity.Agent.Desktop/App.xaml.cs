using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace TeamActivity.Agent.Desktop;

public partial class App : System.Windows.Application
{
    private NotifyIcon? trayIcon;
    private MainWindow? window;
    private bool isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        window = new MainWindow();
        window.Closing += (_, args) =>
        {
            if (isExiting) return;
            args.Cancel = true;
            window.Hide();
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open TeamActivity", null, (_, _) => ShowWindow());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());
        trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Text = "TeamActivity — tracking is paused",
            Visible = true,
            ContextMenuStrip = menu
        };
        trayIcon.DoubleClick += (_, _) => ShowWindow();
        window.Show();
    }

    private void ShowWindow()
    {
        if (window is null) return;
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }

    private void ExitApplication()
    {
        isExiting = true;
        trayIcon?.Dispose();
        window?.Close();
        Shutdown();
    }
}
