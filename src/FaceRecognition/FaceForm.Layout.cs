namespace FaceRecognition;

internal sealed partial class FaceForm
{
    private static readonly Color Canvas = Color.FromArgb(239, 247, 253);
    private static readonly Color Ink = Color.FromArgb(23, 42, 62);
    private static readonly Color MutedInk = Color.FromArgb(74, 99, 124);
    private static readonly Color Accent = Color.FromArgb(217, 255, 254);
    private static readonly Color ErrorInk = Color.FromArgb(155, 36, 61);
    private readonly ToolTip statusTip = new();
    private readonly Label modelStatus = new() { Text = "Detection only", AutoSize = true };
    private readonly Label modelDetails = new() { Text = "No samples loaded", AutoSize = true };

    private Control BuildLayout()
    {
        var layout = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var toolbar = new Panel { Dock = DockStyle.Top };
        Control commands = CommandGroup(choosePhoto, cameraButton);
        toolbar.Controls.Add(commands);

        var modelState = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 10, 0),
            Anchor = AnchorStyles.Left
        };
        modelStatus.Font = new Font(Font, FontStyle.Bold);
        modelStatus.Margin = Padding.Empty;
        modelDetails.ForeColor = MutedInk;
        modelDetails.Margin = Padding.Empty;
        modelState.Controls.AddRange([modelStatus, modelDetails]);

        var samples = new TableLayoutPanel
        {
            ColumnCount = 2,
            Margin = Padding.Empty
        };
        samples.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        samples.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        loadSamples.Anchor = AnchorStyles.Right;
        loadSamples.Margin = Padding.Empty;
        samples.Controls.Add(modelState, 0, 0);
        samples.Controls.Add(loadSamples, 1, 0);
        toolbar.Controls.Add(samples);

        void FitToolbar()
        {
            int width = layout.ClientSize.Width - layout.Padding.Horizontal;
            Size commandSize = commands.GetPreferredSize(Size.Empty);
            Size sampleSize = samples.GetPreferredSize(Size.Empty);
            int groupGap = LogicalToDeviceUnits(12);
            bool compact = width < commandSize.Width + sampleSize.Width + groupGap;
            int sampleTop = compact ? commandSize.Height + LogicalToDeviceUnits(8) : 0;

            commands.Bounds = new Rectangle(0, 0, commandSize.Width, commandSize.Height);
            samples.Bounds = new Rectangle(Math.Max(0, width - sampleSize.Width), sampleTop,
                sampleSize.Width, sampleSize.Height);

            // Docking reserves the full toolbar height before sizing the preview.
            toolbar.Height = Math.Max(commandSize.Height, samples.Bottom) + groupGap;
        }

        status.Dock = DockStyle.Bottom;
        status.Height = 48;
        status.Padding = new Padding(0, 8, 0, 0);
        status.UseMnemonic = false;
        statusTip.SetToolTip(status, status.Text);
        layout.Controls.AddRange([preview, status, toolbar]);

        layout.ClientSizeChanged += (_, _) => FitToolbar();
        cameraButton.TextChanged += (_, _) => FitToolbar();
        modelStatus.TextChanged += (_, _) => FitToolbar();
        modelDetails.TextChanged += (_, _) => FitToolbar();
        FitToolbar();
        return layout;
    }

    private static Control CommandGroup(params Control[] buttons)
    {
        var group = new FlowLayoutPanel
        {
            WrapContents = false,
            Margin = Padding.Empty
        };
        group.Controls.AddRange(buttons);
        return group;
    }

    private enum CommandTone { Secondary, Primary, Stop }

    private sealed class CommandButton : Button
    {
        private CommandTone tone;

        public CommandButton(string text, CommandTone tone = CommandTone.Secondary)
        {
            Text = text;
            AutoSize = true;
            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
            Padding = new Padding(11, 5, 11, 5);
            Margin = new Padding(0, 0, 8, 0);
            MinimumSize = new Size(0, 38);
            this.tone = tone;
            EnabledChanged += (_, _) => ApplyAppearance();
            GotFocus += (_, _) => ApplyAppearance();
            LostFocus += (_, _) => ApplyAppearance();
            ChangeUICues += (_, _) => ApplyAppearance();
            ApplyAppearance();
        }

        public void SetTone(CommandTone value)
        {
            if (tone == value) return;
            tone = value;
            ApplyAppearance();
        }

        protected override bool ShowFocusCues => false;

        private void ApplyAppearance()
        {
            var (normal, focused, hover, pressed) = tone switch
            {
                CommandTone.Primary => (Accent, Color.FromArgb(163, 222, 237), Color.FromArgb(193, 240, 247), Color.FromArgb(177, 229, 241)),
                CommandTone.Stop => (ErrorInk, Color.FromArgb(116, 23, 43), Color.FromArgb(137, 29, 52), Color.FromArgb(103, 21, 38)),
                _ => (Color.White, Color.FromArgb(203, 230, 247), Color.FromArgb(229, 243, 253), Color.FromArgb(212, 232, 248))
            };
            bool keyboardFocus = Enabled && Focused && base.ShowFocusCues;
            BackColor = Enabled ? (keyboardFocus ? focused : normal) : Color.FromArgb(225, 235, 244);
            ForeColor = Enabled ? (tone == CommandTone.Stop ? Color.White : Ink) : MutedInk;
            FlatAppearance.BorderSize = Enabled && tone != CommandTone.Stop ? 1 : 0;
            FlatAppearance.BorderColor = Color.FromArgb(164, 195, 216);
            FlatAppearance.MouseOverBackColor = hover;
            FlatAppearance.MouseDownBackColor = pressed;
        }
    }
}
