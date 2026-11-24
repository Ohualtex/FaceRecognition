namespace FaceRecognition;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using Stream iconResource = typeof(Program).Assembly.GetManifestResourceStream("FaceRecognition.ico")
            ?? throw new InvalidOperationException("The application icon resource is missing.");
        using Icon icon = new(iconResource);
        using var form = new FaceForm { Icon = icon };
        Application.Run(form);
    }
}
