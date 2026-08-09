using System.Windows;
using System.Windows.Input;

namespace GoConsoleOS.Shared.Input;

public class FocusNavigator
{
    private readonly UIElement _root;
    private const int DeadZone = 8000;
    private const int RepeatDelayMs = 250;
    private DateTime _lastNavTime = DateTime.MinValue;

    public FocusNavigator(UIElement root)
    {
        _root = root;
    }

    public bool HandleStick(short thumbX, short thumbY)
    {
        if ((DateTime.UtcNow - _lastNavTime).TotalMilliseconds < RepeatDelayMs)
            return false;

        var dx = Math.Abs(thumbX) > DeadZone ? thumbX : (short)0;
        var dy = Math.Abs(thumbY) > DeadZone ? thumbY : (short)0;

        if (dx == 0 && dy == 0) return false;

        if (Math.Abs(dx) > Math.Abs(dy))
            return MoveFocus(dx > 0 ? FocusNavigationDirection.Right : FocusNavigationDirection.Left);
        else
            return MoveFocus(dy > 0 ? FocusNavigationDirection.Down : FocusNavigationDirection.Up);
    }

    public bool HandleDpad(ControllerButtons button)
    {
        if ((DateTime.UtcNow - _lastNavTime).TotalMilliseconds < RepeatDelayMs)
            return false;

        var dir = button switch
        {
            ControllerButtons.DPadUp => FocusNavigationDirection.Up,
            ControllerButtons.DPadDown => FocusNavigationDirection.Down,
            ControllerButtons.DPadLeft => FocusNavigationDirection.Left,
            ControllerButtons.DPadRight => FocusNavigationDirection.Right,
            _ => (FocusNavigationDirection?)null
        };

        if (dir == null) return false;
        return MoveFocus(dir.Value);
    }

    private bool MoveFocus(FocusNavigationDirection dir)
    {
        var focused = FocusManager.GetFocusedElement(_root) as UIElement;
        if (focused == null)
        {
            _root.MoveFocus(new TraversalRequest(dir));
        }
        else
        {
            var request = new TraversalRequest(dir);
            focused.MoveFocus(request);
        }
        _lastNavTime = DateTime.UtcNow;
        return true;
    }

    public static bool IsFocusable(UIElement element)
    {
        return element is IInputElement ie && ie.Focusable && element.IsVisible && element.IsEnabled;
    }
}
