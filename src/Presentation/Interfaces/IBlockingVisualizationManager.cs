using System;
using System.Windows.Forms;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Interface for managing blocking visualization displays
    /// </summary>
    public interface IBlockingVisualizationManager
    {
        /// <summary>
        /// Updates the keyboard visualization with current blocking state
        /// </summary>
        /// <param name="state">Current keyboard blocking state</param>
        void UpdateKeyboardVisualization(KeyboardBlockState state);

        /// <summary>
        /// Updates the mouse visualization with current blocking state
        /// </summary>
        /// <param name="state">Current mouse blocking state</param>
        void UpdateMouseVisualization(MouseBlockState state);

        /// <summary>
        /// Sets the blocking mode for keyboard visualization
        /// </summary>
        /// <param name="mode">Blocking mode (Simple or Advanced)</param>
        /// <param name="config">Advanced configuration if applicable</param>
        void SetKeyboardBlockingMode(BlockingMode mode, AdvancedKeyboardConfiguration? config = null);

        /// <summary>
        /// Sets the blocking mode for mouse visualization
        /// </summary>
        /// <param name="mode">Blocking mode (Simple or Advanced)</param>
        /// <param name="config">Advanced configuration if applicable</param>
        void SetMouseBlockingMode(BlockingMode mode, AdvancedMouseConfiguration? config = null);

        /// <summary>
        /// Gets the keyboard visualization control
        /// </summary>
        /// <returns>Keyboard visualization control</returns>
        Control GetKeyboardVisualizationControl();

        /// <summary>
        /// Gets the mouse visualization control
        /// </summary>
        /// <returns>Mouse visualization control</returns>
        Control GetMouseVisualizationControl();

        /// <summary>
        /// Creates a panel containing both keyboard and mouse visualization controls
        /// </summary>
        /// <returns>Panel with visualization controls</returns>
        Panel CreateVisualizationPanel();

        /// <summary>
        /// Event fired when visualization needs to be updated
        /// </summary>
        event EventHandler<VisualizationUpdateEventArgs>? VisualizationUpdateRequested;
    }

    /// <summary>
    /// Event arguments for visualization updates
    /// </summary>
    public class VisualizationUpdateEventArgs : EventArgs
    {
        public string DeviceType { get; set; } = string.Empty;
        public string UpdateType { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}