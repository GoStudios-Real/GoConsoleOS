using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GoConsoleOS.GoConsole.Views;

public partial class RemotePlayView : UserControl
{
    private readonly List<DeviceItem> _devices = new();
    private readonly DispatcherTimer _streamTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private int _streamSeconds;

    public RemotePlayView()
    {
        InitializeComponent();
        _devices.AddRange(new[]
        {
            new DeviceItem { Name = "GoConsole TV", Icon = "📺", Detail = "Smart TV \u2022 Connected", ButtonText = "STREAM" },
            new DeviceItem { Name = "Surface Laptop", Icon = "💻", Detail = "Windows \u2022 Connected", ButtonText = "STREAM" },
            new DeviceItem { Name = "Pixel Phone", Icon = "📱", Detail = "Android \u2022 Connected", ButtonText = "STREAM" },
            new DeviceItem { Name = "iPad", Icon = "📱", Detail = "iOS \u2022 Connected", ButtonText = "STREAM" },
        });
        DeviceList.ItemsSource = _devices;

        _streamTimer.Tick += (_, _) =>
        {
            _streamSeconds += 2;
            var active = _devices.FirstOrDefault(d => d.IsStreaming);
            if (active != null)
            {
                StreamingDetail.Text = $"{active.Name} \u2022 {ResolutionBox.Text} \u2022 {FrameBox.Text} \u2022 streamed {_streamSeconds / 60}m {_streamSeconds % 60}s";
            }
        };
    }

    private void ToggleStream(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string name)
        {
            var device = _devices.FirstOrDefault(d => d.Name == name);
            if (device == null) return;

            if (!device.IsStreaming)
            {
                foreach (var other in _devices)
                {
                    other.IsStreaming = false;
                    other.ButtonText = "STREAM";
                }

                device.IsStreaming = true;
                device.ButtonText = "STOP";
                StreamingBadge.Visibility = Visibility.Visible;
                StopAllBtn.Visibility = Visibility.Visible;
                RemoteStatus.Text = $"Streaming to {device.Name}...";
                _streamSeconds = 0;
                _streamTimer.Start();
                ToastManager.Show($"Remote Play started on {device.Name}");
            }
            else
            {
                device.IsStreaming = false;
                device.ButtonText = "STREAM";
                _streamTimer.Stop();
                StreamingBadge.Visibility = Visibility.Collapsed;
                StopAllBtn.Visibility = Visibility.Collapsed;
                RemoteStatus.Text = "Stream your games to other devices";
                ToastManager.Show($"Remote Play stopped on {device.Name}");
            }

            DeviceList.ItemsSource = null;
            DeviceList.ItemsSource = _devices;
        }
    }

    private void StopAll(object sender, RoutedEventArgs e)
    {
        foreach (var device in _devices)
        {
            device.IsStreaming = false;
            device.ButtonText = "STREAM";
        }
        _streamTimer.Stop();
        StreamingBadge.Visibility = Visibility.Collapsed;
        StopAllBtn.Visibility = Visibility.Collapsed;
        RemoteStatus.Text = "Stream your games to other devices";
        DeviceList.ItemsSource = null;
        DeviceList.ItemsSource = _devices;
        ToastManager.Show("All streams stopped");
    }

    public class DeviceItem
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "📱";
        public string Detail { get; set; } = "";
        public string ButtonText { get; set; } = "STREAM";
        public bool IsStreaming { get; set; }
    }
}
