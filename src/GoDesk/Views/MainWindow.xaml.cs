using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GoDesk.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private Border? _activeNav;

    public MainWindow()
    {
        InitializeComponent();
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
        _activeNav = NavHome;
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        SystemTime.Text = now.ToString("hh:mm tt");
        SystemDate.Text = now.ToString("ddd, MMM dd");
        TrayTime.Text = now.ToString("hh:mm tt");
    }

    private static void StyleNav(Border nav, bool active)
    {
        if (nav.Child is StackPanel sp)
        {
            foreach (var item in sp.Children)
            {
                if (item is TextBlock tb)
                    tb.Foreground = active ? (Brush)Application.Current.FindResource("Accent") : (Brush)Application.Current.FindResource("TextSecondary");
            }
        }
        nav.Background = active ? (Brush)Application.Current.FindResource("BgLight") : Brushes.Transparent;
    }

    private void SetActiveNav(Border nav)
    {
        if (_activeNav != null) StyleNav(_activeNav, false);
        _activeNav = nav;
        StyleNav(nav, true);
    }

    private void NavigateTo(UserControl view) { MainContent.Content = view; }

    private void NavHome_Click(object s, MouseButtonEventArgs e) { SetActiveNav(NavHome); NavigateTo(new HomeView()); }
    private void NavApps_Click(object s, MouseButtonEventArgs e) { SetActiveNav(NavApps); NavigateTo(new AppsView()); }
    private void NavGames_Click(object s, MouseButtonEventArgs e) { SetActiveNav(NavGames); NavigateTo(new GamesView()); }
    private void NavGameBar_Click(object s, MouseButtonEventArgs e) { SetActiveNav(NavGameBar); NavigateTo(new GameBarView()); }
    private void NavGoConsole_Click(object s, MouseButtonEventArgs e) { SetActiveNav(NavGoConsole); NavigateTo(new GoConsoleModeView()); }

    private void TaskbarHome_Click(object s, MouseButtonEventArgs e) => NavHome_Click(s, e);
    private void TaskbarApps_Click(object s, MouseButtonEventArgs e) => NavApps_Click(s, e);
    private void TaskbarGames_Click(object s, MouseButtonEventArgs e) => NavGames_Click(s, e);
    private void TaskbarGameBar_Click(object s, MouseButtonEventArgs e) => NavGameBar_Click(s, e);
    private void TaskbarGoConsole_Click(object s, MouseButtonEventArgs e) => NavGoConsole_Click(s, e);

    private void StartButton_Click(object s, MouseButtonEventArgs e)
    {
        MessageBox.Show("GoDesk v2.3\nGoStudios Corporation\n\nUSB-Powered Gaming Desktop", "GoDesk", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExitButton_Click(object s, MouseButtonEventArgs e)
    {
        if (MessageBox.Show("Exit GoDesk?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            Application.Current.Shutdown();
    }
}
