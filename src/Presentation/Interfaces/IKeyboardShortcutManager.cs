using System;
using System.Windows.Forms;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Interface for managing keyboard shortcuts and their handlers
    /// </summary>
    public interface IKeyboardShortcutManager
    {
        /// <summary>
        /// Fired when toggle is requested via keyboard shortcut
        /// </summary>
        event EventHandler? ToggleRequested;

        /// <summary>
        /// Fired when hide to tray is requested via keyboard shortcut
        /// </summary>
        event EventHandler? HideToTrayRequested;

        /// <summary>
        /// Fired when help is requested via keyboard shortcut
        /// </summary>
        event EventHandler? HelpRequested;

        /// <summary>
        /// Fired when theme toggle is requested via keyboard shortcut
        /// </summary>
        event EventHandler? ThemeToggleRequested;

        /// <summary>
        /// Handles key down events and processes shortcuts
        /// </summary>
        void HandleKeyDown(KeyEventArgs e);

        /// <summary>
        /// Shows the help dialog
        /// </summary>
        void ShowHelp();
    }
}
