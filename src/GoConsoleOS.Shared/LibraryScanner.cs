using System.Text.Json;
using System.Text.RegularExpressions;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.Shared;

public class LibraryScanner
{
    private readonly string _libraryPath;
    private readonly string _rootPath;

    public LibraryScanner(string rootPath)
    {
        _rootPath = rootPath;
        _libraryPath = ConfigReader.ResolvePath("launcher\\library");
        Directory.CreateDirectory(_libraryPath);
    }

    public LibraryData LoadLibrary()
    {
        var path = Path.Combine(_libraryPath, "library.json");
        if (!File.Exists(path))
            return new LibraryData { LastScanned = DateTime.MinValue };

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<LibraryData>(json) ?? new LibraryData();
        }
        catch
        {
            return new LibraryData();
        }
    }

    public void SaveLibrary(LibraryData data)
    {
        var path = Path.Combine(_libraryPath, "library.json");
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public LibraryData ScanAll()
    {
        Logger.Info("Starting library scan...");
        var games = new List<GameInfo>();

        games.AddRange(ScanSteam());
        games.AddRange(ScanEpic());
        games.AddRange(ScanGog());
        games.AddRange(ScanXbox());
        games.AddRange(ScanCustomExecutables());

        var existing = LoadLibrary();
        foreach (var game in games)
        {
            var old = existing.Games.FirstOrDefault(g => g.Id == game.Id);
            if (old != null)
            {
                game.PlaytimeMinutes = old.PlaytimeMinutes;
                game.LastPlayed = old.LastPlayed;
                game.IsFavorite = old.IsFavorite;
            }
        }

        var data = new LibraryData
        {
            Version = "1.4.0",
            LastScanned = DateTime.UtcNow,
            Games = games
        };

        SaveLibrary(data);
        Logger.Info($"Library scan complete: {games.Count} games found");
        return data;
    }

    private List<GameInfo> ScanSteam()
    {
        var games = new List<GameInfo>();
        try
        {
            var steamPath = PlatformDetection.GetSteamPath();
            if (steamPath == null) return games;

            var libraryFolders = new List<string> { steamPath };
            var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdfPath))
            {
                var content = File.ReadAllText(vdfPath);
                var matches = Regex.Matches(content, "\"path\"\\s+\"([^\"]+)\"");
                foreach (Match m in matches)
                    if (!libraryFolders.Contains(m.Groups[1].Value))
                        libraryFolders.Add(m.Groups[1].Value);
            }

            foreach (var folder in libraryFolders)
            {
                var appsDir = Path.Combine(folder, "steamapps");
                if (!Directory.Exists(appsDir)) continue;

                foreach (var acf in Directory.GetFiles(appsDir, "*.acf"))
                {
                    try
                    {
                        var acfContent = File.ReadAllText(acf);
                        var appId = Regex.Match(acfContent, "\"appid\"\\s+\"(\\d+)\"").Groups[1].Value;
                        var name = Regex.Match(acfContent, "\"name\"\\s+\"([^\"]+)\"").Groups[1].Value;
                        var installDir = Regex.Match(acfContent, "\"installdir\"\\s+\"([^\"]+)\"").Groups[1].Value;

                        if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(name)) continue;

                        var exePath = FindExecutableInDir(Path.Combine(appsDir, "common", installDir));
                        games.Add(new GameInfo
                        {
                            Id = $"steam_{appId}",
                            Title = name,
                            Platform = "Steam",
                            ExecutablePath = exePath ?? Path.Combine(installDir, "game.exe"),
                            WorkingDirectory = Path.Combine(appsDir, "common", installDir),
                            StoreId = appId,
                            IsInstalled = Directory.Exists(Path.Combine(appsDir, "common", installDir))
                        });
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Steam scan error: {ex.Message}");
        }
        return games;
    }

    private List<GameInfo> ScanEpic()
    {
        var games = new List<GameInfo>();
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var manifestsDir = Path.Combine(localAppData, "EpicGamesLauncher", "Saved", "Data", "Manifests");
            if (!Directory.Exists(manifestsDir)) return games;

            foreach (var file in Directory.GetFiles(manifestsDir, "*.item"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var displayName = root.TryGetProperty("DisplayName", out var dn) ? dn.GetString() : null;
                    var launchExec = root.TryGetProperty("LaunchExecutable", out var le) ? le.GetString() : null;
                    var installPath = root.TryGetProperty("InstallLocation", out var ip) ? ip.GetString() : null;
                    var appName = root.TryGetProperty("AppName", out var an) ? an.GetString() : null;

                    if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(appName)) continue;

                    games.Add(new GameInfo
                    {
                        Id = $"epic_{appName}",
                        Title = displayName,
                        Platform = "Epic Games",
                        ExecutablePath = launchExec != null && installPath != null
                            ? Path.Combine(installPath, launchExec) : "",
                        WorkingDirectory = installPath,
                        IsInstalled = installPath != null && Directory.Exists(installPath)
                    });
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Epic scan error: {ex.Message}");
        }
        return games;
    }

    private List<GameInfo> ScanGog()
    {
        var games = new List<GameInfo>();
        try
        {
            var gogPath = PlatformDetection.GetGogPath();
            if (gogPath == null) return games;

            var galaxyPath = Path.Combine(gogPath, "Galaxy");
            if (!Directory.Exists(gogPath))
            {
                gogPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "GOG Galaxy");
                galaxyPath = Path.Combine(gogPath, "Galaxy");
            }

            var dbPath = Path.Combine(gogPath, "storage", "galaxy", "library", "library.db");
            if (!File.Exists(dbPath))
            {
                var dbDir = Path.Combine(gogPath, "storage", "galaxy", "library");
                if (!Directory.Exists(dbDir)) return games;
            }

            var regPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GOG.com", "GalaxyClient", "games");
            if (Directory.Exists(regPath))
            {
                foreach (var jsonFile in Directory.GetFiles(regPath, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(jsonFile);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        var gameId = root.TryGetProperty("gameId", out var gi) ? gi.GetInt32() : 0;
                        var name = root.TryGetProperty("gameName", out var gn) ? gn.GetString() : null;
                        var exePath = root.TryGetProperty("playTask", out var pt) && pt.ValueKind == JsonValueKind.Object
                            ? (pt.TryGetProperty("path", out var pp) ? pp.GetString() : null) : null;
                        var workDir = root.TryGetProperty("workingDir", out var wd) ? wd.GetString() : null;

                        if (string.IsNullOrEmpty(name) || gameId == 0) continue;

                        games.Add(new GameInfo
                        {
                            Id = $"gog_{gameId}",
                            Title = name,
                            Platform = "GOG",
                            ExecutablePath = exePath ?? "",
                            WorkingDirectory = workDir,
                            IsInstalled = true
                        });
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"GOG scan error: {ex.Message}");
        }
        return games;
    }

    private List<GameInfo> ScanXbox()
    {
        var games = new List<GameInfo>();
        try
        {
            var xboxPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages");

            if (!Directory.Exists(xboxPath)) return games;

            var xboxDirs = Directory.GetDirectories(xboxPath, "Microsoft.GamingApp*")
                .Concat(Directory.GetDirectories(xboxPath, "Microsoft.Xbox*"))
                .ToList();

            foreach (var dir in xboxDirs)
            {
                var localState = Path.Combine(dir, "LocalState");
                if (!Directory.Exists(localState)) continue;

                var appxManifest = Path.Combine(dir, "AppxManifest.xml");
                if (!File.Exists(appxManifest)) continue;

                try
                {
                    var manifest = File.ReadAllText(appxManifest);
                    var displayName = Regex.Match(manifest, "<DisplayName>([^<]+)</DisplayName>").Groups[1].Value;
                    var appId = Regex.Match(manifest, "<Application Id=\"([^\"]+)\"").Groups[1].Value;

                    if (string.IsNullOrEmpty(displayName)) continue;

                    games.Add(new GameInfo
                    {
                        Id = $"xbox_{Path.GetFileName(dir)}_{appId}",
                        Title = displayName.Trim(),
                        Platform = "Xbox",
                        ExecutablePath = $"shell:appsFolder\\{Path.GetFileName(dir)}!{appId}",
                        IsInstalled = true
                    });
                }
                catch { }
            }

            var gamePassDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Gaming", "GameList");
            if (Directory.Exists(gamePassDir))
            {
                foreach (var jsonFile in Directory.GetFiles(gamePassDir, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(jsonFile);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        var title = root.TryGetProperty("Title", out var t) ? t.GetString() : null;
                        var appId = root.TryGetProperty("AppModelId", out var a) ? a.GetString() : null;

                        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(appId)) continue;

                        if (!games.Any(g => g.StoreId == appId))
                        {
                            games.Add(new GameInfo
                            {
                                Id = $"xbox_{Guid.NewGuid():N}",
                                Title = title,
                                Platform = "Xbox",
                                ExecutablePath = $"shell:appsFolder\\{appId}",
                                StoreId = appId,
                                IsInstalled = true
                            });
                        }
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Xbox scan error: {ex.Message}");
        }
        return games;
    }

    private List<GameInfo> ScanCustomExecutables()
    {
        var games = new List<GameInfo>();
        try
        {
            var customDir = ConfigReader.ResolvePath("launcher\\library\\custom");
            var configPath = Path.Combine(customDir, "custom_games.json");
            if (!File.Exists(configPath)) return games;

            var json = File.ReadAllText(configPath);
            var custom = JsonSerializer.Deserialize<List<GameInfo>>(json);
            if (custom != null) games.AddRange(custom);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Custom EXE scan error: {ex.Message}");
        }
        return games;
    }

    private static string? FindExecutableInDir(string dir)
    {
        if (!Directory.Exists(dir)) return null;
        var exts = new[] { "*.exe", "*.bat", "*.lnk" };
        foreach (var ext in exts)
        {
            var files = Directory.GetFiles(dir, ext);
            if (files.Length > 0)
                return files[0];
        }
        return null;
    }
}
