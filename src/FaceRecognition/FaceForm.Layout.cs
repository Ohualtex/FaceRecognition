namespace FaceRecognition;

internal sealed partial class FaceForm
{
    private static readonly Color Canvas = Color.FromArgb(243, 246, 248);
    private static readonly Color Ink = Color.FromArgb(25, 47, 54);
    private static readonly Color MutedInk = Color.FromArgb(82, 101, 106);
    private static readonly Color Accent = Color.FromArgb(20, 109, 100);
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
                CommandTone.Primary => (Accent, Color.FromArgb(13, 77, 70), Color.FromArgb(17, 91, 83), Color.FromArgb(11, 68, 61)),
                CommandTone.Stop => (ErrorInk, Color.FromArgb(116, 23, 43), Color.FromArgb(137, 29, 52), Color.FromArgb(103, 21, 38)),
                _ => (Color.White, Color.FromArgb(207, 231, 226), Color.FromArgb(228, 239, 237), Color.FromArgb(218, 234, 231))
            };
            bool keyboardFocus = Enabled && Focused && base.ShowFocusCues;
            BackColor = Enabled ? (keyboardFocus ? focused : normal) : Color.FromArgb(230, 236, 238);
            ForeColor = Enabled ? (tone == CommandTone.Secondary ? Ink : Color.White) : MutedInk;
            FlatAppearance.BorderSize = Enabled && tone == CommandTone.Secondary ? 1 : 0;
            FlatAppearance.BorderColor = Color.FromArgb(191, 208, 211);
            FlatAppearance.MouseOverBackColor = hover;
            FlatAppearance.MouseDownBackColor = pressed;
        }
    }
}
