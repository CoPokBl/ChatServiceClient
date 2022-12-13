using System.Text.Json;

namespace ChatServiceClientLibrary; 

public class TrustedUsers {
    private List<User> _trustedUsersList;
    
    public TrustedUsers() {
        _trustedUsersList = new List<User>();
    }
    
    public bool IsMessageFromTrustedUser(Message message) {
        if (_trustedUsersList == null) {
            throw new Exception("Trusted users list is null, please initialize it first");
        }
        User[]? peopleWithThatName = _trustedUsersList.Where(msg => msg.Username == message.CreatorName)?.ToArray();
        if (peopleWithThatName == null) {
            throw new Exception("Enumerable.Where returned null");
        }
        return peopleWithThatName.Length != 0 && peopleWithThatName.Any(usr => KeySigning.VerifySignature(usr.PublicKey, message.Signature, message.Text));
    }
    
    public void TrustUser(User user) {
        _trustedUsersList.Add(user);
    }
    
    public void TrustUser(string username, string pubkey) {
        _trustedUsersList.Add(new User {
            Username = username, PublicKey = pubkey
        });
    }
    
    public bool IsTrusted(User user) {
        return _trustedUsersList.Any(usr => usr == user);
    }
    
    public bool IsTrusted(string username, string pubkey) {
        return _trustedUsersList.Any(usr => usr.Username == username && usr.PublicKey == pubkey);
    }
    
    public void UntrustUser(User user) {
        _trustedUsersList.Remove(user);
    }
    
    public void ClearTrustedUsers() {
        _trustedUsersList.Clear();
    }

    public void SaveData(ChatClient client) {
        client.Config.SaveTrustedUsersFunction!(_trustedUsersList);
    }

    internal Task Save(List<User> users) {
        string json = JsonSerializer.Serialize(users);
        File.WriteAllText("trustedusers.json", json);
        return Task.CompletedTask;
    }
    
    internal List<User> Load(ChatClient client) {
        if (!File.Exists("trustedusers.json")) return new List<User>();
        string json = File.ReadAllText("trustedusers.json");
        List<User> users = JsonSerializer.Deserialize<List<User>>(json)!;
        _trustedUsersList = users;
        if (users != null) return users;
        _trustedUsersList = new List<User>();
        return new List<User>();
    }
}