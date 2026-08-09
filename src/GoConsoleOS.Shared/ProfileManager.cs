using System.Text.Json;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.Shared;

public class ProfileManager
{
    private readonly string _profilesDir;
    private UserProfile? _currentProfile;

    public UserProfile? CurrentProfile => _currentProfile;
    public event Action<UserProfile>? ProfileLoaded;

    public ProfileManager(string rootPath)
    {
        _profilesDir = ConfigReader.ResolvePath("profiles");
        Directory.CreateDirectory(_profilesDir);

        var guestDir = Path.Combine(_profilesDir, "guest");
        Directory.CreateDirectory(guestDir);
    }

    public List<string> GetProfileNames()
    {
        return Directory.GetDirectories(_profilesDir)
            .Select(Path.GetFileName)
            .Where(n => n != null)
            .Cast<string>()
            .ToList();
    }

    public UserProfile? LoadProfile(string username)
    {
        var path = Path.Combine(_profilesDir, username, "profile.json");
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            var profile = JsonSerializer.Deserialize<UserProfile>(json);
            if (profile != null)
            {
                profile.LastLogin = DateTime.UtcNow;
                _currentProfile = profile;
                ProfileLoaded?.Invoke(profile);
                return profile;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load profile '{username}': {ex.Message}");
        }
        return null;
    }

    public UserProfile CreateProfile(string username, string displayName, bool isGuest = false)
    {
        var profile = new UserProfile
        {
            Username = username,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow,
            IsGuest = isGuest
        };

        SaveProfile(profile);
        _currentProfile = profile;
        ProfileLoaded?.Invoke(profile);
        return profile;
    }

    public UserProfile GetOrCreateGuestProfile()
    {
        var guest = LoadProfile("guest");
        if (guest != null) return guest;
        return CreateProfile("guest", "Guest", true);
    }

    public void SaveProfile(UserProfile profile)
    {
        try
        {
            var dir = Path.Combine(_profilesDir, profile.Username);
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "profile.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save profile '{profile.Username}': {ex.Message}");
        }
    }

    public bool DeleteProfile(string username)
    {
        if (username == "guest") return false;
        try
        {
            var dir = Path.Combine(_profilesDir, username);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to delete profile '{username}': {ex.Message}");
        }
        return false;
    }

    public void AddAchievement(string achievementId, string name, string description, string? iconPath = null)
    {
        if (_currentProfile == null) return;

        if (!_currentProfile.Achievements.ContainsKey(achievementId))
        {
            _currentProfile.Achievements[achievementId] = new UserAchievement
            {
                Id = achievementId,
                Name = name,
                Description = description,
                IsUnlocked = true,
                UnlockedAt = DateTime.UtcNow,
                IconPath = iconPath
            };
            SaveProfile(_currentProfile);
            Logger.Info($"Achievement unlocked: {name}");
        }
    }

    public void AddPlaytime(int minutes)
    {
        if (_currentProfile == null) return;
        _currentProfile.TotalPlaytimeMinutes += minutes;
    }

    public void ToggleFavorite(string gameId)
    {
        if (_currentProfile == null) return;
        if (_currentProfile.FavoriteGames.Contains(gameId))
            _currentProfile.FavoriteGames.Remove(gameId);
        else
            _currentProfile.FavoriteGames.Add(gameId);
        SaveProfile(_currentProfile);
    }
}
