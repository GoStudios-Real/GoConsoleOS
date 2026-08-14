using GoConsoleOS.Mobile.Pages;

namespace GoConsoleOS.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        MainPage = new NavigationPage(new ConnectPage());
    }
}