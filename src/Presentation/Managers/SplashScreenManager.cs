using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Forms;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Presentation.Managers
{
    /// <summary>
    /// Manages the application splash screen lifecycle
    /// </summary>
    public class SplashScreenManager : ISplashScreenManager
    {
        private readonly UISettings _uiSettings;
        private readonly IThemeManager _themeManager;
        private readonly ILogoManager _logoManager;
        private readonly ILogger<SplashScreenManager> _logger;

        private SplashForm? _splashForm;
        private bool _disposed = false;

        public event EventHandler? SplashScreenClosed;

        public bool IsVisible => _splashForm?.Visible ?? false;

        public SplashScreenManager(
            UISettings uiSettings,
            IThemeManager themeManager,
            ILogoManager logoManager,
            ILogger<SplashScreenManager> logger)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            _logoManager = logoManager ?? throw new ArgumentNullException(nameof(logoManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Shows the splash screen
        /// </summary>
        /// <returns>Task representing the show operation</returns>
        public async Task ShowAsync()
        {
            if (_disposed)
            {
                _logger.LogWarning("Attempted to show splash screen after disposal");
                return;
            }

            try
            {
                if (_splashForm != null)
                {
                    _logger.LogWarning("Splash screen is already created");
                    return;
                }

                // Create splash form directly on the UI thread (Program.Main is [STAThread])
                CreateAndShowSplashForm();

                // Give the splash screen time to render
                await Task.Delay(100);
                Application.DoEvents();

                _logger.LogInformation("Splash screen shown successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show splash screen");
                throw;
            }
        }

        /// <summary>
        /// Updates the splash screen progress
        /// </summary>
        /// <param name="percentage">Progress percentage (0-100)</param>
        /// <param name="status">Status message describing current operation</param>
        public void UpdateProgress(int percentage, string status)
        {
            if (_disposed || _splashForm == null)
                return;

            try
            {
                // Ensure progress updates happen on the UI thread
                if (_splashForm.InvokeRequired)
                {
                    _splashForm.Invoke(new Action<int, string>(_splashForm.UpdateProgress), percentage, status);
                }
                else
                {
                    _splashForm.UpdateProgress(percentage, status);
                }

                // Process pending Windows messages to ensure UI updates
                Application.DoEvents();
                
                _logger.LogDebug("Splash screen progress updated: {Progress}% - {Status}", percentage, status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating splash screen progress");
            }
        }

        /// <summary>
        /// Closes the splash screen
        /// </summary>
        /// <returns>Task representing the close operation</returns>
        public async Task CloseAsync()
        {
            if (_disposed)
                return;

            try
            {
                if (_splashForm == null)
                {
                    _logger.LogDebug("No splash screen to close");
                    return;
                }

                // Close splash form on the UI thread
                if (_splashForm.InvokeRequired)
                {
                    _splashForm.Invoke(new Action(CloseSplashForm));
                }
                else
                {
                    CloseSplashForm();
                }

                // Small delay to ensure smooth transition
                await Task.Delay(100);

                _logger.LogInformation("Splash screen closed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing splash screen");
            }
        }

        private void CreateAndShowSplashForm()
        {
            try
            {
                // Create logger for SplashForm using the same logger factory
                var splashFormLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                }).CreateLogger<SplashForm>();

                _splashForm = new SplashForm(_uiSettings, _themeManager, _logoManager, splashFormLogger);
                
                _splashForm.FormClosed += OnSplashFormClosed;
                _splashForm.Show();
                _splashForm.BringToFront();
                _splashForm.Activate();

                // Process Windows messages to ensure the form is fully rendered
                Application.DoEvents();

                _logger.LogDebug("Splash form created and shown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating splash form");
                throw;
            }
        }

        private void CloseSplashForm()
        {
            try
            {
                if (_splashForm != null)
                {
                    _splashForm.FormClosed -= OnSplashFormClosed;
                    _splashForm.Close();
                    _splashForm.Dispose();
                    _splashForm = null;
                }

                // Raise the closed event
                SplashScreenClosed?.Invoke(this, EventArgs.Empty);

                _logger.LogDebug("Splash form closed and disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing splash form");
            }
        }

        private void OnSplashFormClosed(object? sender, FormClosedEventArgs e)
        {
            try
            {
                if (_splashForm != null)
                {
                    _splashForm.FormClosed -= OnSplashFormClosed;
                    _splashForm.Dispose();
                    _splashForm = null;
                }

                // Raise the closed event
                SplashScreenClosed?.Invoke(this, EventArgs.Empty);

                _logger.LogDebug("Splash form closed event handled");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling splash form closed event");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // Close splash screen if still open
                    if (_splashForm != null)
                    {
                        CloseAsync().Wait(1000); // Wait max 1 second
                    }

                    _disposed = true;
                    _logger.LogDebug("SplashScreenManager disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during SplashScreenManager disposal");
                }
            }
        }
    }
}