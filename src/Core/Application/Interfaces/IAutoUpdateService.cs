using System;
using System.Threading.Tasks;

namespace SimBlock.Core.Application.Interfaces
{
    public interface IAutoUpdateService
    {
        /// <summary>
        /// Checks for available updates from GitHub releases
        /// </summary>
        /// <returns>Update information if available, null if no update</returns>
        Task<UpdateInfo?> CheckForUpdatesAsync();

        /// <summary>
        /// Downloads and installs the specified update
        /// </summary>
        /// <param name="updateInfo">The update information</param>
        /// <returns>True if update was successful</returns>
        Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo);

        /// <summary>
        /// Gets the current application version
        /// </summary>
        string GetCurrentVersion();

        /// <summary>
        /// Event fired when update progress changes
        /// </summary>
        event EventHandler<UpdateProgressEventArgs>? UpdateProgressChanged;

        /// <summary>
        /// Event fired when an update is available
        /// </summary>
        event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public long FileSize { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    public class UpdateProgressEventArgs : EventArgs
    {
        public int ProgressPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
    }

    public class UpdateAvailableEventArgs : EventArgs
    {
        public UpdateInfo UpdateInfo { get; set; } = null!;
    }
}