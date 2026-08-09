using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using GoConsoleOS.Shared;
using NAudio.Wave;

namespace GoConsoleOS.GoConsole.Views;

public partial class DiscordView : UserControl
{
    private DiscordSocketClient? _client;
    private IAudioClient? _audioClient;
    private AudioOutStream? _sendStream;
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _playbackProvider;
    private bool _connecting;

    private SocketGuild? _currentGuild;
    private ulong _currentChannelId;
    private SocketVoiceChannel? _voiceChannel;
    private Discord.IDMChannel? _dmChannel;    private bool _friendsMode;

    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private readonly ObservableCollection<MessageItem> _messages = new();

    private string _token = "";
    private string _configPath = "";

    public DiscordView()
    {
        InitializeComponent();
        MessagesList.ItemsSource = _messages;
        TokenTypeCombo.SelectedIndex = 0;
        LoadConfig();
        UpdateConnectUi();
    }

    private void LoadConfig()
    {
        _configPath = Path.Combine(ConfigReader.RootPath ?? "", "system", "discord", "config.json");
        try
        {
            if (File.Exists(_configPath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(_configPath));
                if (doc.RootElement.TryGetProperty("token", out var t)) _token = t.GetString() ?? "";
                var tt = doc.RootElement.TryGetProperty("tokenType", out var ttProp) ? ttProp.GetString() : "bot";
                TokenTypeCombo.SelectedIndex = string.Equals(tt, "user", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Discord config load: {ex.Message}");
        }
        TokenDisplayText.Text = string.IsNullOrEmpty(_token) ? "Not set" : "••••••••••••••";
    }

    private void SaveConfig()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            var json = JsonSerializer.Serialize(new
            {
                token = _token,
                tokenType = TokenTypeCombo.SelectedIndex == 1 ? "user" : "bot"
            });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Discord config save: {ex.Message}");
        }
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (_connecting) return;
        if (_client != null && _client.ConnectionState == ConnectionState.Connected) return;
        if (string.IsNullOrWhiteSpace(_token))
        {
            ConnStatusText.Text = "Set a token first (CHANGE)";
            return;
        }

        _connecting = true;
        UpdateConnectUi();
        SetStatus("Connecting to Discord...", "offline");

        try
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
                               | GatewayIntents.MessageContent | GatewayIntents.GuildVoiceStates
                               | GatewayIntents.DirectMessages,
                LogLevel = LogSeverity.Warning,
                MessageCacheSize = 256
            };

            _client = new DiscordSocketClient(config);
            _client.Log += OnDiscordLog;
            _client.Ready += OnReady;
            _client.MessageReceived += OnMessageReceived;
            _client.GuildAvailable += OnGuildAvailable;
            _client.UserVoiceStateUpdated += OnVoiceStateUpdated;

            var tokenType = TokenTypeCombo.SelectedIndex == 1 ? TokenType.Bearer : TokenType.Bot;
            await _client.LoginAsync(tokenType, _token);
            await _client.StartAsync();
            SaveConfig();

            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (_client.ConnectionState != ConnectionState.Connected && DateTime.UtcNow < deadline)
                await Task.Delay(250);

