using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GoConsoleOS.GoConsole.Views;

public partial class GameRecordingView : UserControl
{
    private bool _isRecording;

    public GameRecordingView()
    {
        InitializeComponent();
        LoadClips();
    }

    private void LoadClips()
    {
        var clips = new List<ClipItem>
        {
            new() { Game = "Neon Drift", Duration = "0:30", Date = "Today 2:15pm" },
            new() { Game = "Void Marauders", Duration = "1:00", Date = "Today 1:30pm" },
            new() { Game = "Crystal Realms", Duration = "3:00", Date = "Yesterday 8:45pm" },
        };
        ClipList.ItemsSource = clips;
    }

    private void ToggleRecording(object sender, RoutedEventArgs e)
    {
        _isRecording = !_isRecording;
        if (_isRecording)
        {
            RecordingStatus.Text = "● RECORDING";
            RecordingStatus.Foreground = System.Windows.Media.Brushes.Tomato;
            RecordToggleBtn.Content = "⏹ STOP RECORDING";
            RecordToggleBtn.Background = System.Windows.Media.Brushes.Gray;
            ToastManager.Show("Recording started");
        }
        else
        {
            RecordingStatus.Text = "IDLE";
            RecordingStatus.Foreground = System.Windows.Media.Brushes.White;
            RecordToggleBtn.Content = "⏺ START RECORDING";
            RecordToggleBtn.Background = System.Windows.Media.Brushes.Tomato;
            ToastManager.Show("Clip saved");
        }
    }

    public class ClipItem
    {
        public string Game { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Date { get; set; } = "";
    }
}
