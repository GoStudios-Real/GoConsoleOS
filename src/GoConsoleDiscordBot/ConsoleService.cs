using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace GoConsoleDiscordBot;

public class ConsoleService : IDisposable
{
    private readonly BotConfig _config;
    private readonly HttpClient _http;
    private string? _connectedConsoleAddress;
    private string? _connectedConsoleName;
    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public string? ConsoleName => _connectedConsoleName;
    public string? ConsoleAddress => _connectedConsoleAddress;

    public ConsoleService(BotConfig config)
    {
        _config = config;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task<bool> TryConnectToLocalConsole()
    {
        try
        {
            var response = await _http.GetStringAsync($"http://localhost:{_config.ConsoleApiPort}/api/console");
            var json = JsonSerializer.Deserialize<JsonElement>(response);
            if (json.GetProperty("success").GetBoolean())
            {
                _connectedConsoleAddress = $"http://localhost:{_config.ConsoleApiPort}";
                _connectedConsoleName = json.TryGetProperty("name", out var n) ? n.GetString() ?? "GoConsoleOS" : "GoConsoleOS";
                _isConnected = true;
                return true;
            }
        }
        catch { }
        return false;
    }

    public async Task<bool> TryConnectToCloud()
    {
        try
        {
            var response = await _http.GetStringAsync($"{_config.CloudServerUrl}/console");
            var json = JsonSerializer.Deserialize<JsonElement>(response);
            if (json.GetProperty("success").GetBoolean())
            {
                _connectedConsoleAddress = _config.CloudServerUrl;
                _connectedConsoleName = json.TryGetProperty("name", out var n) ? n.GetString() ?? "GoConsoleOS Cloud" : "GoConsoleOS Cloud";
                _isConnected = true;
                return true;
            }
        }
        catch { }
        return false;
    }

    public void Disconnect()
    {
        _connectedConsoleAddress = null;
        _connectedConsoleName = null;
        _isConnected = false;
    }

    public async Task<List<GameState>> GetGamesAsync()
    {
        if (!_isConnected || _connectedConsoleAddress == null) return new List<GameState>();
        try
        {
            var response = await _http.GetStringAsync($"{_connectedConsoleAddress}/api/games");
            var json = JsonSerializer.Deserialize<JsonElement>(response);
            var gamesArray = json.GetProperty("games");
            var games = new List<GameState>();
            foreach (var g in gamesArray.EnumerateArray())
            {
                games.Add(new GameState
                {
                    Title = g.TryGetProperty("title", out var t) ? t.GetString() ?? "Unknown" : "Unknown",
                    Platform = g.TryGetProperty("platform", out var p) ? p.GetString() ?? "" : "",
                    IsRunning = g.TryGetProperty("running", out var r) && r.GetBoolean(),
                });
            }
            return games;
        }
        catch { return new List<GameState>(); }
    }

    public async Task<bool> LaunchGameAsync(string title)
    {
        if (!_isConnected || _connectedConsoleAddress == null) return false;
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { title }),
                System.Text.Encoding.UTF8,
                "application/json");
            var response = await _http.PostAsync($"{_connectedConsoleAddress}/api/games/launch", content);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<ConsoleState> GetConsoleStateAsync()
    {
        if (!_isConnected || _connectedConsoleAddress == null)
            return new ConsoleState { IsOnline = false };

        try
        {
            var response = await _http.GetStringAsync($"{_connectedConsoleAddress}/api/console");
            var json = JsonSerializer.Deserialize<JsonElement>(response);
            return new ConsoleState
            {
                IsOnline = true,
                Name = json.TryGetProperty("name", out var n) ? n.GetString() ?? "GoConsoleOS" : "GoConsoleOS",
                Version = json.TryGetProperty("version", out var v) ? v.GetString() ?? "2.2.0" : "2.2.0",
                CpuUsage = json.TryGetProperty("cpu", out var c) ? c.GetDouble() : 0,
                RamUsageMb = json.TryGetProperty("ram", out var r) ? r.GetDouble() : 0,
                Uptime = json.TryGetProperty("uptime", out var u) ? u.GetString() ?? "Unknown" : "Unknown",
            };
        }
        catch
        {
            return new ConsoleState { IsOnline = false };
        }
    }

    public async Task<List<UsbDevice>> GetUsbDevicesAsync()
    {
        var devices = new List<UsbDevice>();
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable && d.IsReady);
            foreach (var drive in drives)
            {
                var initCfg = Path.Combine(drive.RootDirectory.FullName, "boot", "init.cfg");
                var isGoConsole = File.Exists(initCfg);
                var content = isGoConsole ? await File.ReadAllTextAsync(initCfg) : "";
                var osName = content.Contains("os_name=GoConsoleOS") ? "GoConsoleOS" : "Unknown USB";

                devices.Add(new UsbDevice
                {
                    DriveLetter = drive.Name,
                    Label = drive.VolumeLabel,
                    TotalSizeGb = drive.TotalSize / (1024.0 * 1024 * 1024),
                    FreeSpaceGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024),
                    IsGoConsole = isGoConsole,
                    OsName = osName,
                });
            }
        }
        catch { }
        return devices;
    }

    public string GetInviteUrl(ulong clientId)
    {
        return $"https://discord.com/oauth2/authorize?client_id={clientId}&permissions=8&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A53178%2F&integration_type=0&scope=bot";
    }

    public void Dispose()
    {
        _http?.Dispose();
    }
}

public class GameState
{
    public string Title { get; set; } = "";
    public string Platform { get; set; } = "";
    public bool IsRunning { get; set; }
}

public class ConsoleState
{
    public bool IsOnline { get; set; }
    public string Name { get; set; } = "GoConsoleOS";
    public string Version { get; set; } = "2.2.0";
    public double CpuUsage { get; set; }
    public double RamUsageMb { get; set; }
    public string Uptime { get; set; } = "Unknown";
}

public class UsbDevice
{
    public string DriveLetter { get; set; } = "";
    public string Label { get; set; } = "";
    public double TotalSizeGb { get; set; }
    public double FreeSpaceGb { get; set; }
    public bool IsGoConsole { get; set; }
    public string OsName { get; set; } = "";
}
