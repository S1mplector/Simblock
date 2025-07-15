using System;
using System.Windows.Forms;

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
            public Button ToggleButton { get; set; } = null!;
            public Label StatusLabel { get; set; } = null!;
            public PictureBox LogoIcon { get; set; } = null!;
            public Label KeyboardNameLabel { get; set; } = null!;
            public Label LastToggleLabel { get; set; } = null!;
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
        void UpdateUI(UIControls controls, bool isKeyboardBlocked, string statusText, string toggleButtonText, DateTime lastToggleTime);

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
        void UpdateKeyboardNameLabel(Label keyboardNameLabel, string keyboardName);
    }
}
