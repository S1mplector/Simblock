using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SimBlock.Presentation.Services
{
    public interface IApplicationStartupService
    {
        /// <summary>
        /// Initializes the application startup services including auto-update checking
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Shuts down the startup services
        /// </summary>
        void Shutdown();
    }

    public class ApplicationStartupService : IApplicationStartupService, IDisposable
    {
        private readonly IAutoUpdateManager _autoUpdateManager;
        private readonly ILogger<ApplicationStartupService> _logger;
        private bool _disposed;

        public ApplicationStartupService(
            IAutoUpdateManager autoUpdateManager,
            ILogger<ApplicationStartupService> logger)
        {
            _autoUpdateManager = autoUpdateManager;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing application startup services...");

                // Start automatic update checking every 4 hours
                _autoUpdateManager.StartAutomaticUpdateChecking(TimeSpan.FromHours(4));

                // Perform initial update check after a short delay (to not interfere with startup)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(30)); // Wait 30 seconds after startup
                    await _autoUpdateManager.CheckForUpdatesAsync(false);
                });

                _logger.LogInformation("Application startup services initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing application startup services");
            }
        }

        public void Shutdown()
        {
            try
            {
                _logger.LogInformation("Shutting down application startup services...");
                _autoUpdateManager.StopAutomaticUpdateChecking();
                _logger.LogInformation("Application startup services shut down successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down application startup services");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Shutdown();
                _autoUpdateManager?.Dispose();
                _disposed = true;
            }
        }
    }
}