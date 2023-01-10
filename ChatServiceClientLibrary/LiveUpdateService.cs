using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System;

namespace ChatServiceClientLibrary; 

internal static class LiveUpdateService {

    public static CancellationTokenSource CancellationTokenSource = new();

    public static void Start(object? invoker) {
        if (invoker is not ChatClient invokerClient) {
            throw new ArgumentException("invoker must be of type ChatClient");
        }

        invokerClient.Config.LogFunction("[Live Update Service] Starting...");

        while (!CancellationTokenSource.IsCancellationRequested) {
            try {
                ConnectInstance(invokerClient);
            }
            catch (Exception e) {
                invokerClient.Config.LogFunction($"[Live Update Service] {e}");
            }
        }
        invokerClient.Config.LogFunction($"[Live Update Service] Exiting...");
    }

    private static void ConnectInstance(ChatClient invokerClient) {
        string ip = invokerClient.Config.LiveUpdateUrl.Split(':')[0];
        int port = int.Parse(invokerClient.Config.LiveUpdateUrl.Split(':')[1]);
        TcpClient client = new(ip, port);
        NetworkStream stream = client.GetStream();
        invokerClient.Config.LogFunction("[Live Update Service] Connected to server");
        
        CancellationTokenSource.Token.Register(() => {
            try {
                client.Close();
            }
            catch (Exception) {
                invokerClient.Config.LogFunction("[Live Update Service] Failed to close client");
            }
            try {
                stream.Close();
            }
            catch (Exception) {
                invokerClient.Config.LogFunction("[Live Update Service] Failed to close client");
            }
        });
        
        while (!CancellationTokenSource.IsCancellationRequested) {
            string serverMsg;
            try {
                invokerClient.Config.LogFunction("[Live Update Service] Waiting for server message...");
                serverMsg = ReceiveMessage(stream);
            }
            catch (Exception) {
                invokerClient.Config.LogFunction("[Live Update Service] An error occured while receiving a message from the server. Retrying...");
                continue;
            }
            if (CancellationTokenSource.IsCancellationRequested) {
                continue;
            }
            invokerClient.Config.LogFunction.Invoke("[Live Update Server] " + serverMsg);

            string[] args = serverMsg.Split(' ');
            if (args.Length == 0) {
                invokerClient.Config.LogFunction.Invoke("Invalid live update server message");
                continue;
            }

            string response;
            switch (args[0]) {
                
                default:
                    invokerClient.Config.LogFunction.Invoke("Unknown live update server message, responding with ACK. You should update your client.");
                    response = "ACK";
                    break;
                
                case "":
                    invokerClient.Config.LogFunction.Invoke("Live update server sent empty message, abandoning connection");
                    try {
                        client.Close();
                    }
                    catch (Exception e) {
                        invokerClient.Config.LogFunction.Invoke("Failed to close live update server connection: " + e.Message);
                    }
                    return;

                case "USERNAME":
                    response = invokerClient.Config.Username;
                    break;
                
                case "PUBKEY":
                    response = KeySigning.GetPublicKey(invokerClient.Config.PrivateKey);
                    break;
                
                case "SIGN":
                    string text = args[1];
                    response = KeySigning.SignText(invokerClient.Config.PrivateKey, text);
                    break;
                
                case "CHANNEL":
                    response = invokerClient.Channel;
                    break;
                
                case "ACK":
                    response = "ACK";
                    break;
                
                case "DISCONNECT":
                    invokerClient.Config.LogFunction.Invoke("Disconnect requested by live update server, reconnecting");
                    client.Close();
                    return;

                // A new message has been sent to the channel
                case "MSG": {
                    string msgJson = serverMsg[(serverMsg.IndexOf(' ') + 1)..];
                    Message msg = JsonSerializer.Deserialize<Message>(msgJson)!;
                    invokerClient.NewMessage(msg);
                    response = "ACK";
                    break;
                }

                // A new user has joined the channel
                case "ONLINE": {
                    string userJson = serverMsg[(serverMsg.IndexOf(' ') + 1)..];
                    User user = JsonSerializer.Deserialize<User>(userJson)!;
                    invokerClient.UserOnline(user);
                    response = "ACK";
                    break;
                }
                
                // A user has left the channel
                case "OFFLINE": {
                    string userJson = serverMsg[(serverMsg.IndexOf(' ') + 1)..];
                    User user = JsonSerializer.Deserialize<User>(userJson)!;
                    invokerClient.UserOffline(user);
                    response = "ACK";
                    break;
                }
                
            }
            
            invokerClient.Config.LogFunction.Invoke("[Live Update Client] " + response);
            try {
                SendMessage(client.Client, response);
            }
            catch (Exception e) {
                invokerClient.Config.LogFunction.Invoke("Failed to respond to server: " + e.Message);
                invokerClient.Config.LogFunction.Invoke("Reconnecting...");
                try {
                    client.Close();
                }
                catch (Exception) {
                    invokerClient.Config.LogFunction.Invoke("Closing client failed");
                }
                return;
            }
        }
        client.Close();
    }

    private static string ReceiveMessage(Stream stream) {
        // Read until we get a newline
        StringBuilder cmdBuilder = new();
        while (true) {
            if (CancellationTokenSource.IsCancellationRequested) {
                throw new TaskCanceledException();
            }
            int b = stream.ReadByte();
            if (b == -1) {
                break;
            }
            if (b == '\n') {
                break;
            }
            cmdBuilder.Append((char)b);
        }
        // unescape the newline
        return cmdBuilder.ToString().Replace("\\n", "\n");
    }

    private static async void SendMessage(Socket socket, string data) {
        // Escape the newline character
        data = data.Replace("\n", "\\n");
        // Send the data
        await socket.SendAsync(Encoding.UTF8.GetBytes(data + "\n"), SocketFlags.None, CancellationTokenSource.Token);
    }
    
}