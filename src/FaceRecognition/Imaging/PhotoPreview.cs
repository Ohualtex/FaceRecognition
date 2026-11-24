using OpenCvSharp;

namespace FaceRecognition.Imaging;

internal static class PhotoPreview
{
    public static (Bitmap Image, int FaceCount) Create(string photoPath, string modelPath)
    {
        byte[] bytes = File.ReadAllBytes(photoPath);
        if (bytes.Length == 0)
            throw new InvalidDataException("The selected image file is empty.");

        using Mat image = Cv2.ImDecode(bytes, ImreadModes.Color);
        if (image.Empty())
            throw new InvalidDataException("OpenCV could not read the selected image. Choose a valid image file.");

        using var detector = new FaceDetector(modelPath);
        Rect[] faces = detector.Detect(image);
        foreach (Rect face in faces)
            Cv2.Rectangle(image, face, new Scalar(90, 238, 108), 3);

        if (!Cv2.ImEncode(".png", image, out byte[] preview))
            throw new InvalidDataException("The annotated preview could not be created.");

        using var stream = new MemoryStream(preview);
        using var bitmap = new Bitmap(stream);
        // Copy the bitmap so the PictureBox can outlive the image stream.
        return (new Bitmap(bitmap), faces.Length);
    }
}
