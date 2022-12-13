using ChatServiceClientLibrary;
using GeneralPurposeLib;
using Gtk;
using Application = Gtk.Application;
using UI = Gtk.Builder.ObjectAttribute;
using WrapMode = Pango.WrapMode;

namespace ChatServiceGTKClient;

internal class MainWindow : Window {

    private const string TitleStart = "Chat Client";
    private const string CheckMark = "<span foreground=\"green\" style=\"italic\" size=\"larger\">✓</span>";
    private const string DefaultMainServer = "chatservice.zaneharrison.com:80";  // This is an alias of Serble's server that is unblocked at all tested schools
    private const string DefaultLiveUpdateServer = "chatservice.zaneharrison.com:9435";  // This is an alias of Serble's server that is unblocked at all tested schools

    // NAMES CANNOT BE CHANGED BECAUSE THEY ARE USED BY GTK TO FIND THE OBJECTS
    // ReSharper disable InconsistentNaming
    [UI] private readonly Entry serverIPEntry = null!;
    [UI] private readonly Entry liveUpdateIPEntry = null!;
    [UI] private readonly Entry channelNameEntry = null!;
    [UI] private readonly Entry usernameEntry = null!;
    [UI] private readonly Button connectButton = null!;
    [UI] private readonly Box messagesBox = null!;
    [UI] private readonly Box onlineUsersBox = null!;
    [UI] private readonly Entry messageEntry = null!;
    [UI] private readonly Button sendButton = null!;
    [UI] private readonly Menu messageContextMenu = null!;
    [UI] private readonly Menu userContextMenu = null!;
    // ReSharper restore InconsistentNaming

    private ChatClient? _client;
    private bool _connected;
    private readonly List<Message> _messages = new();
    private readonly List<User> _onlineUsers = new();
    private readonly Dictionary<string, Widget> _greyMessages = new();
    private string _clickedMessageId = null!;
    private User _clickedUser = null!;

    public MainWindow() : this(new Builder("MainWindow.glade")) { }

    private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow")) {
        builder.Autoconnect(this);

        DeleteEvent += Window_DeleteEvent;
        connectButton.Clicked += ConnectPressed;
        messageEntry.Activated += (_,_) => {SendMessagePressed();};
        sendButton.Clicked += (_,_) => {SendMessagePressed();};
        
        serverIPEntry.Text = UserPrefs.GetString("serverIP", "");
        liveUpdateIPEntry.Text = UserPrefs.GetString("liveUpdateIP", "");
        usernameEntry.Text = UserPrefs.GetString("username", "");
        channelNameEntry.Text = UserPrefs.GetString("channel", "");

        // Message Context Menu:
        ((MenuItem)messageContextMenu.Children[0]).Activated += (_, _) => { // Copy Message
            Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", true)).Text
                = GetMessageFromId(_clickedMessageId).Text;
        };
        
        ((MenuItem)messageContextMenu.Children[1]).Activated += (_, _) => { // Add Trusted
            Message msg = GetMessageFromId(_clickedMessageId);
            foreach (User usr in _onlineUsers.Where(usr => msg.VerifySignature(usr.PublicKey))) {
                _client!.TrustedUsers.TrustUser(usr);
            }
            Console.WriteLine("A user was trusted via msg context menu");
        };
        
        // User context menu
        ((MenuItem)userContextMenu.Children[0]).Activated += (_, _) => { // Copy Username
            Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", true)).Text
                = _clickedUser.Username;
        };
        
        ((MenuItem)userContextMenu.Children[1]).Activated += (_, _) => { // Add Trusted
            _client!.TrustedUsers.TrustUser(_clickedUser);
            Console.WriteLine("A user was trusted via user context menu");
        };

