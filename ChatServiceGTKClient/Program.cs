using System.Reflection;
using GLib;
using Application = Gtk.Application;

namespace ChatServiceGTKClient; 

internal static class Program {
    
    [STAThread]
    public static void Main() {
        Application.Init();

        Application app = new("net.Serble.ChatAppClient", ApplicationFlags.None);
        app.Register(Cancellable.Current);
        ExceptionManager.UnhandledException += a => {
            if (a.ExceptionObject is TargetInvocationException {InnerException: QuitRequestException}) {
                a.ExitApplication = true;
                Console.WriteLine("Cya! (Error Event Handler)");
            }
            else {
                Console.WriteLine("Woops, we did an oopsie! (Event Handler)");
                Console.WriteLine(a.ExceptionObject);
            }
        };

        MainWindow win = new();
        app.AddWindow(win);

        win.Show();

        Application.Run();
    }
    
}