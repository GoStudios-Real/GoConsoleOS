using System.Diagnostics;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.Shared;

public static class GameLauncher
{
    public static bool Launch(GameInfo game, Action? onLaunchSuccess = null, Action<Process?>? onProcessStarted = null)
    {
        if (game == null) return false;

        Logger.Info($"Launching game: {game.Title} ({game.Platform})");

        Process? process = null;

        if (!string.IsNullOrEmpty(game.ExecutablePath) && File.Exists(game.ExecutablePath))
            process = StartExecutable(game);
        else
            process = StartViaPlatformUri(game);

        if (process != null)
        {
            onProcessStarted?.Invoke(process);
            onLaunchSuccess?.Invoke();
            return true;
        }

        return false;
    }

    private static Process? StartExecutable(GameInfo game)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = game.ExecutablePath,
                WorkingDirectory = game.WorkingDirectory ?? Path.GetDirectoryName(game.ExecutablePath),
                Arguments = game.LaunchArguments ?? "",
                UseShellExecute = true
            };
            var proc = Process.Start(psi);
            Logger.Info($"Launched EXE: {game.ExecutablePath}");
            return proc;
        }
        catch (Exception ex)
        {
            Logger.Error($"EXE launch failed for {game.Title}: {ex.Message}");
            return null;
        }
    }

    private static Process? StartViaPlatformUri(GameInfo game)
    {
        string? uri = null;

        if (game.Id.StartsWith("steam_") && game.StoreId != null)
            uri = $"steam://rungameid/{game.StoreId}";
        else if (game.Id.StartsWith("epic_"))
        {
            var epicId = game.StoreId ?? game.Id["epic_".Length..];
            uri = $"com.epicgames.launcher://apps/{epicId}?action=launch&silent=true";
        }
        else if (game.Id.StartsWith("gog_"))
        {
            var gogId = game.StoreId ?? game.Id["gog_".Length..];
            uri = $"goggalaxy://openGameView/{gogId}";
        }
        else if (game.Platform == "Xbox" && game.StoreId != null)
        {
            uri = $"ms-xbox://{game.StoreId}";
        }

        if (uri == null)
        {
            Logger.Warn($"No launch method found for {game.Title} (Id: {game.Id})");
            return null;
        }

        try
        {
            var proc = Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            Logger.Info($"Launched via URI: {uri}");
            return proc;
        }
        catch (Exception ex)
        {
            Logger.Error($"URI launch failed for {game.Title}: {ex.Message}");
            return null;
        }
    }

    public static string GetLaunchButtonText(GameInfo game)
    {
        if (!string.IsNullOrEmpty(game.ExecutablePath) && File.Exists(game.ExecutablePath))
            return "PLAY";

        if (game.Id.StartsWith("steam_") && game.StoreId != null)
            return "PLAY ON STEAM";

        if (game.Id.StartsWith("epic_"))
            return "PLAY ON EPIC";

        if (game.Id.StartsWith("gog_"))
            return "PLAY ON GOG";

        if (game.Platform == "Xbox")
            return "PLAY ON XBOX";

        return "LAUNCH";
    }
}
