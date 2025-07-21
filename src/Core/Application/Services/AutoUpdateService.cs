using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using WinForms = System.Windows.Forms;

namespace SimBlock.Core.Application.Services
{
    public class AutoUpdateService : IAutoUpdateService
    {
        private readonly IGitHubReleaseService _gitHubReleaseService;
        private readonly IVersionComparator _versionComparator;
        private readonly ILogger<AutoUpdateService> _logger;

        public event EventHandler<UpdateProgressEventArgs>? UpdateProgressChanged;
        public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

        public AutoUpdateService(
            IGitHubReleaseService gitHubReleaseService,
            IVersionComparator versionComparator,
            ILogger<AutoUpdateService> logger)
        {
            _gitHubReleaseService = gitHubReleaseService;
            _versionComparator = versionComparator;
            _logger = logger;
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                _logger.LogInformation("Checking for updates...");

                var currentVersion = GetCurrentVersion();
                _logger.LogInformation("Current version: {Version}", currentVersion);

                var latestRelease = await _gitHubReleaseService.GetLatestReleaseAsync();
                if (latestRelease == null)
                {
                    _logger.LogWarning("Could not fetch latest release information");
                    return null;
                }

                // Skip draft and pre-release versions
                if (latestRelease.Draft || latestRelease.Prerelease)
                {
                    _logger.LogInformation("Latest release is draft or pre-release, skipping");
                    return null;
                }

                var latestVersion = latestRelease.TagName;
                _logger.LogInformation("Latest version: {Version}", latestVersion);

                if (!_versionComparator.IsNewerVersion(currentVersion, latestVersion))
                {
                    _logger.LogInformation("No update available. Current version is up to date.");
                    return null;
                }

                // Find the appropriate asset (executable file)
                var asset = FindExecutableAsset(latestRelease);
                if (asset == null)
                {
                    _logger.LogWarning("No suitable executable found in release assets");
                    return null;
                }

                var updateInfo = new UpdateInfo
                {
                    Version = latestVersion,
                    DownloadUrl = asset.BrowserDownloadUrl,
                    ReleaseNotes = latestRelease.Body,
                    PublishedAt = latestRelease.PublishedAt,
                    FileSize = asset.Size,
                    FileName = asset.Name
                };

                _logger.LogInformation("Update available: {Version}", updateInfo.Version);

                // Fire event
                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs { UpdateInfo = updateInfo });

