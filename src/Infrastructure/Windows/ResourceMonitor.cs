using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Infrastructure.Windows
{
    /// <summary>
    /// Monitors system resource usage including CPU and RAM
    /// </summary>
    public class ResourceMonitor : IResourceMonitor
    {
        private readonly ILogger<ResourceMonitor> _logger;
        private readonly PerformanceCounter _systemCpuCounter;
        private readonly PerformanceCounter _ramCounter;
        private readonly Process _currentProcess;
        private DateTime _lastCpuTime;
        private TimeSpan _lastTotalProcessorTime;
        private bool _disposed = false;

        public ResourceMonitor(ILogger<ResourceMonitor>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ResourceMonitor>.Instance;
            
            try
            {
                // Initialize performance counters
                _systemCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                _currentProcess = Process.GetCurrentProcess();
                
                // Initialize CPU monitoring for application
                _lastCpuTime = DateTime.UtcNow;
                _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
                
                // Initial read to prepare counters
                _systemCpuCounter.NextValue();
                _ramCounter.NextValue();
                
                _logger.LogInformation("Resource monitor initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize resource monitor");
                throw;
            }
        }

        /// <summary>
        /// Gets the current application CPU usage percentage
        /// </summary>
        public float GetCpuUsage()
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                _currentProcess.Refresh();
                var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;

                var cpuUsedMs = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
                var totalMsPassed = (currentTime - _lastCpuTime).TotalMilliseconds;
                
                if (totalMsPassed == 0) return 0;
                
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                _lastCpuTime = currentTime;
                _lastTotalProcessorTime = currentTotalProcessorTime;

                return (float)(cpuUsageTotal * 100);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting CPU usage");
                return 0;
            }
        }

        /// <summary>
        /// Gets the current application memory usage in MB (Private Working Set - matches Task Manager)
        /// </summary>
        public long GetApplicationMemoryUsage()
        {
            try
            {
                _currentProcess.Refresh();
                // Use PrivateMemorySize64 which is closer to what Task Manager shows
                // This represents the private (non-shared) memory committed by the process
                return _currentProcess.PrivateMemorySize64 / 1024 / 1024; // Convert to MB
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting application memory usage");
                return 0;
            }
        }

        /// <summary>
        /// Gets the current application working set memory usage in MB
        /// </summary>
        public long GetApplicationWorkingSetUsage()
        {
            try
            {
                _currentProcess.Refresh();
                return _currentProcess.WorkingSet64 / 1024 / 1024; // Convert to MB
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting application working set usage");
                return 0;
            }
        }

        /// <summary>
        /// Gets the application memory usage that matches Task Manager's "Memory" column
        /// </summary>
        public long GetTaskManagerMemoryUsage()
        {
            try
            {
                _currentProcess.Refresh();
                // Task Manager's "Memory" column typically shows Private Working Set
                // We'll use the smaller of Private Memory Size and Working Set for accuracy
                var privateMemoryMB = _currentProcess.PrivateMemorySize64 / 1024 / 1024;
                var workingSetMB = _currentProcess.WorkingSet64 / 1024 / 1024;
                
                // Return the private memory size as it's closer to what Task Manager shows
                return privateMemoryMB;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting task manager memory usage");
                return 0;
            }
        }

        /// <summary>
        /// Gets a compact resource usage string for status bar (application-focused)
        /// </summary>
        public string GetCompactResourceString()
        {
            try
            {
                var appCpuUsage = GetCpuUsage();
                var appMemoryUsage = GetTaskManagerMemoryUsage();

                return $"CPU: {appCpuUsage:F1}% | RAM: {appMemoryUsage}MB";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error formatting compact resource string");
                return "Resource info unavailable";
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _systemCpuCounter?.Dispose();
                    _ramCounter?.Dispose();
                    _currentProcess?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing resource monitor");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}
