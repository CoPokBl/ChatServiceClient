
using ChatServiceClientLibrary;

ChatClientConfiguration config = new("CoPokBl", "chatservice.serble.net:80", "chatservice.serble.net:9435") {
    LogFunction = s => {
        Console.WriteLine("[LOG] " + s);
        return Task.CompletedTask;
    }
};
ChatClient client = new(config, "a");
await client.Connect();
Message[] messages = await client.GetMessages(10, 0);
foreach (Message msg in messages) {
    Console.WriteLine($"{msg.CreatorName}: {msg.Text}");
}
client.OnMessageReceived += msg => {
    Console.WriteLine($"{msg.CreatorName}: {msg.Text}");
};
await client.SendMessage("Hello World!");
int x = 0;
while (true) {
    await client.SendMessage("X=" + x++);
    await Task.Delay(2000);
}
Thread.Sleep(-1);