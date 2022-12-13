namespace ChatServiceClientLibrary; 

public class ChatClient {
    internal readonly ChatClientConfiguration Config;
    public string Channel { get; internal set; }
    public string Username => Config.Username;
    public string PublicKey => KeySigning.GetPublicKey(Config.PrivateKey);
    public string PrivateKey => Config.PrivateKey;
    private bool _isConnected;
    public event Action<Message>? OnMessageReceived;
    public event Action<User>? OnUserOnline;
    public event Action<User>? OnUserOffline;
    private Thread? _liveUpdateThread;
    public TrustedUsers TrustedUsers { get; }

    public ChatClient(ChatClientConfiguration config, string channel) {
        Config = config;
        
        // Remove http:// and https:// from urls
        Config.ServerUrl = Config.ServerUrl.Replace("http://", "").Replace("https://", "");
        Config.LiveUpdateUrl = Config.LiveUpdateUrl.Replace("http://", "").Replace("https://", "");
        
        Channel = channel;
        TrustedUsers = new TrustedUsers();
        Config.LoadTrustedUsersFunction ??= TrustedUsers.Load;
        Config.SaveTrustedUsersFunction ??= TrustedUsers.Save;
        Config.LoadTrustedUsersFunction(this);
    }

    public async Task Connect() {
        if (_isConnected) {
            throw new InvalidOperationException("Already connected");
        }
        try {
            await GetOnlineUsers();
        }
        catch (Exception e) {
            throw new Exception("Failed to connect to chat service: " + e.Message, e);
        }
        if (Config.EnableLiveUpdates) {
            await Config.LogFunction("[Chat Client] Starting live update thread");
            _liveUpdateThread = new Thread(LiveUpdateService.Start);
            _liveUpdateThread.Start(this);
        }
        else {
            await Config.LogFunction("[Chat Client] Live update thread has been disabled");
        }
        _isConnected = true;
    }

    public Task<Message> SendMessage(string msg) {
        SentMessage message = new() {
            Text = msg,
            CreatorName = Config.Username,
            Signature = KeySigning.SignText(Config.PrivateKey, msg)
        };
        return HttpRequests.SendMessage(this, message);
    }

    public Task<Message[]> GetMessages(int limit = 10, int offset = 0) {
        return HttpRequests.GetMessages(this, limit, offset);
    }
    
    public Task<User[]> GetOnlineUsers() {
        return HttpRequests.GetOnlineUsers(this);
    }

    internal async void NewMessage(Message msg) {
        if (OnMessageReceived == null) {
            await Config.LogFunction("[Chat Client] OnMessageReceived event is null");
            return;
        }
        await Config.LogFunction("[Chat Client] Invoking OnMessageReceived event");
        OnMessageReceived.Invoke(msg);
    }
    
    internal async void UserOnline(User u) {
        if (OnUserOnline == null) {
            await Config.LogFunction("[Chat Client] OnUserOnline event is null");
            return;
        }
        await Config.LogFunction("[Chat Client] Invoking OnUserOnline event");
        OnUserOnline.Invoke(u);
    }
    
    internal async void UserOffline(User u) {
        if (OnUserOffline == null) {
            await Config.LogFunction("[Chat Client] OnUserOffline event is null");
            return;
        }
        await Config.LogFunction("[Chat Client] Invoking OnUserOffline event");
        OnUserOffline.Invoke(u);
    }

}