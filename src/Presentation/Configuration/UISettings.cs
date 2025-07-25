using System.Drawing;
using System.Windows.Forms;
using SimBlock.Core.Domain.Enums;
using SimBlock.Core.Domain.Entities;

namespace SimBlock.Presentation.Configuration
{
    /// <summary>
    /// Defines available UI themes
    /// </summary>
    public enum Theme
    {
        Light,
        Dark
    }

    /// <summary>
    /// Configuration settings for UI appearance and behavior
    /// </summary>
    public class UISettings
    {
        // Timer intervals
        public int StatusUpdateInterval { get; set; } = 1000; // 1 second

        // Theme settings
        public Theme CurrentTheme { get; set; } = Theme.Light;

        // Emergency unlock shortcut settings (configurable)
        public Keys EmergencyUnlockKey { get; set; } = Keys.U;
        public bool EmergencyUnlockRequiresCtrl { get; set; } = true;
        public bool EmergencyUnlockRequiresAlt { get; set; } = true;
        public bool EmergencyUnlockRequiresShift { get; set; } = false;

        // Startup settings
        public bool StartWithWindows { get; set; } = false;
        
        // Auto-update settings
        public bool AutoUpdateEnabled { get; set; } = true;
        public int AutoUpdateCheckIntervalHours { get; set; } = 4;
        public bool AutoUpdateNotifyOnly { get; set; } = false; // If true, only notify, don't auto-install
        
        // Advanced blocking settings
        public BlockingMode KeyboardBlockingMode { get; set; } = BlockingMode.Simple;
        public BlockingMode MouseBlockingMode { get; set; } = BlockingMode.Simple;
        public AdvancedKeyboardConfiguration AdvancedKeyboardConfig { get; set; } = new();
        public AdvancedMouseConfiguration AdvancedMouseConfig { get; set; } = new();

        // Colors (will be dynamically set based on theme)
        public Color NormalColor { get; set; } = Color.Blue;
        public Color WarningColor { get; set; } = Color.Orange;
        public Color ErrorColor { get; set; } = Color.Red;
        public Color SuccessColor { get; set; } = Color.Green;
        public Color InactiveColor { get; set; } = Color.Gray;
        public Color SelectedColor { get; set; } = Color.Orange;

        // Button colors
        public Color PrimaryButtonColor { get; set; } = Color.FromArgb(0, 120, 215);
        public Color PrimaryButtonTextColor { get; set; } = Color.White;
        public Color DangerButtonColor { get; set; } = Color.FromArgb(215, 0, 0);
        public Color SecondaryButtonColor { get; set; } = Color.Gray;

        // Background and text colors
        public Color BackgroundColor { get; set; } = Color.White;
        public Color TextColor { get; set; } = Color.Black;
        public Color StatusBarBackColor { get; set; } = Color.FromArgb(240, 240, 240);

        // Window settings
        public Size WindowSize { get; set; } = new Size(900, 800);
        public int WindowPadding { get; set; } = 30;

        // Status bar settings
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

        /// <summary>
        /// Applies the specified theme to the UI settings
        /// </summary>
        public void ApplyTheme(Theme theme)
        {
            CurrentTheme = theme;

            switch (theme)
            {
                case Theme.Light:
                    ApplyLightTheme();
                    break;
                case Theme.Dark:
                    ApplyDarkTheme();
                    break;
            }
        }

        private void ApplyLightTheme()
        {
            // Background colors
            BackgroundColor = Color.White;
            StatusBarBackColor = Color.FromArgb(240, 240, 240);
            
            // Text colors
            TextColor = Color.Black;
            InactiveColor = Color.Gray;
            
            // Status colors
            NormalColor = Color.Blue;
            WarningColor = Color.Orange;
            ErrorColor = Color.Red;
            SuccessColor = Color.Green;
            SelectedColor = Color.FromArgb(255, 165, 0); // Orange
            
            // Button colors
            PrimaryButtonColor = Color.FromArgb(0, 120, 215);
            PrimaryButtonTextColor = Color.White;
            DangerButtonColor = Color.FromArgb(215, 0, 0);
            SecondaryButtonColor = Color.Gray;
        }

        private void ApplyDarkTheme()
        {
            // Background colors
            BackgroundColor = Color.FromArgb(32, 32, 32);
            StatusBarBackColor = Color.FromArgb(45, 45, 45);
            
            // Text colors
            TextColor = Color.White;
            InactiveColor = Color.FromArgb(160, 160, 160);
            
            // Status colors (adjusted for dark theme)
            NormalColor = Color.FromArgb(100, 149, 237); // Cornflower blue
            WarningColor = Color.FromArgb(255, 165, 0); // Orange
            ErrorColor = Color.FromArgb(255, 99, 71); // Tomato
            SuccessColor = Color.FromArgb(50, 205, 50); // Lime green
            SelectedColor = Color.FromArgb(255, 140, 0); // Dark orange
            
            // Button colors (adjusted for dark theme)
            PrimaryButtonColor = Color.FromArgb(0, 120, 215);
            PrimaryButtonTextColor = Color.White;
            DangerButtonColor = Color.FromArgb(215, 0, 0);
            SecondaryButtonColor = Color.FromArgb(80, 80, 80);
        }
    }
}
