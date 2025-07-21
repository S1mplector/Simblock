using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Presentation.Forms;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimBlock.Presentation.Services
{
    public interface IAutoUpdateManager
    {
        /// <summary>
        /// Checks for updates and shows update dialog if available
        /// </summary>
        Task CheckForUpdatesAsync(bool showNoUpdateMessage = false);
        void Dispose();


        /// <summary>
        /// Starts automatic update checking with specified interval
        /// </summary>
        void StartAutomaticUpdateChecking(TimeSpan interval);

        /// <summary>
        /// Stops automatic update checking
        /// </summary>
        void StopAutomaticUpdateChecking();
    }

    public class AutoUpdateManager : IAutoUpdateManager, IDisposable
    {
        private readonly IAutoUpdateService _autoUpdateService;
        private readonly ILogger<AutoUpdateManager> _logger;
        private System.Threading.Timer? _updateTimer;
        private bool _disposed;

        public AutoUpdateManager(IAutoUpdateService autoUpdateService, ILogger<AutoUpdateManager> logger)
        {
            _autoUpdateService = autoUpdateService;
            _logger = logger;
        }

        public async Task CheckForUpdatesAsync(bool showNoUpdateMessage = false)
        {
            try
            {
                _logger.LogInformation("Checking for updates...");

                var updateInfo = await _autoUpdateService.CheckForUpdatesAsync();

                if (updateInfo != null)
                {
                    _logger.LogInformation("Update available: {Version}", updateInfo.Version);

                    // Show update dialog on UI thread
                    if (Application.OpenForms.Count > 0)
                    {
                        var mainForm = Application.OpenForms[0];
                        if (mainForm.InvokeRequired)
                        {
                            mainForm.Invoke(new Action(() => ShowUpdateDialog(updateInfo)));
                        }
                        else
                        {
                            ShowUpdateDialog(updateInfo);
                        }
                    }
                    else
                    {
                        ShowUpdateDialog(updateInfo);
                    }
                }
                else
                {
                    _logger.LogInformation("No updates available");

                    if (showNoUpdateMessage)
                    {
                        MessageBox.Show("You are running the latest version of SimBlock.",
                            "No Updates Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");

                if (showNoUpdateMessage)
                {
                    MessageBox.Show("Unable to check for updates. Please check your internet connection and try again.",
                        "Update Check Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        public void StartAutomaticUpdateChecking(TimeSpan interval)
        {
            _logger.LogInformation("Starting automatic update checking with interval: {Interval}", interval);

            StopAutomaticUpdateChecking();

            _updateTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    await CheckForUpdatesAsync(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during automatic update check");
                }
            }, null, TimeSpan.Zero, interval);
        }

        public void StopAutomaticUpdateChecking()
        {
            if (_updateTimer != null)
            {
                _logger.LogInformation("Stopping automatic update checking");
                _updateTimer.Dispose();
                _updateTimer = null;
            }
        }

        private void ShowUpdateDialog(UpdateInfo updateInfo)
        {
            try
            {
                using var updateDialog = new UpdateDialog(_autoUpdateService, updateInfo);
                var result = updateDialog.ShowDialog();

                switch (result)
                {
                    case DialogResult.OK:
                        _logger.LogInformation("User accepted update");
                        break;
                    case DialogResult.Ignore:
                        _logger.LogInformation("User skipped update");
                        break;
                    case DialogResult.Cancel:
                        _logger.LogInformation("User cancelled update");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing update dialog");
                MessageBox.Show("Error displaying update information.", "Update Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopAutomaticUpdateChecking();
                _disposed = true;
            }
        }
    }
}