            if (_client.ConnectionState == ConnectionState.Connected)
            {
                SetStatus("Connected", "online");
                RefreshGuilds();
            }
            else
            {
                SetStatus("Connection timed out — check token / intents", "offline");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Discord connect: {ex}");
            SetStatus("Failed: " + ex.Message, "offline");
        }
        finally
        {
            _connecting = false;
            UpdateConnectUi();
        }
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        await LeaveCallInternal();
        if (_client != null)
        {
            try { await _client.StopAsync(); await _client.LogoutAsync(); } catch { }
            _client = null;
        }
        _currentGuild = null;
        _currentChannelId = 0;
        _voiceChannel = null;
        _dmChannel = null;
        ServersList.ItemsSource = null;
        ChannelsList.ItemsSource = null;
        _messages.Clear();
        SetStatus("Disconnected", "offline");
        UpdateConnectUi();
    }

    private void UpdateConnectUi()
    {
        var connected = _client != null && _client.ConnectionState == ConnectionState.Connected;
        ConnectBtn.IsEnabled = !_connecting && !connected;
        DisconnectBtn.Visibility = connected ? Visibility.Visible : Visibility.Collapsed;
        TokenTypeCombo.IsEnabled = !connected;
        if (!connected)
        {
            JoinCallBtn.IsEnabled = false;
            VoiceBar.Visibility = Visibility.Collapsed;
        }
        else
        {
            UpdateVoiceUi();
        }
    }

    private Task OnDiscordLog(LogMessage msg)
    {
        if (msg.Severity >= LogSeverity.Warning)
            Logger.Warn($"Discord: {msg.Message}");
        return Task.CompletedTask;
    }

    private Task OnReady()
    {
        Logger.Info("Discord ready");
        return Task.CompletedTask;
    }

    private Task OnGuildAvailable(SocketGuild guild)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_client != null && _client.ConnectionState == ConnectionState.Connected && _currentGuild == null)
                RefreshGuilds();
        }));
        return Task.CompletedTask;
    }

    private void RefreshGuilds()
    {
        if (_client == null) return;

        var items = _client.Guilds
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GuildItem
            {
                Id = g.Id,
                Name = g.Name,
                Initial = g.Name.Length > 0 ? g.Name[..1].ToUpperInvariant() : "?",
                IsSelected = _currentGuild?.Id == g.Id
            })
            .ToList();

        ServersList.ItemsSource = items;

        if (_currentGuild == null && items.Count > 0)
            SelectGuildById(items[0].Id);
    }

    private void SelectGuild(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is GuildItem item)
            SelectGuildById(item.Id);
    }

    private void SelectGuildById(ulong id)
    {
        _currentGuild = _client?.GetGuild(id);
        _currentChannelId = 0;
        _voiceChannel = null;

        if (ServersList.ItemsSource is IEnumerable<GuildItem> guilds)
            foreach (var g in guilds)
                g.IsSelected = g.Id == id;

        RefreshChannels();
        UpdateVoiceUi();
        _messages.Clear();
    }

    private void RefreshChannels()
    {
        ChannelsList.ItemsSource = null;
        if (_currentGuild == null) return;

        var list = new List<ChannelItem>();
        foreach (var ch in _currentGuild.Channels.OrderBy(c => c.Position))
        {
            switch (ch)
            {
                case SocketVoiceChannel vc:
                    list.Add(new ChannelItem
                    {
                        Id = vc.Id,
                        Name = vc.Name,
                        Type = "voice",
                        Icon = "🔊",
                        TextBrush = "#8888AA"
                    });
                    break;
                case SocketTextChannel tc:
                    list.Add(new ChannelItem
                    {
                        Id = tc.Id,
                        Name = tc.Name,
                        Type = "text",
                        Icon = "#",
                        TextBrush = "#F0F0FF",
                        IsSelected = tc.Id == _currentChannelId
                    });
                    break;
                case SocketCategoryChannel cat:
                    list.Add(new ChannelItem { Id = cat.Id, Name = cat.Name, Type = "category" });
                    break;
            }
        }

        ChannelsList.ItemsSource = list;
    }

    private async void SelectChannel(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not ChannelItem item) return;
        if (item.Type == "category") return;

        if (item.Type == "voice")
        {
            _voiceChannel = _currentGuild?.GetVoiceChannel(item.Id);
            _currentChannelId = 0;
            MarkChannels();
            UpdateVoiceUi();
        }
        else if (item.Type == "friend")
        {
            await OpenDmWithFriend(item.Id);
        }
        else
        {
            _currentChannelId = item.Id;
            _dmChannel = null;
            _voiceChannel = null;
            MarkChannels();
            UpdateVoiceUi();
            LoadMessages();
        }
    }

    private async Task OpenDmWithFriend(ulong userId)
    {
        if (_client == null) return;
        try
        {
            SetStatus("Opening DM...", "connecting");
            var dm = await _client.GetDMChannelAsync(userId);
            if (dm == null) return;
            _dmChannel = dm;
            _currentChannelId = dm.Id;
            _voiceChannel = null;
            MarkChannels();
            UpdateVoiceUi();
            LoadMessages();
            SetStatus("Connected", "online");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Discord DM open: {ex.Message}");
            ConnStatusText.Text = "Could not open DM: " + ex.Message;
            SetStatus("Connected", "online");
        }
    }

    private void MarkChannels()
    {
        if (ChannelsList.ItemsSource is IEnumerable<ChannelItem> channels)
            foreach (var c in channels)
                c.IsSelected = c.Type == "text" && c.Id == _currentChannelId;
    }

    private async void LoadMessages()
    {
        if (_currentChannelId == 0) return;

        _messages.Clear();
        try
        {
            IEnumerable<IMessage> messages;
            if (_dmChannel != null)
            {
                messages = await _dmChannel.GetMessagesAsync(50).FlattenAsync();
            }
            else
            {
                if (_currentGuild == null) return;
                var channel = _currentGuild.GetTextChannel(_currentChannelId);
                if (channel == null) return;
                messages = await channel.GetMessagesAsync(50).FlattenAsync();
            }
            foreach (var m in messages.Reverse())
                AddMessage(m);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Discord fetch messages: {ex.Message}");
            ConnStatusText.Text = "Could not load messages: " + ex.Message;
        }
    }

    private void AddMessage(IMessage message)
    {
        var author = message.Author?.Username ?? "Unknown";
        var content = string.IsNullOrWhiteSpace(message.Content) ? "[attachment or embed]" : message.Content;
        _messages.Add(new MessageItem
        {
            Author = author,
            Content = content,
            Time = message.CreatedAt.LocalDateTime.ToString("HH:mm")
        });
        ScrollMessages();
    }

    private void ScrollMessages()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (MessagesScroll.ExtentHeight > MessagesScroll.ViewportHeight)
                MessagesScroll.ScrollToEnd();
        }), DispatcherPriority.Background);
    }

    private Task OnMessageReceived(SocketMessage message)
    {
        if (message.Channel.Id != _currentChannelId) return Task.CompletedTask;
        Dispatcher.BeginInvoke(new Action(() => AddMessage(message)));
        return Task.CompletedTask;
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        var text = MessageInput.Text.Trim();
        if (string.IsNullOrEmpty(text) || _currentChannelId == 0 || _client == null) return;

        MessageInput.Text = "";
        try
        {
            if (_dmChannel != null)
            {
                await _dmChannel.SendMessageAsync(text, allowedMentions: AllowedMentions.None);
            }
            else
            {
                if (_currentGuild == null) return;
                var channel = _currentGuild.GetTextChannel(_currentChannelId);
                if (channel == null) return;
                await channel.SendMessageAsync(text, allowedMentions: AllowedMentions.None);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Discord send: {ex.Message}");
            ConnStatusText.Text = "Send failed: " + ex.Message;
        }
    }

    private void ChangeToken_Click(object sender, MouseButtonEventArgs e)
    {
        var kb = new OnScreenKeyboard { Owner = Window.GetWindow(this) };
        if (kb.ShowDialog() == true && !string.IsNullOrWhiteSpace(kb.InputText))
        {
            _token = kb.InputText.Trim();
            TokenDisplayText.Text = "••••••••••••••";
            SaveConfig();
        }
    }

    private void KeyButton_Click(object sender, MouseButtonEventArgs e)
    {
        var kb = new OnScreenKeyboard { Owner = Window.GetWindow(this) };
        if (kb.ShowDialog() == true && !string.IsNullOrWhiteSpace(kb.InputText))
        {
            _token = kb.InputText.Trim();
            TokenDisplayText.Text = "••••••••••••••";
            SaveConfig();
            ConnStatusText.Text = "Key saved — press CONNECT";
            SoundManager.Play("select");
        }
    }

    private void CreateToken_Click(object sender, MouseButtonEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
            main.NavigateTo("tokencreator");
    }

    private void Keyboard_Click(object sender, MouseButtonEventArgs e)
    {
        var kb = new OnScreenKeyboard { Owner = Window.GetWindow(this) };
        if (kb.ShowDialog() == true)
            MessageInput.Text = kb.InputText;
    }

    // ---- Friends ----

    private void Tab_Click(object sender, MouseButtonEventArgs e)
    {
        var tag = (sender as FrameworkElement)?.Tag?.ToString();
        _friendsMode = tag == "friends";
        TabChannels.Foreground = _friendsMode
            ? (System.Windows.Media.Brush)FindResource("BrushTextSecondary")
            : (System.Windows.Media.Brush)FindResource("BrushAccentSecondary");
        TabFriends.Foreground = _friendsMode
            ? (System.Windows.Media.Brush)FindResource("BrushAccentSecondary")
            : (System.Windows.Media.Brush)FindResource("BrushTextSecondary");

        if (_friendsMode)
            RefreshFriends();
        else
            RefreshChannels();
        UpdateVoiceUi();
    }

    private async void RefreshFriends()
    {
        ChannelsList.ItemsSource = null;
        var connected = _client != null && _client.ConnectionState == ConnectionState.Connected;
        if (!connected || string.IsNullOrEmpty(_token))
        {
            VoiceChannelInfo.Text = "Connect to Discord to see friends";
            return;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me/relationships");
            req.Headers.Add("Authorization", _token);
            req.Headers.Add("User-Agent", "DiscordBot (https://github.com/discord-net/Discord.Net, 3.20.1)");
            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                VoiceChannelInfo.Text = resp.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "Friends require a user token (CREATE TOKEN)"
                    : $"Friends failed: HTTP {(int)resp.StatusCode}";
                return;
            }

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var list = new List<ChannelItem>();
            foreach (var rel in doc.RootElement.EnumerateArray())
            {
                if (!rel.TryGetProperty("user", out var user)) continue;
                if (!user.TryGetProperty("id", out var idEl) || !ulong.TryParse(idEl.GetString(), out var uid)) continue;
                var relType = rel.TryGetProperty("type", out var t) ? t.GetInt32() : 1;
                if (relType == 2) continue;

                var username = user.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "";
                var display = user.TryGetProperty("global_name", out var gn) && gn.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(gn.GetString())
                    ? gn.GetString()! : username;
                var statusText = relType == 3 ? "Incoming request" : relType == 4 ? "Outgoing request" : "";
                var icon = relType switch
                {
                    3 => "👋",
                    4 => "⏳",
                    _ => PresenceDot(_client?.GetUser(uid)?.Status)
                };
                list.Add(new ChannelItem
                {
                    Id = uid,
                    Name = display,
                    Type = "friend",
                    Icon = icon,
                    TextBrush = relType == 3 ? "#FFD600" : "#F0F0FF",
                    Subtitle = relType == 3 ? "Friend request — click to message" : statusText
                });
            }

            list = list.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
            ChannelsList.ItemsSource = list;
            VoiceChannelInfo.Text = list.Count > 0
                ? $"{list.Count} friend(s) — click to open a DM"
                : "No friends yet — use + ADD FRIEND";
        }
        catch (Exception ex)
        {
            Logger.Warn($"Discord friends: {ex.Message}");
            VoiceChannelInfo.Text = "Could not load friends: " + ex.Message;
        }
    }

    private static string PresenceDot(Discord.UserStatus? status) => status switch
    {
        Discord.UserStatus.Online => "🟢",
        Discord.UserStatus.Idle => "🌙",
        Discord.UserStatus.DoNotDisturb => "🔴",
        _ => "⚫"
    };

    private async void AddFriend_Click(object sender, RoutedEventArgs e)
    {
        var kb = new OnScreenKeyboard { Owner = Window.GetWindow(this) };
        if (kb.ShowDialog() != true || string.IsNullOrWhiteSpace(kb.InputText)) return;
        var name = kb.InputText.Trim();

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/v10/users/@me/relationships")
            {
                Content = new StringContent(JsonSerializer.Serialize(new { username = name }), Encoding.UTF8, "application/json")
            };
            req.Headers.Add("Authorization", _token);
            req.Headers.Add("User-Agent", "DiscordBot (https://github.com/discord-net/Discord.Net, 3.20.1)");
            using var resp = await _http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                VoiceChannelInfo.Text = $"Request sent to {name}";
            }
            else
            {
                var err = "Could not add friend";
                try
                {
                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                    if (doc.RootElement.TryGetProperty("message", out var m)) err = m.GetString() ?? err;
                }
                catch { }
                VoiceChannelInfo.Text = $"Add failed ({err})";
            }
            RefreshFriends();
        }
        catch (Exception ex)
        {
            Logger.Warn($"Discord add friend: {ex.Message}");
            VoiceChannelInfo.Text = "Add friend failed: " + ex.Message;
        }
    }

    // ---- Voice ----

    private async void JoinCall_Click(object sender, RoutedEventArgs e)
    {
        if (_voiceChannel == null || _client == null || _audioClient != null) return;

        try
        {
            SetStatus($"Joining call: {_voiceChannel.Name}...", "connecting");
            _audioClient = await _voiceChannel.ConnectAsync();
            if (_audioClient == null)
            {
                SetStatus("Could not connect to voice", "offline");
                return;
            }

            _audioClient.Disconnected += OnVoiceDisconnected;
            _audioClient.StreamCreated += OnVoiceStreamCreated;
            await _audioClient.SetSpeakingAsync(true);
            _sendStream = _audioClient.CreatePCMStream(AudioApplication.Mixed);

            StartMicCapture();
            StartSpeaker();

            SetStatus("Connected", "online");
            UpdateVoiceUi();
            UpdateVoiceMembers();
        }
        catch (Exception ex)
        {
            Logger.Error($"Discord voice join: {ex}");
            SetStatus("Voice failed: " + ex.Message, "offline");
            await LeaveCallInternal();
        }
    }

    private void StartMicCapture()
    {
        try
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(48000, 16, 2),
                BufferMilliseconds = 20,
                DeviceNumber = 0
            };
            _waveIn.DataAvailable += (_, e) =>
            {
                try
                {
                    if (_sendStream != null && _sendStream.CanWrite)
                        _sendStream.Write(e.Buffer, 0, e.BytesRecorded);
                }
                catch { }
            };
            _waveIn.StartRecording();
        }
        catch (Exception ex)
        {
            Logger.Warn($"Microphone unavailable: {ex.Message}");
        }
    }

    private void StartSpeaker()
    {
        try
        {
            _playbackProvider = new BufferedWaveProvider(new WaveFormat(48000, 16, 2))
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromSeconds(5)
            };
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_playbackProvider);
            _waveOut.Play();
        }
        catch (Exception ex)
        {
            Logger.Warn($"Speaker unavailable: {ex.Message}");
        }
    }

    private Task OnVoiceStreamCreated(ulong userId, AudioInStream stream)
    {
        _ = Task.Run(async () =>
        {
            var buffer = new byte[48000 * 2 * 2];
            while (true)
            {
                int read;
                try { read = await stream.ReadAsync(buffer, 0, buffer.Length); }
                catch { break; }
                if (read <= 0) break;
                try { _playbackProvider?.AddSamples(buffer, 0, read); }
                catch { break; }
            }
        });
        return Task.CompletedTask;
    }

    private Task OnVoiceDisconnected(Exception ex)
    {
        Logger.Warn($"Discord voice disconnected: {ex?.Message}");
        Dispatcher.BeginInvoke(new Action(async () =>
        {
            await LeaveCallInternal();
            SetStatus(_client != null && _client.ConnectionState == ConnectionState.Connected ? "Connected" : "Disconnected", _client != null && _client.ConnectionState == ConnectionState.Connected ? "online" : "offline");
        }));
        return Task.CompletedTask;
    }

    private Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (_voiceChannel != null && (before.VoiceChannel == _voiceChannel || after.VoiceChannel == _voiceChannel))
            Dispatcher.BeginInvoke(new Action(UpdateVoiceMembers));
        return Task.CompletedTask;
    }

    private void UpdateVoiceMembers()
    {
        if (_voiceChannel == null)
        {
            VoiceMembersText.Text = "";
            return;
        }
        var names = _voiceChannel.Users.Select(u => u.DisplayName).ToList();
        if (_client?.CurrentUser != null)
        {
            var me = names.FirstOrDefault(n => string.Equals(n, _client.CurrentUser.Username, StringComparison.OrdinalIgnoreCase));
            if (me != null) names[names.IndexOf(me)] = me + " (you)";
        }
        VoiceMembersText.Text = string.Join("  •  ", names);
    }

    private void UpdateVoiceUi()
    {
        var inCall = _audioClient != null;
        var connected = _client != null && _client.ConnectionState == ConnectionState.Connected;

        VoiceBar.Visibility = inCall ? Visibility.Visible : Visibility.Collapsed;
        JoinCallBtn.Visibility = _friendsMode ? Visibility.Collapsed : Visibility.Visible;
        AddFriendBtn.Visibility = _friendsMode && connected ? Visibility.Visible : Visibility.Collapsed;
        JoinCallBtn.IsEnabled = _voiceChannel != null && connected && !inCall;

        if (_voiceChannel != null)
            VoiceChannelInfo.Text = inCall
                ? $"Connected to {_voiceChannel.Name}"
                : $"Ready to call: {_voiceChannel.Name}";
        else if (connected)
            VoiceChannelInfo.Text = "Select a voice channel to call";
        else
            VoiceChannelInfo.Text = "Connect to Discord to use calls";

        if (inCall && _voiceChannel != null)
        {
            CallInfoText.Text = _voiceChannel.Name;
            UpdateVoiceMembers();
        }
    }

    private async void LeaveCall_Click(object sender, RoutedEventArgs e)
    {
        await LeaveCallInternal();
        SetStatus(_client != null && _client.ConnectionState == ConnectionState.Connected ? "Connected" : "Disconnected", _client != null && _client.ConnectionState == ConnectionState.Connected ? "online" : "offline");
    }

    private async Task LeaveCallInternal()
    {
        try
        {
            if (_waveIn != null)
            {
                _waveIn.StopRecording();
                _waveIn.Dispose();
                _waveIn = null;
            }
            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
            if (_sendStream != null)
            {
                _sendStream.Dispose();
                _sendStream = null;
            }
            if (_audioClient != null)
            {
                try { await _audioClient.StopAsync(); } catch { }
                _audioClient = null;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Voice leave: {ex.Message}");
        }
        UpdateVoiceUi();
    }

    private void SetStatus(string text, string mode)
    {
        ConnStatusText.Text = text;
        StatusChip.Text = mode switch
        {
            "online" => "ONLINE",
            "connecting" => "CONNECTING",
            _ => "OFFLINE"
        };
        StatusChip.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.ColorConverter.ConvertFromString(mode switch
        {
            "online" => "#00E676",
            "connecting" => "#FFD600",
            _ => "#555577"
        }) as System.Windows.Media.Color? ?? System.Windows.Media.Colors.Gray);
    }

    // ---- Item models ----

    public class GuildItem : INotifyPropertyChanged
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = "";
        public string Initial { get; set; } = "?";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class ChannelItem : INotifyPropertyChanged
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "text";
        public string Icon { get; set; } = "#";
        public string TextBrush { get; set; } = "#F0F0FF";
        public string Subtitle { get; set; } = "";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class MessageItem
    {
        public string Author { get; set; } = "";
        public string Content { get; set; } = "";
        public string Time { get; set; } = "";
    }
}
