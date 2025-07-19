using System;
using System.Threading.Tasks;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Interface for managing the application splash screen
    /// </summary>
    public interface ISplashScreenManager
    {
        /// <summary>
        /// Shows the splash screen
        /// </summary>
        /// <returns>Task representing the show operation</returns>
        Task ShowAsync();

        /// <summary>
        /// Updates the splash screen progress
        /// </summary>
        /// <param name="percentage">Progress percentage (0-100)</param>
        /// <param name="status">Status message describing current operation</param>
        void UpdateProgress(int percentage, string status);

        /// <summary>
        /// Closes the splash screen
        /// </summary>
        /// <returns>Task representing the close operation</returns>
        Task CloseAsync();

        /// <summary>
        /// Gets whether the splash screen is currently visible
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Event raised when the splash screen is closed
        /// </summary>
        event EventHandler? SplashScreenClosed;
    }
}