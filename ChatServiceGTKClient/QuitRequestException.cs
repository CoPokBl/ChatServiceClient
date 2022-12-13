namespace ChatServiceGTKClient; 

public class QuitRequestException : Exception {
    public QuitRequestException(string message) : base(message) { }
}