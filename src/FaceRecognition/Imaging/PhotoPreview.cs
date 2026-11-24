using System.Drawing.Drawing2D;
using OpenCvSharp;

namespace FaceRecognition.Imaging;

internal static class PhotoPreview
{
    public static (Bitmap Image, int FaceCount) Create(string photoPath, string modelPath, LocalFaceRecognizer? recognizer = null)
    {
        using Mat image = ReadImage(photoPath);
        using var detector = new FaceDetector(modelPath);
        return Annotate(image, detector, recognizer);
    }

    public static Mat ReadImage(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length == 0)
            throw new InvalidDataException("The selected image file is empty.");

        Mat image = Cv2.ImDecode(bytes, ImreadModes.Color);
        if (!image.Empty()) return image;

        image.Dispose();
        throw new InvalidDataException("OpenCV could not read the selected image. Choose a valid image file.");
    }

    public static (Bitmap Image, int FaceCount) Annotate(Mat image, FaceDetector detector, LocalFaceRecognizer? recognizer = null)
    {
        Rect[] faces = detector.Detect(image);
        // Predict before annotating so the rectangles do not affect matching.
        var predictions = recognizer is null
            ? null
            : faces.Select(face => recognizer.Predict(image, face)).ToArray();

        foreach (Rect face in faces)
            Cv2.Rectangle(image, face, new Scalar(90, 238, 108), 3);

        if (!Cv2.ImEncode(".bmp", image, out byte[] preview))
            throw new InvalidDataException("The annotated preview could not be created.");

        using var stream = new MemoryStream(preview);
        using var bitmap = new Bitmap(stream);
        var display = new Bitmap(bitmap);
        try
        {
            if (predictions is not null && predictions.Length > 0)
                DrawLabels(display, faces, predictions);
            return (display, faces.Length);
        }
        catch
        {
            display.Dispose();
            throw;
        }
    }

    private static void DrawLabels(Bitmap bitmap, Rect[] faces, (string Label, double Distance)[] predictions)
    {
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        using var font = new Font("Segoe UI", Math.Clamp(bitmap.Width / 55f, 17f, 64f), FontStyle.Bold, GraphicsUnit.Pixel);
        using var fill = new SolidBrush(Color.FromArgb(225, 18, 42, 44));
        using var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        float padding = font.Size / 3f;
        for (int index = 0; index < faces.Length; index++)
        {
            Rect face = faces[index];
            var (label, distance) = predictions[index];
            string text = $"{label} · {distance:F1}";
            float width = Math.Min(bitmap.Width, graphics.MeasureString(text, font).Width + 2 * padding);
            float height = Math.Min(bitmap.Height, font.GetHeight(graphics) + padding);
            float left = Math.Clamp(face.X, 0, bitmap.Width - width);
            float top = Math.Clamp(face.Y >= height ? face.Y - height : face.Y + face.Height, 0, bitmap.Height - height);
            graphics.FillRectangle(fill, left, top, width, height);
            graphics.DrawString(text, font, Brushes.White,
                new RectangleF(left + padding, top, Math.Max(1, width - 2 * padding), height), format);
        }
    }
}