                return updateInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                return null;
            }
        }

        public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                _logger.LogInformation("Starting update process for version {Version}", updateInfo.Version);

                // Create temp directory for update
                var tempDir = Path.Combine(Path.GetTempPath(), "SimBlockUpdate");
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);

                Directory.CreateDirectory(tempDir);

                var downloadPath = Path.Combine(tempDir, updateInfo.FileName);

                // Report initial progress
                ReportProgress(0, "Starting download...", 0, updateInfo.FileSize);

                // Download the update
                var progress = new Progress<(long bytesReceived, long totalBytes)>(p =>
                {
                    var percentage = p.totalBytes > 0 ? (int)((p.bytesReceived * 100) / p.totalBytes) : 0;
                    ReportProgress(percentage, "Downloading update...", p.bytesReceived, p.totalBytes);
                });

                var downloadSuccess = await _gitHubReleaseService.DownloadFileAsync(
                    updateInfo.DownloadUrl, downloadPath, progress);

                if (!downloadSuccess)
                {
                    _logger.LogError("Failed to download update");
                    ReportProgress(0, "Download failed", 0, 0);
                    return false;
                }

                ReportProgress(100, "Download completed. Preparing to install...", updateInfo.FileSize, updateInfo.FileSize);

                // Verify the downloaded file exists and has the expected size
                var fileInfo = new FileInfo(downloadPath);
                if (!fileInfo.Exists)
                {
                    _logger.LogError("Downloaded file does not exist");
                    return false;
                }

                if (fileInfo.Length != updateInfo.FileSize)
                {
                    _logger.LogWarning("Downloaded file size mismatch. Expected: {Expected}, Actual: {Actual}",
                        updateInfo.FileSize, fileInfo.Length);
                }

                // If we downloaded a portable zip, handle via PortableZipInstaller
                if (Path.GetExtension(downloadPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return await PortableZipInstaller.InstallAsync(downloadPath);
                }

                // Otherwise, proceed with traditional installer/update script
                return await InstallUpdateAsync(downloadPath, updateInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during update process");
                ReportProgress(0, $"Update failed: {ex.Message}", 0, 0);
                return false;
            }
        }

        public string GetCurrentVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString(3) ?? "1.0.0"; // Return major.minor.patch
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not determine current version, defaulting to 1.0.0");
                return "1.0.0";
            }
        }

        private GitHubAsset? FindExecutableAsset(GitHubRelease release)
        {
            // Look for .exe files first
            var exeAsset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                a.Name.Contains("SimBlock", StringComparison.OrdinalIgnoreCase));

            if (exeAsset != null)
                return exeAsset;

            // Look for .zip files containing the application
            var zipAsset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                a.Name.Contains("SimBlock", StringComparison.OrdinalIgnoreCase));

            return zipAsset;
        }

        private async Task<bool> InstallUpdateAsync(string downloadPath, UpdateInfo updateInfo)
        {
            try
            {
                ReportProgress(100, "Installing update...", updateInfo.FileSize, updateInfo.FileSize);

                var currentExecutable = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExecutable))
                {
                    _logger.LogError("Could not determine current executable path");
                    return false;
                }

                var currentDirectory = Path.GetDirectoryName(currentExecutable);
                if (string.IsNullOrEmpty(currentDirectory))
                {
                    _logger.LogError("Could not determine current directory");
                    return false;
                }

                // Create update script
                var updateScript = CreateUpdateScript(downloadPath, currentExecutable, updateInfo);
                var scriptPath = Path.Combine(Path.GetTempPath(), "SimBlockUpdater.bat");

                await File.WriteAllTextAsync(scriptPath, updateScript);

                _logger.LogInformation("Starting update script: {ScriptPath}", scriptPath);

                // Start the update script and exit current application
                var startInfo = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);

                // Give the script a moment to start
                await Task.Delay(1000);

                // Exit the current application
                _logger.LogInformation("Exiting application for update...");
                WinForms.Application.Exit();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error installing update");
                return false;
            }
        }

        private string CreateUpdateScript(string downloadPath, string currentExecutable, UpdateInfo updateInfo)
        {
            var backupPath = currentExecutable + ".backup";
            var script = $@"@echo off
echo SimBlock Auto-Updater
echo Updating to version {updateInfo.Version}...

REM Wait for the main application to close
timeout /t 3 /nobreak > nul

REM Create backup of current executable
echo Creating backup...
copy ""{currentExecutable}"" ""{backupPath}"" > nul
if errorlevel 1 (
    echo Failed to create backup
    pause
    exit /b 1
)

REM Replace the executable
echo Installing update...
copy ""{downloadPath}"" ""{currentExecutable}"" > nul
if errorlevel 1 (
    echo Failed to install update, restoring backup...
    copy ""{backupPath}"" ""{currentExecutable}"" > nul
    del ""{backupPath}"" > nul 2>&1
    pause
    exit /b 1
)

REM Clean up
echo Cleaning up...
del ""{backupPath}"" > nul 2>&1
del ""{downloadPath}"" > nul 2>&1
rmdir ""{Path.GetDirectoryName(downloadPath)}"" > nul 2>&1

REM Start the updated application
echo Starting updated application...
start """" ""{currentExecutable}""

REM Clean up this script
del ""%~f0"" > nul 2>&1
";

            return script;
        }

        private void ReportProgress(int percentage, string status, long bytesReceived, long totalBytes)
        {
            UpdateProgressChanged?.Invoke(this, new UpdateProgressEventArgs
            {
                ProgressPercentage = percentage,
                Status = status,
                BytesReceived = bytesReceived,
                TotalBytes = totalBytes
            });
        }
    }
}