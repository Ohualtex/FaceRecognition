using OpenCvSharp;

namespace FaceRecognition.Imaging;

internal sealed class FaceDetector : IDisposable
{
    private readonly CascadeClassifier classifier;

    public FaceDetector(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("The face detector model is missing. Restore the Models folder and try again.");

        classifier = new CascadeClassifier(modelPath);
        if (!classifier.Empty()) return;

        classifier.Dispose();
        throw new InvalidDataException("The face detector model could not be loaded.");
    }

    public Rect[] Detect(Mat image)
    {
        using var gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.EqualizeHist(gray, gray);
        return classifier.DetectMultiScale(gray, scaleFactor: 1.1, minNeighbors: 9, minSize: new OpenCvSharp.Size(28, 28));
    }

    public void Dispose() => classifier.Dispose();
}
