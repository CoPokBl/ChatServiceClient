namespace ChatServiceClientLibrary; 

public class ChatClientConfiguration {
    public string Username { get; set; } = null!;
    public string PrivateKey { get; set; } = null!;
    public string ServerUrl { get; set; } = null!;
    public string LiveUpdateUrl { get; set; } = null!;
    public Func<string, Task> LogFunction { get; set; } = null!;
    public Func<ChatClient, List<User>>? LoadTrustedUsersFunction { get; set; }
    public Func<List<User>, Task>? SaveTrustedUsersFunction { get; set; }
    public bool EnableLiveUpdates { get; set; } = true;

    public ChatClientConfiguration() {
        PrivateKey = KeySigning.GenerateKeyPair();
    }

    public ChatClientConfiguration(string username, string serverUrl, string liveUpdateUrl, string privateKey) {
        Username = username;
        ServerUrl = serverUrl;
        PrivateKey = privateKey;
        LiveUpdateUrl = liveUpdateUrl;
    }
    
    public ChatClientConfiguration(string username, string serverUrl, string liveUpdateUrl) {
        Username = username;
        ServerUrl = serverUrl;
        PrivateKey = KeySigning.GenerateKeyPair();
        LiveUpdateUrl = liveUpdateUrl;
    }
}