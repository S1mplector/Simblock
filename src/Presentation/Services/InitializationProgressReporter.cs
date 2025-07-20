using System;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Presentation.Services
{
    /// <summary>
    /// Implementation of IInitializationProgress for reporting initialization progress
    /// </summary>
    public class InitializationProgressReporter : IInitializationProgress
    {
        public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;

        /// <summary>
        /// Reports progress with percentage and status message
        /// </summary>
        /// <param name="percentage">Progress percentage (0-100)</param>
        /// <param name="status">Status message describing current operation</param>
        public void ReportProgress(int percentage, string status)
        {
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(percentage, status));
        }
    }
}