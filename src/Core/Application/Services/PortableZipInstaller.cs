using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace SimBlock.Core.Application.Services
{
    /// <summary>
    /// Handles installation of portable (zip) updates: shows where the update has been downloaded,
    /// extracts the zip, starts the new executable and closes the current instance.
    /// </summary>
    internal static class PortableZipInstaller
    {
        private const string AppName = "SimBlock";
        private const string RegistryKeyPath = @"SOFTWARE\SimBlock";
        private const string InstallLocationValue = "InstallLocation";
        public static async Task<bool> InstallAsync(string zipPath)
        {
            try
            {
                if (!File.Exists(zipPath))
                    throw new FileNotFoundException("Zip file not found", zipPath);

                // Get install location from registry or prompt user
                string? installDir = GetInstallLocation();
                if (string.IsNullOrEmpty(installDir))
                {
                    using var dialog = new FolderBrowserDialog
                    {
                        Description = "Select installation directory for SimBlock update",
                        ShowNewFolderButton = true,
                        RootFolder = Environment.SpecialFolder.ProgramFiles
                    };

                    if (dialog.ShowDialog() != DialogResult.OK)
                    {
                        // User cancelled
                        return false;
                    }
                    installDir = dialog.SelectedPath;
                }

                // Create version-specific subdirectory
                string versionDir = Path.Combine(installDir, $"v{DateTime.Now:yyyyMMddHHmmss}");
                Directory.CreateDirectory(versionDir);

                // Extract the update
                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, versionDir));

                // Find the main executable
                string? exePath = Directory.GetFiles(versionDir, "*.exe", SearchOption.AllDirectories)
                    .FirstOrDefault(f => Path.GetFileName(f).StartsWith(AppName, StringComparison.OrdinalIgnoreCase));

                if (exePath == null)
                {
                    WinForms.MessageBox.Show(
                        "Could not locate the new executable in the extracted files.", 
                        "Update Error", 
                        WinForms.MessageBoxButtons.OK, 
                        WinForms.MessageBoxIcon.Error);
                    return false;
                }

                // Save install location to registry for future updates
                SaveInstallLocation(installDir);

                // Start the new version
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? versionDir,
                    Arguments = "--updated" // Optional: add a flag to indicate this is an update
                };

                // Start the new instance before exiting
                Process.Start(startInfo);

                // Give the new process a moment to start
                await Task.Delay(1000);

                // Force exit the current application
                Environment.Exit(0);
                return true;
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show($"Failed to install update: {ex.Message}", "Update Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                return false;
            }
        }

        private static string? GetInstallLocation()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
                return key?.GetValue(InstallLocationValue) as string;
            }
            catch
            {
                // If registry access fails, return null to prompt user
                return null;
            }
        }

        private static void SaveInstallLocation(string path)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
                key?.SetValue(InstallLocationValue, path);
            }
            catch
            {
                // Non-critical if we can't save to registry
            }
        }
    }
}
