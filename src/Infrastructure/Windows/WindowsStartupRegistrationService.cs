using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SimBlock.Presentation.Interfaces;
using System;

namespace SimBlock.Infrastructure.Windows
{
    /// <summary>
    /// Windows-specific implementation for registering the app to run at user logon.
    /// </summary>
    public class WindowsStartupRegistrationService : IStartupRegistrationService
    {
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SimBlock";
        private readonly ILogger<WindowsStartupRegistrationService> _logger;

        public WindowsStartupRegistrationService(ILogger<WindowsStartupRegistrationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsApplicationInStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
                if (key == null)
                {
                    return false;
                }
                var value = key.GetValue(AppName);
                return value != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking startup state from registry");
                return false;
            }
        }

        public void SetStartWithWindows(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
                if (key == null)
                {
                    throw new InvalidOperationException("Unable to access Windows startup registry key");
                }

                if (enable)
                {
                    var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    if (executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        executablePath = Environment.ProcessPath ?? executablePath;
                    }
                    key.SetValue(AppName, $"\"{executablePath}\"");
                    _logger.LogInformation("Added {ApplicationName} to Windows startup", AppName);
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    _logger.LogInformation("Removed {ApplicationName} from Windows startup", AppName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying startup registration setting");
                throw;
            }
        }
    }
}
