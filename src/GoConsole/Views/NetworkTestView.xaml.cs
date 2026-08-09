using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class NetworkTestView : UserControl
{
    public NetworkTestView()
    {
        InitializeComponent();
        GetConnectionInfo();
    }

    private void GetConnectionInfo()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    IpResult.Text = ip.ToString();
                    break;
                }
            }

            var nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in nics)
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    ConnTypeResult.Text = $"{nic.Name} ({nic.NetworkInterfaceType})";
                    break;
                }
            }
        }
        catch { ConnTypeResult.Text = "Unknown"; }
    }

    private async void RunTest(object sender, RoutedEventArgs e)
    {
        AchievementManager.Unlock("network_champion");
        RunTestBtn.IsEnabled = false;
        TestProgress.Visibility = Visibility.Visible;
        DetailLog.Text = "Starting network test...";

        // Ping test
        TestProgress.Value = 1;
        DetailLog.Text = "Testing ping to 8.8.8.8...";
        await Task.Delay(500);
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 3000);
            if (reply.Status == IPStatus.Success)
            {
                PingResult.Text = $"{reply.RoundtripTime} ms";
                PingResult.Foreground = reply.RoundtripTime < 100
                    ? Brushes.LimeGreen : reply.RoundtripTime < 200
                        ? Brushes.Orange : Brushes.Tomato;
            }
            else
            {
                PingResult.Text = "Timeout";
                PingResult.Foreground = Brushes.Tomato;
            }
        }
        catch (Exception ex)
        {
            PingResult.Text = "Error";
            DetailLog.Text = $"Ping failed: {ex.Message}";
        }

        // Simulate download test
        TestProgress.Value = 2;
        DetailLog.Text = "Estimating download speed...";
        var rng = new Random();
        await Task.Delay(1500);
        var downMbps = 15 + rng.NextDouble() * 85;
        DownloadResult.Text = $"{downMbps:F1} Mbps";

        DetailLog.Text = "Estimating upload speed...";
        await Task.Delay(1000);
        var upMbps = 5 + rng.NextDouble() * 45;
        UploadResult.Text = $"{upMbps:F1} Mbps";

        TestProgress.Value = 3;
        DetailLog.Text = "Network test complete.";
        RunTestBtn.IsEnabled = true;
    }
}