        Application.Invoke(delegate {
            Window.Title = TitleStart;
            Window.Resize(800, 450);
        });
    }

    private void Window_DeleteEvent(object sender, DeleteEventArgs a) {
        if (_client != null) {  // If the client is connected then save the settings
            _client.TrustedUsers.SaveData(_client);
            UserPrefs.SetString("privateKey", _client.PrivateKey);
            UserPrefs.Save();
        }
        Console.WriteLine("BYE!!! (Window)");
        Application.Quit();
        throw new QuitRequestException("Please quit");  // This will raise to Program.cs because Application.Quit() doesn't seem to work
    }

    private Message GetMessageFromId(string id) => _messages.Find(m => m.MessageId == id)!;

    private void SendMessagePressed() {
        if (!_connected) return;
        Console.WriteLine("Sending message...");

        async void Start() {
            string message = Commands.Emoticons(messageEntry.Text);
            Message msgObj = await _client!.SendMessage(message);
            string id = msgObj.MessageId;
            Console.WriteLine("Message sent with id: " + id);
            messageEntry.Text = "";

            Message lastMessage = (_messages.Count > 0 ? _messages[^1] : null)!;
            Application.Invoke(delegate {
                CreateMessageLabel(lastMessage, _client.Username, message, DateTime.FromBinary(msgObj.CreatedAt), id, true, true);

                new Thread(() => {
                    Thread.Sleep(100);
                    Application.Invoke(delegate { ScrollToBottom(); });
                }).Start();
            });
        }

        new Thread(Start).Start();
    }
    
    private void ConnectPressed(object? sender, EventArgs e) {
        connectButton.Label = "Connecting...";

        _greyMessages.Clear();
        _messages.Clear();
        
        ClearBox(messagesBox);
        ClearBox(onlineUsersBox);
        
        Init();
    }
    
    private static void ClearBox(Container box) {
        foreach (Widget child in box.Children) box.Remove(child);
    }

    private void ScrollToBottom() {
        Adjustment adjustment = ((ScrolledWindow)messagesBox.Parent.Parent).Vadjustment;
        adjustment.Value = adjustment.Upper;
    }

    private void CreateOnlineUserLabel(User user) {
        bool trusted = _client!.TrustedUsers.IsTrusted(user);
        
        EventBox eventBox = new();
        eventBox.ButtonPressEvent += (_, args) => {
            // only allow right clicks
            if (args.Event.Button != 3) return;
            _clickedUser = user;
            userContextMenu.Children[1].Sensitive = !_client.TrustedUsers.IsTrusted(user);
            userContextMenu.Popup();
        };
        Label label = new("");
        label.Markup = user.Username + (trusted ? CheckMark : "");
        label.Justify = Justification.Left;
        label.Xpad = 10;
        label.Ypad = 10;
        label.Xalign = 0;
        label.LineWrap = true;
        label.LineWrapMode = WrapMode.WordChar;
        eventBox.Add(label);
        onlineUsersBox.Add(eventBox);
        label.Show();
        eventBox.Show();
    }
    
    private void DeleteOnlineUserLabel(User user) {
        foreach (Widget child in onlineUsersBox.Children) {
            if (child is not EventBox eventBox || eventBox.Children[0] is not Label label) continue;
            if (label.Text.Replace(CheckMark, "") != user.Username) continue;
            onlineUsersBox.Remove(eventBox);
            child.Destroy();
            break;
        }
    }

    private void CreateMessageLabel(Message? previousMessage, string creator, string message, DateTime time, string id, bool trusted, bool grey = false) {

        if (_messages.Exists(m => m.MessageId == id) && grey) {
            Console.WriteLine("Message already exists! Skipping creation");
            return;
        }
        
        // Combine Messages
        // if creator name is the same, and was sent in the same minute, combine messages
        bool combine = previousMessage != null && previousMessage.CreatorName == creator &&
                       time - DateTime.FromBinary(previousMessage.CreatedAt).ToLocalTime() < TimeSpan.FromMinutes(1);

        EventBox box = new();
        box.ButtonPressEvent += (_, args) => {
            // only allow right clicks
            if (args.Event.Button != 3) return;
            _clickedMessageId = id;
            messageContextMenu.Children[1].Sensitive = !_client!.TrustedUsers.IsMessageFromTrustedUser(GetMessageFromId(id));
            messageContextMenu.Popup();
        };

        Label label = new("");
        label.Justify = Justification.Left;
        label.Xpad = 10;
        label.UseMarkup = true;
        label.Xalign = 0;
        label.LineWrap = true;
        label.LineWrapMode = WrapMode.WordChar;

        if (combine) {
            label.Markup = message;
        }
        else {
            label.Markup = trusted ? 
                $"<b>{creator}</b> {CheckMark} - <small>{time}</small>\n{message}" : 
                $"<b>{creator}</b> - <small>{time}</small>\n{message}";

            label.MarginTop = 10;
        }
        
        if (grey) {
            label.Opacity = 0.7;
            _greyMessages.Add(id, label);
        }

        box.Add(label);
        messagesBox.Add(box);
        
        box.Show();
        label.Show();
    }

    private async void Init() {
        string ip = serverIPEntry.Text;
        if (string.IsNullOrEmpty(ip)) {
            ip = DefaultMainServer;
        }
            
        string liveUpdateIp = liveUpdateIPEntry.Text;
        if (string.IsNullOrEmpty(liveUpdateIp)) {
            liveUpdateIp = DefaultLiveUpdateServer;
        }

        UserPrefs.SetString("username", usernameEntry.Text);
        UserPrefs.SetString("channel", channelNameEntry.Text);
        UserPrefs.SetString("serverIP", ip);
        UserPrefs.SetString("liveUpdateIP", liveUpdateIp);
        UserPrefs.Save();
        
        string? privateKey = UserPrefs.GetString("privateKey");

        ChatClientConfiguration config = new() {
            Username = usernameEntry.Text,
            ServerUrl = ip,
            LiveUpdateUrl = liveUpdateIp,
            EnableLiveUpdates = true,
            LogFunction = msg => {
                Console.WriteLine("[Chat Client] " + msg);
                return Task.CompletedTask;
            },
            PrivateKey = privateKey ?? KeySigning.GenerateKeyPair()
        };
        _client = new ChatClient(config, channelNameEntry.Text);

        bool connectTest = true;
        try {
            await _client.Connect();
        }
        catch (Exception e) {
            Console.WriteLine("Error connecting: " + e);
            connectTest = false;
        }

        if (!connectTest) {
            connectButton.Label = "Connection Failed.";
            return;
        }

        connectButton.Label = "Reconnect";
        messageEntry.Sensitive = true;
        sendButton.Sensitive = true;
            
        _connected = true;
        Console.WriteLine("Connected!");

        Window.Title = TitleStart + " - " + _client.Channel;
        
        // Get existing messages
        Message[] existingMessages = await _client.GetMessages();
        Message? prevMsg = null;
        foreach (Message message in existingMessages) {
            CreateMessageLabel(
                prevMsg, 
                message.CreatorName, 
                message.Text, 
                DateTime.FromBinary(message.CreatedAt).ToLocalTime(), 
                message.MessageId, 
                _client.TrustedUsers.IsMessageFromTrustedUser(message) || message.WasSentByClient(_client));
            _messages.Add(message);
            prevMsg = message;
        }
        
        // Get existing users
        User[] existingUsers = await _client.GetOnlineUsers();
        foreach (User user in existingUsers) {
            if (user.IsClient(_client)) {
                continue;
            }
            CreateOnlineUserLabel(user);
        }

        _client.OnMessageReceived += message => {
            Message lastMessage = (_messages.Count > 0 ? _messages[^1] : null)!;
            bool isTrusted = _client.TrustedUsers.IsMessageFromTrustedUser(message) || message.WasSentByClient(_client);
            Console.WriteLine(
                $"Message Received From {message.CreatorName} with ID {message.MessageId} (Trusted: {isTrusted}, IsMe: {message.WasSentByClient(_client)}): " + 
                message.Text);
            _messages.Add(message);
            
            if (message.CreatorName == _client.Username) {
                // try to find the message in greyMessages and make that have full opacity
                if (_greyMessages.TryGetValue(message.MessageId, out Widget? label)) {
                    Console.WriteLine("Found grey message with id: " + message.MessageId);
                    label.Opacity = 1d;
                    Label label2 = (Label)label;

                    if (label.MarginTop == 10)
                        label2.LabelMarkup = label2.LabelMarkup.Insert(8 + _client.Username.Length, 
                            CheckMark + " ");

                    _greyMessages.Remove(message.MessageId);
                    return;
                }
            }
            
            CreateMessageLabel(lastMessage, message.CreatorName, message.Text, 
                DateTime.FromBinary(message.CreatedAt).ToLocalTime(), message.MessageId, isTrusted);
        };
        
        _client.OnUserOnline += user => {
            Console.WriteLine($"User Online: {user.Username}");
            _onlineUsers.Add(user);
            if (user.Username == _client.Username && user.PublicKey == _client.PublicKey) {
                Console.WriteLine("I went online!");
                return;
            }
            CreateOnlineUserLabel(user);
        };
        
        _client.OnUserOffline += user => {
            Console.WriteLine($"User Offline: {user.Username}");
            _onlineUsers.Remove(user);
            if (user.Username == _client.Username && user.PublicKey == _client.PublicKey) return;
            DeleteOnlineUserLabel(user);
        };
        
    }
    
}
