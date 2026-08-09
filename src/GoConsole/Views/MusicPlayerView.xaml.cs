using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class MusicPlayerView : UserControl
{
    private List<TrackItem> _tracks = new();
    private int _currentIndex = -1;
    private bool _isPlaying;
    private bool _repeat;

    public MusicPlayerView()
    {
        InitializeComponent();
        ApplyGenrePreference();
        LoadTracks();
    }

    private void ApplyGenrePreference()
    {
        var config = ConfigReader.ReadInitConfig();
        var defaultGenre = config.Music.Genre;
        var saved = SettingsStore.Get("music.genre");
        var genre = !string.IsNullOrWhiteSpace(saved) ? saved : (defaultGenre ?? "");

        if (!string.IsNullOrWhiteSpace(genre))
        {
            SettingsStore.Set("music.genre", genre);
            GenreText.Text = genre.ToUpperInvariant();
            GenreBadge.Visibility = Visibility.Visible;
        }
    }

    private void LoadTracks()
    {
        var musicDir = Path.Combine(ConfigReader.RootPath ?? "", "system", "music");
        if (!Directory.Exists(musicDir))
        {
            Directory.CreateDirectory(musicDir);
            MusicStatus.Text = "No music folder found. Created system/music/ — add .mp3 files there.";
            return;
        }

        var files = Directory.GetFiles(musicDir, "*.mp3").Union(
                     Directory.GetFiles(musicDir, "*.wav")).Union(
                     Directory.GetFiles(musicDir, "*.m4a")).ToArray();

        if (files.Length == 0)
        {
            MusicStatus.Text = "No music files found. Add .mp3 files to system/music/ folder.";
            return;
        }

        _tracks = files.Select(f =>
        {
            var fi = new FileInfo(f);
            var name = Path.GetFileNameWithoutExtension(f);
            var parts = name.Split('-');
            return new TrackItem
            {
                Path = f,
                Title = parts.Length > 1 ? parts[1].Trim() : name,
                Artist = parts.Length > 1 ? parts[0].Trim() : "Unknown",
                Duration = FormatDuration(fi.Length)
            };
        }).ToList();

        TrackList.ItemsSource = _tracks;
        MusicStatus.Text = $"{_tracks.Count} track{(_tracks.Count == 1 ? "" : "s")} loaded";
    }

    private static string FormatDuration(long bytes)
    {
        var sec = bytes / 20000;
        if (sec > 3600) return $"{sec / 3600}:{(sec % 3600) / 60:D2}:{sec % 60:D2}";
        return $"{(sec / 60) % 60}:{sec % 60:D2}";
    }

    private void PlayTrack(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string path)
        {
            var idx = _tracks.FindIndex(t => t.Path == path);
            if (idx >= 0) PlayAtIndex(idx);
        }
    }

    private void PlayAtIndex(int index)
    {
        if (index < 0 || index >= _tracks.Count) return;
        _currentIndex = index;
        var track = _tracks[index];

        try
        {
            MediaPlayer.Source = new Uri(track.Path);
            MediaPlayer.Play();
            _isPlaying = true;
            PlayPauseIcon.Text = "⏸";
            NowPlayingTitle.Text = track.Title;
            NowPlayingArtist.Text = track.Artist;
        }
        catch (Exception ex)
        {
            NowPlayingTitle.Text = $"Error: {ex.Message}";
        }
    }

    private void TogglePlayPause(object sender, MouseButtonEventArgs e)
    {
        if (_currentIndex < 0)
        {
            if (_tracks.Count > 0) PlayAtIndex(0);
            return;
        }

        _isPlaying = !_isPlaying;
        if (_isPlaying) { MediaPlayer.Play(); PlayPauseIcon.Text = "⏸"; }
        else { MediaPlayer.Pause(); PlayPauseIcon.Text = "▶"; }
    }

    private void PreviousTrack(object sender, MouseButtonEventArgs e)
    {
        if (_tracks.Count == 0) return;
        PlayAtIndex(_currentIndex > 0 ? _currentIndex - 1 : _tracks.Count - 1);
    }

    private void NextTrack(object sender, MouseButtonEventArgs e)
    {
        if (_tracks.Count == 0) return;
        PlayAtIndex((_currentIndex + 1) % _tracks.Count);
    }

    private void TrackEnded(object sender, RoutedEventArgs e)
    {
        if (_repeat)
            PlayAtIndex(_currentIndex);
        else
            NextTrack(null!, null!);
    }

    private void ToggleRepeat(object sender, MouseButtonEventArgs e)
    {
        _repeat = !_repeat;
        RepeatIcon.Foreground = _repeat
            ? FindResource("BrushAccentPrimary") as Brush ?? Brushes.Cyan
            : FindResource("BrushTextSecondary") as Brush;
    }

    public class TrackItem
    {
        public string Path { get; set; } = "";
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Duration { get; set; } = "";
    }
}
