using System;
using System.Windows.Forms;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Interface for managing the status bar UI components and updates
    /// </summary>
    public interface IStatusBarManager
    {
        /// <summary>
        /// Initializes the status bar and adds it to the parent form
        /// </summary>
        StatusStrip Initialize(Form parentForm);

        /// <summary>
        /// Updates the current time display
        /// </summary>
        void UpdateTime();

        /// <summary>
        /// Updates the blocking duration display
        /// </summary>
        void UpdateBlockingDuration();

        /// <summary>
        /// Updates the hook status display
        /// </summary>
        void UpdateHookStatus(bool isActive);

        /// <summary>
        /// Updates the resource usage display
        /// </summary>
        void UpdateResourceUsage(string resourceText, float cpuUsage, long memoryUsage);

        /// <summary>
        /// Updates status bar when keyboard blocking state changes
        /// </summary>
        void UpdateBlockingState(bool isBlocked);

        /// <summary>
        /// Gets the current blocking start time
        /// </summary>
        DateTime? GetBlockingStartTime();

        /// <summary>
        /// Gets the current block count for today
        /// </summary>
        int GetTodayBlockCount();
    }
}
