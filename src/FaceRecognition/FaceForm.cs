using FaceRecognition.Imaging;

namespace FaceRecognition;

internal sealed partial class FaceForm : Form
{
    private static readonly string ModelPath = Path.Combine(AppContext.BaseDirectory, "Models", "haarcascade_frontalface_default.xml");
    private readonly CommandButton choosePhoto = new("Choose photo", CommandTone.Primary);
    private readonly PictureBox preview = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(220, 234, 246),
        SizeMode = PictureBoxSizeMode.Zoom,
        AccessibleName = "Image preview"
    };
    private readonly Label status = new()
    {
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "Choose a photo or start the camera to detect faces."
    };

    public FaceForm()
    {
        Text = "FaceRecognition";
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(960, 640);
        MinimumSize = new Size(640, 440);
        BackColor = Canvas;
        ForeColor = Ink;

        Controls.Add(BuildLayout());

        choosePhoto.Click += async (_, _) => await OpenPhotoAsync();
        cameraButton.Click += async (_, _) =>
        {
            if (cameraStop is null) await StartCameraAsync();
            else StopCamera();
        };
        loadSamples.Click += async (_, _) => await LoadSamplesAsync();
    }

    private async Task OpenPhotoAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose a photo",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.webp|All files|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        SetInputControlsEnabled(false);
        SetStatus("Detecting faces...");
        string photoPath = dialog.FileName;
        LocalFaceRecognizer? currentRecognizer = recognizer;
        try
        {
            var (image, count) = await Task.Run(() => PhotoPreview.Create(photoPath, ModelPath, currentRecognizer));
            if (IsDisposed)
            {
                image.Dispose();
                return;
            }

            ReplaceImage(image);
            SetStatus(DescribeFaces(count));
        }
        catch (Exception error)
        {
            if (IsDisposed) return;
            ReplaceImage(null);
            SetStatus("Could not process the photo. " + error.Message, error: true);
        }
        finally
        {
            if (!IsDisposed) SetInputControlsEnabled(true);
        }
    }

    private static string DescribeFaces(int count) => count switch
    {
        0 => "No faces detected.",
        1 => "1 face detected.",
        _ => $"{count} faces detected."
    };

    private void SetStatus(string message, bool error = false)
    {
        status.Text = message;
        status.ForeColor = error ? ErrorInk : Ink;
    }

    private void ReplaceImage(Image? image)
    {
        Image? previous = preview.Image;
        preview.Image = image;
        previous?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            cameraStop?.Cancel();
            recognizer?.Dispose();
            recognizer = null;
            statusTip.Dispose();
            ReplaceImage(null);
        }
        base.Dispose(disposing);
    }
}
