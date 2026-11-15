using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GoConsoleOS.Shared.Acc;

namespace GoConsoleOS.GoConsole.Views;

public partial class RemotePlayView : UserControl
{
    private readonly List<DeviceItem> _devices = new();
    private readonly DispatcherTimer _streamTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _pairTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private int _streamSeconds;
    private string _shownCode = "";

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
        RefreshPairing(forceImage: true);
        _pairTimer.Tick += (_, _) => RefreshPairing(forceImage: false);
        _pairTimer.Start();

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

    private void RefreshPairing(bool forceImage)
    {
        try
        {
            var code = PairingService.CurrentCode;
            var expires = PairingService.ExpiresUtc;
            var remaining = expires - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            PairingCodeText.Text = code;
            PairingAddressText.Text = $"http://{PairingService.DetectLanIPv4() ?? "127.0.0.1"}:{GoConsoleServer.DefaultPort}";
            PairingExpiryText.Text = $"Code expires in {(int)remaining.TotalMinutes}:{remaining.Seconds:D2}";

            if (forceImage || code != _shownCode)
            {
                _shownCode = code;
                PairingQrImage.Source = BuildQrImage(PairingService.BuildDeepLink());
            }
        }
        catch (Exception ex)
        {
            PairingExpiryText.Text = $"Pairing unavailable: {ex.Message}";
        }
    }

    private void NewPairingCode(object sender, RoutedEventArgs e)
    {
        PairingService.Regenerate();
        RefreshPairing(forceImage: true);
        ToastManager.Show("New cloud gaming pairing code generated");
    }

    private static BitmapImage BuildQrImage(string content)
    {
        using var generator = new QRCoder.QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCoder.QRCodeGenerator.ECCLevel.M);
        var qr = new QRCoder.PngByteQRCode(data);
        var png = qr.GetGraphic(6);
        using var ms = new MemoryStream(png);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
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
