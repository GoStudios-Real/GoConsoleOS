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
        Logger.Info("Starting GoStudios library scan...");
        var games = new List<GameInfo>();

        games.AddRange(ScanGoStudiosGames());
        games.AddRange(ScanGoStudiosCatalog());

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
            Version = "2.0.0",
            LastScanned = DateTime.UtcNow,
            Games = games
        };

        SaveLibrary(data);
        Logger.Info($"GoStudios library scan complete: {games.Count} games found");
        return data;
    }

    private List<GameInfo> ScanGoStudiosGames()
    {
        var games = new List<GameInfo>();
        try
        {
            // Scan for games in the USB 'games' folder
            var gamesPath = Path.Combine(_rootPath, "games");
            if (!Directory.Exists(gamesPath))
            {
                Logger.Info("No 'games' folder found, creating placeholder games");
                return GetPlaceholderGames();
            }

            // Look for game directories or executables
            var gameDirs = Directory.GetDirectories(gamesPath);
            foreach (var gameDir in gameDirs)
            {
                var gameName = Path.GetFileName(gameDir);
                var exePath = FindExecutableInDir(gameDir);
                
                if (exePath != null)
                {
                    games.Add(new GameInfo
                    {
                        Id = $"gostudios_{gameName.ToLowerInvariant().Replace(" ", "_")}",
                        Title = gameName,
                        Platform = "GoStudios",
                        GameType = gameName.Contains("3D") ? "3D" : "2D",
                        ExecutablePath = exePath,
                        WorkingDirectory = gameDir,
                        IsInstalled = true
                    });
                }
            }

            // If no games found in folder, use placeholders
            if (games.Count == 0)
            {
                games = GetPlaceholderGames();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"GoStudios game scan error: {ex.Message}");
            games = GetPlaceholderGames();
        }
        return games;
    }

    private List<GameInfo> ScanGoStudiosCatalog()
    {
        var games = new List<GameInfo>();
        try
        {
            // Look for games catalog JSON
            var catalogPath = ConfigReader.ResolvePath("launcher\\library\\gostudios_catalog.json");
            if (!File.Exists(catalogPath))
            {
                // Create default catalog
                CreateDefaultCatalog(catalogPath);
            }

            var json = File.ReadAllText(catalogPath);
            var catalog = JsonSerializer.Deserialize<List<GameInfo>>(json);
            if (catalog != null)
            {
                foreach (var game in catalog)
                {
                    game.Platform = "GoStudios";
                    games.Add(game);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"GoStudios catalog scan error: {ex.Message}");
        }
        return games;
    }

    private List<GameInfo> GetPlaceholderGames()
    {
        return new List<GameInfo>
        {
            // 2D Games
            new GameInfo
            {
                Id = "gostudios_pixel_adventure",
                Title = "Pixel Adventure",
                Platform = "GoStudios",
                GameType = "2D",
                Description = "A classic 2D platformer with pixel art graphics",
                IsInstalled = true,
                Genres = new List<string> { "Platformer", "Adventure" }
            },
            new GameInfo
            {
                Id = "gostudios_space_shooter",
                Title = "Space Shooter",
                Platform = "GoStudios",
                GameType = "2D",
                Description = "Retro-style space shooter with intense action",
                IsInstalled = true,
                Genres = new List<string> { "Shooter", "Action" }
            },
            new GameInfo
            {
                Id = "gostudios_puzzle_quest",
                Title = "Puzzle Quest",
                Platform = "GoStudios",
                GameType = "2D",
                Description = "Brain-teasing puzzle game with quest elements",
                IsInstalled = true,
                Genres = new List<string> { "Puzzle", "RPG" }
            },
            // 3D Games
            new GameInfo
            {
                Id = "gostudios_3d_arena",
                Title = "3D Arena",
                Platform = "GoStudios",
                GameType = "3D",
                Description = "Fast-paced 3D arena combat game",
                IsInstalled = true,
                Genres = new List<string> { "Action", "Arena" }
            },
            new GameInfo
            {
                Id = "gostudios_racing_3d",
                Title = "Racing 3D",
                Platform = "GoStudios",
                GameType = "3D",
                Description = "High-speed 3D racing game",
                IsInstalled = true,
                Genres = new List<string> { "Racing", "Sports" }
            },
            new GameInfo
            {
                Id = "gostudios_world_explorer",
                Title = "World Explorer",
                Platform = "GoStudios",
                GameType = "3D",
                Description = "Explore vast 3D worlds and discover secrets",
                IsInstalled = true,
                Genres = new List<string> { "Adventure", "Exploration" }
            }
        };
    }

    private void CreateDefaultCatalog(string path)
    {
        var catalog = GetPlaceholderGames();
        var json = JsonSerializer.Serialize(catalog, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
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
