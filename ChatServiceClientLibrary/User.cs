namespace ChatServiceClientLibrary; 

public class User : IComparable<User> {
    public string Username { get; set; } = null!;
    public string PublicKey { get; set; } = null!;

    public int CompareTo(User? obj) {
        return obj switch {
            null => 0,
            _ => string.Compare(Username, obj.Username, StringComparison.Ordinal)
        };
    }

    public bool IsClient(ChatClient client) {
        return client.PublicKey == PublicKey && client.Username == Username;
    }
}