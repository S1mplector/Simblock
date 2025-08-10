using System;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel for Settings, mediating between UI and services.
    /// Phase 2 minimal: encapsulates load/save and exposes UISettings.
    /// </summary>
    public class SettingsViewModel
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IStartupRegistrationService _startupRegistrationService;
        private readonly ILogger<SettingsViewModel> _logger;

        public UISettings Settings { get; }

        public SettingsViewModel(
            UISettings settings,
            ISettingsManager settingsManager,
            IStartupRegistrationService startupRegistrationService,
            ILogger<SettingsViewModel> logger)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _startupRegistrationService = startupRegistrationService ?? throw new ArgumentNullException(nameof(startupRegistrationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LoadSettings()
        {
            try
            {
                _settingsManager.LoadSettings();
                _logger.LogInformation("Settings loaded via ViewModel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings via ViewModel");
                throw;
            }
        }

        public void SaveSettings()
        {
            try
            {
                _settingsManager.SaveSettings();
                _logger.LogInformation("Settings saved via ViewModel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings via ViewModel");
                throw;
            }
        }

        public bool IsApplicationInStartup()
        {
            return _startupRegistrationService.IsApplicationInStartup();
        }

        public void SetStartWithWindows(bool enable)
        {
            _startupRegistrationService.SetStartWithWindows(enable);
            Settings.StartWithWindows = enable;
        }
    }
}
