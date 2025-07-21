using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using SimBlock.Core.Application.Interfaces;
using WinForms = System.Windows.Forms;

namespace SimBlock.Core.Application.Services
{
    /// <summary>
    /// Handles installation of portable (zip) updates: shows where the update has been downloaded,
    /// extracts the zip, starts the new executable and closes the current instance.
    /// </summary>
    internal static class PortableZipInstaller
    {
        private static UpdateInfo? _updateInfo;
        private const string AppName = "SimBlock";
        private const string RegistryKeyPath = @"SOFTWARE\SimBlock";
        private const string InstallLocationValue = "InstallLocation";
        public static async Task<bool> InstallAsync(string zipPath, UpdateInfo? updateInfo = null)
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

                // Create a friendly version directory name with version and date
                string version = _updateInfo?.Version?.TrimEnd('.', '0') ?? "Update";
                string today = DateTime.Now.ToString("yy/MM/dd");
                string baseDirName = $"SimBlock Update (v{version}) {today}";
                string versionDir = Path.Combine(installDir, baseDirName);
                
                // Ensure directory doesn't exist or add a number suffix
                int counter = 1;
                string finalDir = versionDir;
                while (Directory.Exists(finalDir))
                {
                    finalDir = $"{versionDir} ({counter++})";
                }
                
                Directory.CreateDirectory(finalDir);

                // Extract the update
                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, finalDir));

                // Find the main executable
                string? exePath = Directory.GetFiles(finalDir, "*.exe", SearchOption.AllDirectories)
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

                // Create a PowerShell script to handle the process management
                string scriptPath = Path.Combine(Path.GetTempPath(), $"SimBlock_Update_{Guid.NewGuid()}.ps1");
                string scriptContent = @"
                    param([string]$newExePath, [string]$workingDir, [int]$processId)
                    
                    # Wait for the current process to exit (with timeout)
                    $timeout = 30 # seconds
                    $startTime = Get-Date
                    
                    while ((Get-Process -Id $processId -ErrorAction SilentlyContinue) -ne $null) {
                        if (((Get-Date) - $startTime).TotalSeconds -gt $timeout) {
                            # Force kill if timeout reached
                            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
                            break
                        }
                        Start-Sleep -Milliseconds 100
                    }
                    
                    # Start the new version
                    Start-Process -FilePath $newExePath -WorkingDirectory $workingDir -ArgumentList '--updated'
                ";

                // Write the script to a temporary file
                await File.WriteAllTextAsync(scriptPath, scriptContent);

                // Start the PowerShell script
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -newExePath \"{exePath}\" -workingDir \"{Path.GetDirectoryName(exePath) ?? finalDir}\" -processId {Process.GetCurrentProcess().Id}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                // Start the PowerShell script and immediately exit
                Process.Start(startInfo);
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
