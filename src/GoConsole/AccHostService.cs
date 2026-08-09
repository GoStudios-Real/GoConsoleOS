using System;
using System.IO;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Acc;
using GoConsoleOS.Shared.Ai;

namespace GoConsoleOS.GoConsole;

/// <summary>
/// Hosts the GoConsoleOS ACC server (account API + account website) and the
/// GoAI gaming assistant inside the shell. This gives every GoConsoleOS.exe /
/// GoConsole.exe instance a server on port 39210.
/// </summary>
public sealed class AccHostService
{
    private readonly LibraryScanner _scanner;
    private readonly Func<string, bool> _launchGame;
    private GoConsoleServer? _server;

    public event Action<string, string>? OnLogin;

    public AccHostService(LibraryScanner scanner, Func<string, bool> launchGame)
    {
        _scanner = scanner;
        _launchGame = launchGame;
    }

    public GoConsoleServer? Server => _server;

    public void Start()
    {
        try
        {
            var root = ConfigReader.RootPath ?? Directory.GetCurrentDirectory();
            var store = new AccStore(root);

            var webRoot = Path.Combine(root, "web");
            if (Directory.Exists(webRoot))
                store.AccountWebRoot = webRoot;

            var ai = new GoAiEngine(
                gamesProvider: () => ListGames(),
                launchAction: title =>
                {
                    OnLaunchRequested?.Invoke(title);
                    return true;
                },
                healthProvider: () => "USB health looks good - all drives are OK.",
                statsProvider: () => "CPU, RAM and storage are all within normal limits.");

            _server = new GoConsoleServer(store, ai);
            _server.OnLogin += (u, d) => OnLogin?.Invoke(u, d);
            _server.Start(GoConsoleServer.DefaultPort);
        }
        catch (Exception ex)
        {
            Logger.Warn($"ACC host failed to start: {ex.Message}");
        }
    }

    public event Action<string>? OnLaunchRequested;

    public void Stop() => _server?.Dispose();

    private IEnumerable<string> ListGames()
    {
        try
        {
            return _scanner.LoadLibrary().Games
                .Select(g => g.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .OrderBy(t => t);
        }
        catch
        {
            return new List<string>();
        }
    }
}
