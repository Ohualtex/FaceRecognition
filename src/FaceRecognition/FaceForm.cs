using FaceRecognition.Imaging;

namespace FaceRecognition;

internal sealed class FaceForm : Form
{
    private static readonly Color Canvas = Color.FromArgb(243, 246, 248);
    private static readonly Color Ink = Color.FromArgb(25, 47, 54);
    private static readonly Color Accent = Color.FromArgb(20, 109, 100);
    private static readonly Color ErrorInk = Color.FromArgb(155, 36, 61);
    private readonly Button choosePhoto = new() { Text = "Choose photo", AutoSize = true };
    private readonly PictureBox preview = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(225, 232, 235),
        SizeMode = PictureBoxSizeMode.Zoom,
        AccessibleName = "Image preview"
    };
    private readonly Label status = new()
    {
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "Choose a photo to detect faces."
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

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(22),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 12)
        };
        choosePhoto.FlatStyle = FlatStyle.Flat;
        choosePhoto.FlatAppearance.BorderSize = 0;
        choosePhoto.UseVisualStyleBackColor = false;
        choosePhoto.BackColor = Accent;
        choosePhoto.ForeColor = Color.White;
        choosePhoto.Padding = new Padding(12, 5, 12, 5);
        var title = new Label { Text = "Face detection", AutoSize = true, Margin = new Padding(14, 11, 0, 0) };
        toolbar.Controls.AddRange([choosePhoto, title]);
        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(preview, 0, 1);
        layout.Controls.Add(status, 0, 2);
        Controls.Add(layout);

        choosePhoto.Click += async (_, _) => await OpenPhotoAsync();
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

        choosePhoto.Enabled = false;
        SetStatus("Detecting faces...");
        string photoPath = dialog.FileName;
        string modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "haarcascade_frontalface_default.xml");
        try
        {
            var (image, count) = await Task.Run(() => PhotoPreview.Create(photoPath, modelPath));
            if (IsDisposed)
            {
                image.Dispose();
                return;
            }

            ReplaceImage(image);
            SetStatus(count switch
            {
                0 => "No faces detected.",
                1 => "1 face detected.",
                _ => $"{count} faces detected."
            });
        }
        catch (Exception error)
        {
            if (IsDisposed) return;
            ReplaceImage(null);
            SetStatus("Could not process the photo. " + error.Message, error: true);
        }
        finally
        {
            if (!IsDisposed) choosePhoto.Enabled = true;
        }
    }

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
        if (disposing) ReplaceImage(null);
        base.Dispose(disposing);
    }
}
