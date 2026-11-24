using OpenCvSharp;
using OpenCvSharp.Face;

namespace FaceRecognition.Imaging;

internal sealed class LocalFaceRecognizer(LBPHFaceRecognizer model, string[] labels) : IDisposable
{
    private const double MatchThreshold = 65.0;
    private static readonly OpenCvSharp.Size FaceSize = new(128, 128);
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".webp"
    };
    private readonly object modelLock = new();

    public static (LocalFaceRecognizer Recognizer, int Samples, int Labels, int Skipped) Train(string samplesPath, string modelPath)
    {
        string[] folders = Directory.GetDirectories(samplesPath).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        if (folders.Length < 2)
            throw new InvalidDataException("Choose a folder with at least two subfolders, one per label.");

        using var detector = new FaceDetector(modelPath);
        var faces = new List<Mat>();
        var labelIds = new List<int>();
        int skipped = 0;
        try
        {
            for (int label = 0; label < folders.Length; label++)
            {
                int previousCount = faces.Count;
                foreach (string path in Directory.EnumerateFiles(folders[label]))
                {
                    if (!ImageExtensions.Contains(Path.GetExtension(path))) continue;
                    Mat? face = ReadTrainingFace(path, detector);
                    if (face is null)
                    {
                        skipped++;
                        continue;
                    }

                    faces.Add(face);
                    labelIds.Add(label);
                }

                if (faces.Count == previousCount)
                    throw new InvalidDataException($"'{Path.GetFileName(folders[label])}' needs at least one readable photo with exactly one face.");
            }

            string[] names = folders.Select(folder => Path.GetFileName(folder)!).ToArray();
            var recognizer = LBPHFaceRecognizer.Create();
            try
            {
                recognizer.Train(faces, labelIds);
                return (new LocalFaceRecognizer(recognizer, names), faces.Count, names.Length, skipped);
            }
            catch
            {
                recognizer.Dispose();
                throw;
            }
        }
        finally
        {
            foreach (Mat face in faces) face.Dispose();
        }
    }

    public (string Label, double Distance) Predict(Mat image, Rect bounds)
    {
        using Mat face = NormalizeFace(image, bounds);
        lock (modelLock)
        {
            model.Predict(face, out int label, out double distance);
            return (label >= 0 && distance <= MatchThreshold ? labels[label] : "Unknown", distance);
        }
    }

    public void Dispose()
    {
        lock (modelLock) model.Dispose();
    }

    private static Mat? ReadTrainingFace(string path, FaceDetector detector)
    {
        try
        {
            using Mat image = PhotoPreview.ReadImage(path);
            Rect[] faces = detector.Detect(image);
            return faces.Length == 1 ? NormalizeFace(image, faces[0]) : null;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or OpenCVException)
        {
            return null;
        }
    }

    private static Mat NormalizeFace(Mat image, Rect bounds)
    {
        using var crop = new Mat(image, bounds);
        using var gray = new Mat();
        Cv2.CvtColor(crop, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.Resize(gray, gray, FaceSize, interpolation: InterpolationFlags.Area);
        Cv2.EqualizeHist(gray, gray);
        return gray.Clone();
    }
}
