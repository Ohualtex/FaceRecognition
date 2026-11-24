using FaceRecognition.Imaging;

namespace FaceRecognition;

internal sealed partial class FaceForm
{
    private readonly Button loadSamples = new() { Text = "Load samples", AutoSize = true };
    private LocalFaceRecognizer? recognizer;

    private async Task LoadSamplesAsync()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose a samples folder with one subfolder per face label.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            await TrainSamplesAsync(dialog.SelectedPath);
    }

    private async Task TrainSamplesAsync(string path)
    {
        SetInputControlsEnabled(false);
        SetStatus("Training from local samples...");

        try
        {
            var (trained, samples, labels, skipped) = await Task.Run(() => LocalFaceRecognizer.Train(path, ModelPath));
            if (IsDisposed)
            {
                trained.Dispose();
                return;
            }

            recognizer?.Dispose();
            recognizer = trained;
            ReplaceImage(null);
            string message = $"Loaded {samples} samples across {labels} labels.";
            if (skipped > 0) message += $" Skipped {skipped} invalid samples.";
            SetStatus(message + " Choose a photo to check for matches.");
        }
        catch (Exception error)
        {
            if (!IsDisposed) SetStatus("Could not load samples. " + error.Message, error: true);
        }
        finally
        {
            if (!IsDisposed) SetInputControlsEnabled(true);
        }
    }

    private void SetInputControlsEnabled(bool enabled)
    {
        choosePhoto.Enabled = startCamera.Enabled = loadSamples.Enabled = enabled;
    }
}
