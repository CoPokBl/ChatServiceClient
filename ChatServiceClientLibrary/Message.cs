using System.Text.Json.Serialization;

namespace ChatServiceClientLibrary; 

public class Message {
    
    [JsonPropertyName("MessageId")]
    public string MessageId { get; set; }
    
    [JsonPropertyName("CreatorName")]
    public string CreatorName { get; set; }
    
    [JsonPropertyName("Text")]
    public string Text { get; set; }
    
    [JsonPropertyName("CreatedAt")]
    public long CreatedAt { get; set; }
    
    [JsonPropertyName("Signature")]
    public string Signature { get; set; }

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