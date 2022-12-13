using System.Net.Http.Json;
using System.Text.Json;

namespace ChatServiceClientLibrary; 

internal static class HttpRequests {

    public static async Task<Message> SendMessage(ChatClient chatClient, SentMessage msg) {
        // Send POST request to the server
        HttpClient client = new();
        string json = JsonSerializer.Serialize(msg);
        HttpResponseMessage response = 
            await client.PostAsync($"http://{chatClient.Config.ServerUrl}/channel/{chatClient.Channel}", new StringContent(json));
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(responseBody);
        JsonElement root = doc.RootElement;
        if (root.TryGetProperty("Error", out JsonElement error)) {
            throw new Exception(error.GetString());
        }
        
        // Deserialize the response to a message object
        Message receivedMessage = JsonSerializer.Deserialize<Message>(responseBody)!;
        return receivedMessage;
    }
    
    public static async Task<Message[]> GetMessages(ChatClient chatClient, int limit, int offset) {
        // Send POST request to the server
        HttpClient client = new();
        HttpResponseMessage response = 
            await client.GetAsync($"http://{chatClient.Config.ServerUrl}/channel/{chatClient.Channel}?limit={limit}&offset={offset}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Message[]>())!;
    }
    
    public static async Task<User[]> GetOnlineUsers(ChatClient chatClient) {
        // Send POST request to the server
        HttpClient client = new();
        HttpResponseMessage response = 
            await client.GetAsync($"http://{chatClient.Config.ServerUrl}/online");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<User[]>())!;
    }
    
}