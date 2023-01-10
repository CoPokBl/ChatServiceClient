using System.Text.Json.Serialization;

namespace ChatServiceClientLibrary; 

public class Message {
    
    [JsonPropertyName("MessageId")]
    public string MessageId { get; set; } = null!;

    [JsonPropertyName("CreatorName")]
    public string CreatorName { get; set; } = null!;

    [JsonPropertyName("Text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("CreatedAt")]
    public long CreatedAt { get; set; }
    
    [JsonPropertyName("Signature")]
    public string Signature { get; set; } = null!;

    public Message() { }
    
    public bool VerifySignature(string publicKey) {
        return KeySigning.VerifySignature(publicKey, Signature, Text);
    }
    
    public bool WasSentByClient(ChatClient client) {
        return VerifySignature(KeySigning.GetPublicKey(client.Config.PrivateKey));
    }

    public bool WasSentByVerifiedUser(ChatClient client) {
        return client.TrustedUsers.IsMessageFromTrustedUser(this);
    }

}