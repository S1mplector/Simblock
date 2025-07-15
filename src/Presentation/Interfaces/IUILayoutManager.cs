using System;
using System.Windows.Forms;
using SimBlock.Presentation.ViewModels;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Interface for managing the main UI layout and control creation
    /// </summary>
    public interface IUILayoutManager
    {
        /// <summary>
        /// Contains all the UI controls for the main form
        /// </summary>
        public class UIControls
        {
            // Tab controls
            public TabControl MainTabControl { get; set; } = null!;
            public TabPage KeyboardTab { get; set; } = null!;
            public TabPage MouseTab { get; set; } = null!;
            
            // Keyboard tab controls
            public Button KeyboardToggleButton { get; set; } = null!;
            public Label KeyboardStatusLabel { get; set; } = null!;
            public PictureBox KeyboardLogoIcon { get; set; } = null!;
            public Label KeyboardNameLabel { get; set; } = null!;
            public Label KeyboardLastToggleLabel { get; set; } = null!;
            
            // Mouse tab controls
            public Button MouseToggleButton { get; set; } = null!;
            public Label MouseStatusLabel { get; set; } = null!;
            public PictureBox MouseLogoIcon { get; set; } = null!;
            public Label MouseNameLabel { get; set; } = null!;
            public Label MouseLastToggleLabel { get; set; } = null!;
            
            // Shared controls
            public Button HideToTrayButton { get; set; } = null!;
            public Button SettingsButton { get; set; } = null!;
            public Label InstructionsLabel { get; set; } = null!;
            public Label PrivacyNoticeLabel { get; set; } = null!;
        }

        /// <summary>
        /// Initializes the form layout and creates all UI controls
        /// </summary>
        UIControls InitializeLayout(Form form);

        /// <summary>
        /// Updates the UI controls based on the current state
        /// </summary>
        void UpdateUI(UIControls controls, MainWindowViewModel viewModel);

        /// <summary>
        /// Updates the toggle button state during processing
        /// </summary>
        void SetToggleButtonProcessing(Button toggleButton, bool isProcessing);

        /// <summary>
        /// Updates the settings button
        /// </summary>
        void UpdateSettingsButton(Button settingsButton);

        /// <summary>
        /// Updates the keyboard name label
        /// </summary>
        void UpdateDeviceNameLabel(Label deviceNameLabel, string deviceName);
    }
}
