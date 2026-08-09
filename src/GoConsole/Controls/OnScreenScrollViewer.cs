using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GoConsoleOS.GoConsole.Controls;

public class OnScreenScrollViewer : ContentControl
{
    private ScrollViewer? _scroller;
    private Border? _indicator;
    private readonly DispatcherTimer _hideTimer;
    private bool _visible;

    static OnScreenScrollViewer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(OnScreenScrollViewer),
            new FrameworkPropertyMetadata(typeof(OnScreenScrollViewer)));
    }

    public OnScreenScrollViewer()
    {
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            HideIndicator();
        };
    }

    public ScrollViewer? Scroller => _scroller;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _scroller = GetTemplateChild("ContentScroller") as ScrollViewer;
        _indicator = GetTemplateChild("ScrollIndicator") as Border;

        if (_scroller != null)
            _scroller.ScrollChanged += OnScrollChanged;
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        UpdateIndicator();
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void UpdateIndicator()
    {
        if (_scroller == null || _indicator == null) return;
        var total = _scroller.ExtentHeight;
        var view = _scroller.ViewportHeight;
        if (total <= view + 1)
        {
            HideIndicator();
            return;
        }

        var thumbHeight = Math.Max(28, view / total * _scroller.ActualHeight);
        var maxOffset = _scroller.ScrollableHeight;
        var offset = maxOffset > 0 ? _scroller.VerticalOffset / maxOffset : 0;
        var trackHeight = _scroller.ActualHeight - thumbHeight;
        var top = offset * Math.Max(0, trackHeight);

        _indicator.Height = thumbHeight;
        _indicator.Margin = new Thickness(0, top, 6, 0);

        if (!_visible)
        {
            _visible = true;
            _indicator.Visibility = Visibility.Visible;
            _indicator.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 0.9, TimeSpan.FromMilliseconds(150)));
        }
    }

    private void HideIndicator()
    {
        if (!_visible || _indicator == null) return;
        _visible = false;
        var fade = new DoubleAnimation(0.9, 0, TimeSpan.FromMilliseconds(250));
        fade.Completed += (_, _) =>
        {
            if (!_visible && _indicator != null) _indicator.Visibility = Visibility.Collapsed;
        };
        _indicator.BeginAnimation(OpacityProperty, fade);
    }

    public void ScrollDown(double amount)
    {
        if (_scroller == null) return;
        _scroller.ScrollToVerticalOffset(_scroller.VerticalOffset + amount);
    }

    public void ScrollUp(double amount)
    {
        if (_scroller == null) return;
        _scroller.ScrollToVerticalOffset(_scroller.VerticalOffset - amount);
    }
}
