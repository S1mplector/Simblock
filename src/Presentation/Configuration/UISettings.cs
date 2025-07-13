using System.Drawing;

namespace SimBlock.Presentation.Configuration
{
    /// <summary>
    /// Configuration settings for UI appearance and behavior
    /// </summary>
    public class UISettings
    {
        // Timer intervals
        public int StatusUpdateInterval { get; set; } = 1000; // 1 second

        // Colors
        public Color NormalColor { get; set; } = Color.Blue;
        public Color WarningColor { get; set; } = Color.Orange;
        public Color ErrorColor { get; set; } = Color.Red;
        public Color SuccessColor { get; set; } = Color.Green;
        public Color InactiveColor { get; set; } = Color.Gray;

        // Button colors
        public Color PrimaryButtonColor { get; set; } = Color.FromArgb(0, 120, 215);
        public Color DangerButtonColor { get; set; } = Color.FromArgb(215, 0, 0);
        public Color SecondaryButtonColor { get; set; } = Color.Gray;

        // Window settings
        public Size WindowSize { get; set; } = new Size(550, 300);
        public int WindowPadding { get; set; } = 20;

        // Status bar settings
        public Color StatusBarBackColor { get; set; } = Color.FromArgb(240, 240, 240);
        public int TimeColumnWidth { get; set; } = 60;
        public int DurationColumnWidth { get; set; } = 100;
        public int SessionColumnWidth { get; set; } = 100;
        public int HookStatusColumnWidth { get; set; } = 80;

        // Resource usage thresholds
        public float CpuWarningThreshold { get; set; } = 2.0f;
        public float CpuErrorThreshold { get; set; } = 5.0f;
        public long MemoryWarningThreshold { get; set; } = 50; // MB
        public long MemoryErrorThreshold { get; set; } = 80; // MB

        // Logo settings
        public Size LogoSize { get; set; } = new Size(48, 48);
        public Size IconSize { get; set; } = new Size(32, 32);
    }
}
