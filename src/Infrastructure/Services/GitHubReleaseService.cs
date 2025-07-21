using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimBlock.Infrastructure.Services
{
    public class GitHubReleaseService : IGitHubReleaseService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubReleaseService> _logger;
        private const string GitHubApiUrl = "https://api.github.com/repos/S1mplector/Simblock/releases";
        private bool _disposed;

        public GitHubReleaseService(ILogger<GitHubReleaseService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SimBlock-AutoUpdater/1.0");
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            try
            {
                _logger.LogInformation("Fetching latest release from GitHub...");

                var response = await _httpClient.GetAsync($"{GitHubApiUrl}/latest");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch latest release. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var release = ParseGitHubRelease(jsonContent);

                _logger.LogInformation("Successfully fetched latest release: {Version}", release?.TagName);
                return release;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest release from GitHub");
                return null;
            }
        }

        public async Task<IEnumerable<GitHubRelease>> GetAllReleasesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all releases from GitHub...");

                var response = await _httpClient.GetAsync(GitHubApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch releases. Status: {StatusCode}", response.StatusCode);
                    return new List<GitHubRelease>();
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var releases = ParseGitHubReleases(jsonContent);

                _logger.LogInformation("Successfully fetched {Count} releases", releases.Count);
                return releases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching releases from GitHub");
                return new List<GitHubRelease>();
            }
        }

        public async Task<bool> DownloadFileAsync(string downloadUrl, string destinationPath,
            IProgress<(long bytesReceived, long totalBytes)>? progressCallback = null)
        {
            try
            {
                _logger.LogInformation("Starting download from {Url} to {Path}", downloadUrl, destinationPath);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to download file. Status: {StatusCode}", response.StatusCode);
                    return false;
                }

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var bytesReceived = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    bytesReceived += bytesRead;

                    progressCallback?.Report((bytesReceived, totalBytes));
                }

                _logger.LogInformation("Successfully downloaded file to {Path}. Size: {Size} bytes",
                    destinationPath, bytesReceived);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from {Url}", downloadUrl);

                // Clean up partial download
                try
                {
                    if (File.Exists(destinationPath))
                        File.Delete(destinationPath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up partial download at {Path}", destinationPath);
                }

                return false;
            }
        }

        private GitHubRelease? ParseGitHubRelease(string jsonContent)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                return new GitHubRelease
                {
                    TagName = root.GetProperty("tag_name").GetString() ?? string.Empty,
                    Name = root.GetProperty("name").GetString() ?? string.Empty,
                    Body = root.GetProperty("body").GetString() ?? string.Empty,
                    Draft = root.GetProperty("draft").GetBoolean(),
                    Prerelease = root.GetProperty("prerelease").GetBoolean(),
                    PublishedAt = root.GetProperty("published_at").GetDateTime(),
                    Assets = ParseAssets(root.GetProperty("assets"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing GitHub release JSON");
                return null;
            }
        }

        private List<GitHubRelease> ParseGitHubReleases(string jsonContent)
        {
            var releases = new List<GitHubRelease>();

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                foreach (var releaseElement in root.EnumerateArray())
                {
                    var release = new GitHubRelease
                    {
                        TagName = releaseElement.GetProperty("tag_name").GetString() ?? string.Empty,
                        Name = releaseElement.GetProperty("name").GetString() ?? string.Empty,
                        Body = releaseElement.GetProperty("body").GetString() ?? string.Empty,
                        Draft = releaseElement.GetProperty("draft").GetBoolean(),
                        Prerelease = releaseElement.GetProperty("prerelease").GetBoolean(),
                        PublishedAt = releaseElement.GetProperty("published_at").GetDateTime(),
                        Assets = ParseAssets(releaseElement.GetProperty("assets"))
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing GitHub releases JSON");
            }

            return releases;
        }

        private List<GitHubAsset> ParseAssets(JsonElement assetsElement)
        {
            var assets = new List<GitHubAsset>();

            try
            {
                foreach (var assetElement in assetsElement.EnumerateArray())
                {
                    var asset = new GitHubAsset
                    {
                        Name = assetElement.GetProperty("name").GetString() ?? string.Empty,
                        BrowserDownloadUrl = assetElement.GetProperty("browser_download_url").GetString() ?? string.Empty,
                        Size = assetElement.GetProperty("size").GetInt64(),
                        ContentType = assetElement.GetProperty("content_type").GetString() ?? string.Empty
                    };

                    assets.Add(asset);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing GitHub assets");
            }

            return assets;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}