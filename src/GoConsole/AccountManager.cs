using System.Security.Cryptography;
using System.Text;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole;

public class AccountManager
{
    private readonly ProfileManager _profileManager;

    public AccountManager(ProfileManager profileManager)
    {
        _profileManager = profileManager;
    }

    public (bool Success, string Message) Register(string username, string displayName, string password, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Username cannot be empty.");
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password cannot be empty.");
        if (password.Length < 4)
            return (false, "Password must be at least 4 characters.");
        if (username.Equals("guest", StringComparison.OrdinalIgnoreCase))
            return (false, "Cannot register with the name 'guest'.");

        var existing = _profileManager.GetProfileNames();
        if (existing.Contains(username, StringComparer.OrdinalIgnoreCase))
            return (false, $"Username '{username}' is already taken.");

        var hash = HashPassword(password);
        var profile = _profileManager.CreateProfile(username, displayName);
        profile.PasswordHash = hash;
        profile.Email = email;
        profile.IsGuest = false;
        profile.CreatedAt = DateTime.UtcNow;
        _profileManager.SaveProfile(profile);

        Logger.Info($"Account registered: {username}");
        return (true, "Account created successfully!");
    }

    public (bool Success, string Message, UserProfile? Profile) Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Username cannot be empty.", null);
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password cannot be empty.", null);

        var profile = _profileManager.LoadProfile(username);
        if (profile == null)
            return (false, $"No account found for '{username}'.", null);

        if (string.IsNullOrEmpty(profile.PasswordHash))
        {
            if (profile.IsGuest)
            {
                _profileManager.LoadProfile(username);
                return (true, "Signed in as Guest.", profile);
            }
            return (false, "Account has no password set. Contact administrator.", null);
        }

        if (!VerifyPassword(password, profile.PasswordHash))
            return (false, "Incorrect password.", null);

        _profileManager.LoadProfile(username);
        Logger.Info($"Account logged in: {username}");
        return (true, $"Welcome back, {profile.DisplayName}!", profile);
    }

    public (bool Success, string Message) ChangePassword(string username, string currentPassword, string newPassword)
    {
        var profile = _profileManager.LoadProfile(username);
        if (profile == null)
            return (false, "Profile not found.");

        if (!VerifyPassword(currentPassword, profile.PasswordHash))
            return (false, "Current password is incorrect.");

        if (newPassword.Length < 4)
            return (false, "New password must be at least 4 characters.");

        profile.PasswordHash = HashPassword(newPassword);
        _profileManager.SaveProfile(profile);
        Logger.Info($"Password changed for: {username}");
        return (true, "Password changed successfully.");
    }

    public (bool Success, string Message) UpdateDisplayName(string username, string newDisplayName)
    {
        if (string.IsNullOrWhiteSpace(newDisplayName))
            return (false, "Display name cannot be empty.");

        var profile = _profileManager.LoadProfile(username);
        if (profile == null) return (false, "Profile not found.");

        profile.DisplayName = newDisplayName;
        _profileManager.SaveProfile(profile);
        return (true, "Display name updated.");
    }

    public (bool Success, string Message) UpdateAvatar(string username, string? avatarPath)
    {
        var profile = _profileManager.LoadProfile(username);
        if (profile == null) return (false, "Profile not found.");

        profile.AvatarPath = avatarPath;
        _profileManager.SaveProfile(profile);
        return (true, "Avatar updated.");
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password + Convert.ToBase64String(salt)));
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;
        var salt = parts[0];
        var expectedHash = parts[1];
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(password + salt));
        return expectedHash == Convert.ToBase64String(actualHash);
    }
}
