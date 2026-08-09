using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace GoConsoleOS.Shared.Input;

public enum ControllerKind
{
    Auto,
    Xbox,
    PlayStation5,
    Switch2,
    Generic
}

public enum ControllerButtons
{
    DPadUp = 0x0001,
    DPadDown = 0x0002,
    DPadLeft = 0x0004,
    DPadRight = 0x0008,
    Start = 0x0010,
    Back = 0x0020,
    LeftThumb = 0x0040,
    RightThumb = 0x0080,
    LeftShoulder = 0x0100,
    RightShoulder = 0x0200,
    Guide = 0x0400,
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000
}

[StructLayout(LayoutKind.Sequential)]
public struct XInputState
{
    public uint PacketNumber;
    public XInputGamepad Gamepad;
}

[StructLayout(LayoutKind.Sequential)]
public struct XInputGamepad
{
    public ushort Buttons;
    public byte LeftTrigger;
    public byte RightTrigger;
    public short ThumbLX;
    public short ThumbLY;
    public short ThumbRX;
    public short ThumbRY;
}

public class ControllerState
{
    public bool IsConnected { get; set; }
    public uint PacketNumber { get; set; }
    public ushort Buttons { get; set; }
    public byte LeftTrigger { get; set; }
    public byte RightTrigger { get; set; }
    public short ThumbLX { get; set; }
    public short ThumbLY { get; set; }
    public short ThumbRX { get; set; }
    public short ThumbRY { get; set; }

    public bool IsButtonDown(ControllerButtons button) => (Buttons & (ushort)button) != 0;

    public bool WasButtonPressed(ControllerButtons button, ushort previousButtons)
    {
        return IsButtonDown(button) && (previousButtons & (ushort)button) == 0;
    }

    public bool WasButtonReleased(ControllerButtons button, ushort previousButtons)
    {
        return !IsButtonDown(button) && (previousButtons & (ushort)button) != 0;
    }

    public float LeftStickX => ThumbLX / 32768f;
    public float LeftStickY => ThumbLY / 32768f;
    public float RightStickX => ThumbRX / 32768f;
    public float RightStickY => ThumbRY / 32768f;
    public float LeftTriggerFloat => LeftTrigger / 255f;
    public float RightTriggerFloat => RightTrigger / 255f;
}

public class ControllerEngine : IDisposable
{
    private const string DllName = "xinput1_4.dll";

    [DllImport(DllName)]
    private static extern int XInputGetState(int dwUserIndex, out XInputState pState);

    [DllImport(DllName)]
    private static extern void XInputEnable(bool enable);

    [DllImport(DllName)]
    private static extern int XInputSetState(int dwUserIndex, ref XInputVibration pVibration);

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputVibration
    {
        public ushort LeftMotor;
        public ushort RightMotor;
    }

    private readonly int _controllerIndex;
    private Thread? _pollThread;
    private volatile bool _running;
    private ushort _previousButtons;
    private int _pollIntervalMs;
    private ControllerKind _kind = ControllerKind.Auto;
    private ControllerKind _detectedKind = ControllerKind.Generic;

    public int ControllerIndex => _controllerIndex;
    public bool IsConnected { get; private set; }
    public ControllerState CurrentState { get; } = new();
    public ushort PreviousButtons => _previousButtons;
    public ControllerKind Kind => _kind == ControllerKind.Auto ? _detectedKind : _kind;
    public ControllerKind DetectedKind => _detectedKind;

    public event Action<ControllerButtons>? ButtonPressed;
    public event Action<ControllerButtons>? ButtonReleased;
    public event Action<ControllerState>? StateUpdated;
    public event Action? Connected;
    public event Action? Disconnected;

    public ControllerEngine(int controllerIndex = 0, int pollRateHz = 60, ControllerKind kind = ControllerKind.Auto)
    {
        _controllerIndex = controllerIndex;
        _pollIntervalMs = 1000 / Math.Clamp(pollRateHz, 10, 240);
        _kind = kind;
        _detectedKind = DetectControllerKind();
    }

    public void SetKind(ControllerKind kind)
    {
        _kind = kind;
        _detectedKind = DetectControllerKind();
    }

    public static ControllerKind DetectControllerKind()
    {
        try
        {
            using var root = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\HID");
            if (root == null) return ControllerKind.Generic;
            foreach (var sub in root.GetSubKeyNames())
            {
                var m = System.Text.RegularExpressions.Regex.Match(sub, @"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})");
                if (!m.Success) continue;
                var vid = m.Groups[1].Value;
                var pid = m.Groups[2].Value;
                if (vid == "045E") return ControllerKind.Xbox;
                if (vid == "054C")
                {
                    if (pid is "0CE6" or "0DF2" or "0D8F" or "0E7E") return ControllerKind.PlayStation5;
                    return ControllerKind.PlayStation5;
                }
                if (vid == "057E")
                {
                    if (pid is "2009" or "2006" or "2001") return ControllerKind.Switch2;
                    return ControllerKind.Generic;
                }
            }
        }
        catch { }
        return ControllerKind.Generic;
    }

    public string GetKindName()
    {
        return Kind switch
        {
            ControllerKind.Xbox => "Xbox Controller",
            ControllerKind.PlayStation5 => "PlayStation 5 (DualSense)",
            ControllerKind.Switch2 => "Nintendo Switch 2",
            _ => "Generic Gamepad"
        };
    }

