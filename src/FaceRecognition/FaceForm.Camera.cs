namespace FaceRecognition;

internal sealed partial class FaceForm
{
    private readonly Button startCamera = new() { Text = "Start camera", AutoSize = true };
    private readonly Button stopCamera = new() { Text = "Stop camera", AutoSize = true, Enabled = false };
    private CancellationTokenSource? cameraStop;
    private bool closeAfterCamera;

    private async Task StartCameraAsync()
    {
        if (cameraStop is not null || !startCamera.Enabled) return;

        using var stop = new CancellationTokenSource();
        cameraStop = stop;
        SetInputControlsEnabled(false);
        stopCamera.Enabled = true;
        ReplaceImage(null);
        SetStatus("Starting camera...");

        try
        {
            await Task.Run(() => Imaging.CameraPreview.Run(ModelPath, stop.Token,
                (image, count) => PublishCameraFrame(image, count, stop.Token)));
            if (!IsDisposed) SetStatus("Camera stopped.");
        }
        catch (Exception error)
        {
            if (!IsDisposed)
            {
                if (stop.IsCancellationRequested)
                    SetStatus("Camera stopped.");
                else
                    SetStatus(error.Message, error: true);
            }
        }
        finally
        {
            cameraStop = null;
            if (!IsDisposed)
            {
                ReplaceImage(null);
                SetInputControlsEnabled(true);
                stopCamera.Enabled = false;
                if (closeAfterCamera) Close();
            }
        }
    }

    private void StopCamera()
    {
        cameraStop?.Cancel();
        stopCamera.Enabled = false;
        SetStatus("Stopping camera...");
    }

    private void PublishCameraFrame(Bitmap image, int count, CancellationToken stop)
    {
        // Synchronous dispatch keeps at most one frame waiting for the UI.
        Bitmap? pending = image;
        try
        {
            if (stop.IsCancellationRequested) return;
            Invoke(() =>
            {
                if (stop.IsCancellationRequested || IsDisposed) return;
                ReplaceImage(image);
                pending = null;
                SetStatus("Camera live · " + DescribeFaces(count));
            });
        }
        finally
        {
            pending?.Dispose();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel || cameraStop is null) return;

        e.Cancel = true;
        closeAfterCamera = true;
        StopCamera();
    }
}
