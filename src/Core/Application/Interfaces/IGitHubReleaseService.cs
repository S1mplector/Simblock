using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimBlock.Core.Application.Interfaces
{
    public interface IGitHubReleaseService
    {
        /// <summary>
        /// Gets the latest release from GitHub
        /// </summary>
        /// <returns>Latest release information</returns>
        Task<GitHubRelease?> GetLatestReleaseAsync();

        /// <summary>
        /// Gets all releases from GitHub
        /// </summary>
        /// <returns>List of all releases</returns>
        Task<IEnumerable<GitHubRelease>> GetAllReleasesAsync();

        /// <summary>
        /// Downloads a file from the specified URL
        /// </summary>
        /// <param name="downloadUrl">URL to download from</param>
        /// <param name="destinationPath">Local path to save the file</param>
        /// <param name="progressCallback">Progress callback</param>
        /// <returns>True if download was successful</returns>
        Task<bool> DownloadFileAsync(string downloadUrl, string destinationPath,
            System.IProgress<(long bytesReceived, long totalBytes)>? progressCallback = null);
    }

    public class GitHubRelease
    {
        public string TagName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
        public System.DateTime PublishedAt { get; set; }
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    public class GitHubAsset
    {
        public string Name { get; set; } = string.Empty;
        public string BrowserDownloadUrl { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}