    public string GetButtonLabel(ControllerButtons button)
    {
        return Kind switch
        {
            ControllerKind.PlayStation5 => button switch
            {
                ControllerButtons.A => "Cross",
                ControllerButtons.B => "Circle",
                ControllerButtons.X => "Square",
                ControllerButtons.Y => "Triangle",
                ControllerButtons.Guide => "PS",
                ControllerButtons.Start => "Options",
                ControllerButtons.Back => "Share",
                ControllerButtons.LeftShoulder => "L1",
                ControllerButtons.RightShoulder => "R1",
                ControllerButtons.LeftThumb => "L3",
                ControllerButtons.RightThumb => "R3",
                ControllerButtons.DPadUp => "D-Pad Up",
                ControllerButtons.DPadDown => "D-Pad Down",
                ControllerButtons.DPadLeft => "D-Pad Left",
                ControllerButtons.DPadRight => "D-Pad Right",
                _ => button.ToString()
            },
            ControllerKind.Switch2 => button switch
            {
                ControllerButtons.A => "B",
                ControllerButtons.B => "A",
                ControllerButtons.X => "Y",
                ControllerButtons.Y => "X",
                ControllerButtons.Guide => "Home",
                ControllerButtons.Start => "+",
                ControllerButtons.Back => "-",
                ControllerButtons.LeftShoulder => "L",
                ControllerButtons.RightShoulder => "R",
                ControllerButtons.LeftThumb => "L Stick",
                ControllerButtons.RightThumb => "R Stick",
                ControllerButtons.DPadUp => "D-Pad Up",
                ControllerButtons.DPadDown => "D-Pad Down",
                ControllerButtons.DPadLeft => "D-Pad Left",
                ControllerButtons.DPadRight => "D-Pad Right",
                _ => button.ToString()
            },
            ControllerKind.Xbox => button switch
            {
                ControllerButtons.Guide => "Xbox",
                ControllerButtons.Start => "Menu",
                ControllerButtons.Back => "View",
                ControllerButtons.LeftShoulder => "LB",
                ControllerButtons.RightShoulder => "RB",
                ControllerButtons.LeftThumb => "LS",
                ControllerButtons.RightThumb => "RS",
                ControllerButtons.DPadUp => "D-Pad Up",
                ControllerButtons.DPadDown => "D-Pad Down",
                ControllerButtons.DPadLeft => "D-Pad Left",
                ControllerButtons.DPadRight => "D-Pad Right",
                _ => button.ToString()
            },
            _ => button switch
            {
                ControllerButtons.A => "A",
                ControllerButtons.B => "B",
                ControllerButtons.X => "X",
                ControllerButtons.Y => "Y",
                _ => button.ToString()
            }
        };
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _pollThread = new Thread(PollLoop) { IsBackground = true, Name = $"Controller_{_controllerIndex}" };
        _pollThread.Start();
        Logger.Info($"Controller engine started for player {_controllerIndex + 1}");
    }

    public void Stop()
    {
        _running = false;
        _pollThread?.Join(2000);
        Logger.Info("Controller engine stopped");
    }

    public void SetPollRate(int hz)
    {
        _pollIntervalMs = 1000 / Math.Clamp(hz, 10, 240);
    }

    private void PollLoop()
    {
        while (_running)
        {
            try
            {
                var result = XInputGetState(_controllerIndex, out var state);
                var wasConnected = IsConnected;
                IsConnected = result == 0;

                if (IsConnected)
                {
                    CurrentState.IsConnected = true;
                    CurrentState.PacketNumber = state.PacketNumber;
                    CurrentState.Buttons = state.Gamepad.Buttons;
                    CurrentState.LeftTrigger = state.Gamepad.LeftTrigger;
                    CurrentState.RightTrigger = state.Gamepad.RightTrigger;
                    CurrentState.ThumbLX = state.Gamepad.ThumbLX;
                    CurrentState.ThumbLY = state.Gamepad.ThumbLY;
                    CurrentState.ThumbRX = state.Gamepad.ThumbRX;
                    CurrentState.ThumbRY = state.Gamepad.ThumbRY;

                    if (!wasConnected)
                    {
                        Logger.Info($"Controller {_controllerIndex + 1} connected");
                        Connected?.Invoke();
                    }

                    DetectButtonChanges();
                    StateUpdated?.Invoke(CurrentState);
                }
                else
                {
                    CurrentState.IsConnected = false;
                    if (wasConnected)
                    {
                        Logger.Info($"Controller {_controllerIndex + 1} disconnected");
                        Disconnected?.Invoke();
                    }
                }
            }
            catch
            {
                CurrentState.IsConnected = false;
            }

            Thread.Sleep(_pollIntervalMs);
        }
    }

    private void DetectButtonChanges()
    {
        foreach (ControllerButtons button in Enum.GetValues<ControllerButtons>())
        {
            if (CurrentState.IsButtonDown(button) && (_previousButtons & (ushort)button) == 0)
                ButtonPressed?.Invoke(button);
            else if (!CurrentState.IsButtonDown(button) && (_previousButtons & (ushort)button) != 0)
                ButtonReleased?.Invoke(button);
        }
        _previousButtons = CurrentState.Buttons;
    }

    public void SetVibration(ushort leftMotor, ushort rightMotor)
    {
        if (!IsConnected) return;
        try
        {
            var vib = new XInputVibration { LeftMotor = leftMotor, RightMotor = rightMotor };
            XInputSetState(_controllerIndex, ref vib);
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
        SetVibration(0, 0);
        GC.SuppressFinalize(this);
    }
}
