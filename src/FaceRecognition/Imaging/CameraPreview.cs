using OpenCvSharp;

namespace FaceRecognition.Imaging;

internal static class CameraPreview
{
    public static void Run(string modelPath, CancellationToken stop, Action<Bitmap, int> publish)
    {
        using var detector = new FaceDetector(modelPath);
        using var camera = new VideoCapture();
        if (stop.IsCancellationRequested) return;
        if (!camera.Open(0))
            throw new InvalidOperationException("Could not open the default camera. Check Windows camera access and close other apps using it.");

        camera.Set(VideoCaptureProperties.FrameWidth, 640);
        camera.Set(VideoCaptureProperties.FrameHeight, 480);
        using var frame = new Mat();

        while (!stop.IsCancellationRequested)
        {
            bool hasFrame = camera.Read(frame) && !frame.Empty();
            if (stop.IsCancellationRequested) break;
            if (!hasFrame)
                throw new IOException("The camera stopped providing frames. Check the connection and try again.");

            var (image, count) = PhotoPreview.Annotate(frame, detector);
            // The receiver owns the bitmap even if it discards a frame during shutdown.
            publish(image, count);
        }
    }
}
