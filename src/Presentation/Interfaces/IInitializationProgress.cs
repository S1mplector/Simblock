using System;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Interface for reporting initialization progress
    /// </summary>
    public interface IInitializationProgress
    {
        /// <summary>
        /// Event raised when initialization progress changes
        /// </summary>
        event EventHandler<ProgressChangedEventArgs>? ProgressChanged;

        /// <summary>
        /// Reports progress with percentage and status message
        /// </summary>
        /// <param name="percentage">Progress percentage (0-100)</param>
        /// <param name="status">Status message describing current operation</param>
        void ReportProgress(int percentage, string status);
    }

    /// <summary>
    /// Event arguments for progress change notifications
    /// </summary>
    public class ProgressChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int Percentage { get; }

        /// <summary>
        /// Status message describing current operation
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// Initializes a new instance of ProgressChangedEventArgs
        /// </summary>
        /// <param name="percentage">Progress percentage (0-100)</param>
        /// <param name="status">Status message</param>
        public ProgressChangedEventArgs(int percentage, string status)
        {
            Percentage = Math.Max(0, Math.Min(100, percentage));
            Status = status ?? string.Empty;
        }
    }
}