using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Domain.Interfaces;

namespace SimBlock.Infrastructure.Windows
{
    /// <summary>
    /// Windows-specific implementation of mouse information service using Win32 API and WMI
    /// </summary>
    public class WindowsMouseInfoService : IMouseInfoService
    {
        private readonly ILogger<WindowsMouseInfoService> _logger;
        private string _cachedMouseName = string.Empty;
        private string _cachedConnectionType = string.Empty;
        private int _cachedButtonCount = 0;
        private int _cachedDpi = 0;
        private bool _cachedHasScrollWheel = false;
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromSeconds(30);

        public WindowsMouseInfoService(ILogger<WindowsMouseInfoService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetCurrentMouseNameAsync()
        {
            return await Task.Run(() => GetCurrentMouseName());
        }

        public async Task<int> GetMouseButtonCountAsync()
        {
            return await Task.Run(() => GetMouseButtonCount());
        }

        public async Task<int> GetMouseDpiAsync()
        {
            return await Task.Run(() => GetMouseDpi());
        }

        public async Task<bool> HasScrollWheelAsync()
        {
            return await Task.Run(() => HasScrollWheel());
        }

        public async Task<string> GetConnectionTypeAsync()
        {
            return await Task.Run(() => GetConnectionType());
        }

        private string GetCurrentMouseName()
        {
            try
            {
                // Check if we have cached data that's still valid
                if (IsCacheValid() && !string.IsNullOrEmpty(_cachedMouseName))
                {
                    return _cachedMouseName;
                }

                // Use WMI to get mouse information
                var mouseName = GetMouseNameFromWMI();
                
                // Update cache
                _cachedMouseName = mouseName;
                _lastCacheUpdate = DateTime.Now;
                
                _logger.LogDebug("Retrieved mouse name: {MouseName}", mouseName);
                
                return mouseName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mouse name");
                return "Unknown Mouse";
            }
        }

        private int GetMouseButtonCount()
        {
            try
            {
                // Check if we have cached data that's still valid
                if (IsCacheValid() && _cachedButtonCount > 0)
                {
                    return _cachedButtonCount;
                }

                // Use GetSystemMetrics to get button count
                var buttonCount = NativeMethods.GetSystemMetrics(NativeMethods.SM_CMOUSEBUTTONS);
                
                // Fallback to 2 if the API returns 0 or negative
                if (buttonCount <= 0)
                {
                    buttonCount = 2; // Standard left and right buttons
                }

                _cachedButtonCount = buttonCount;
                _lastCacheUpdate = DateTime.Now;
                
                _logger.LogDebug("Retrieved mouse button count: {ButtonCount}", buttonCount);
                
                return buttonCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mouse button count");
                return 2; // Default fallback
            }
        }

        private int GetMouseDpi()
        {
            try
            {
                // Check if we have cached data that's still valid
                if (IsCacheValid() && _cachedDpi > 0)
                {
                    return _cachedDpi;
                }

                // For now, we'll use a standard DPI value as getting actual mouse DPI
                // requires more complex registry queries or driver-specific APIs
                var dpi = GetMouseDpiFromRegistry();
                
                _cachedDpi = dpi;
                _lastCacheUpdate = DateTime.Now;
                
                _logger.LogDebug("Retrieved mouse DPI: {Dpi}", dpi);
                
                return dpi;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mouse DPI");
                return 800; // Standard DPI fallback
            }
        }

        private bool HasScrollWheel()
        {
            try
            {
                // Check if we have cached data that's still valid
                if (IsCacheValid())
                {
                    return _cachedHasScrollWheel;
                }

                // Use GetSystemMetrics to check for scroll wheel
                var hasWheel = NativeMethods.GetSystemMetrics(NativeMethods.SM_MOUSEWHEELPRESENT) != 0;
                
                _cachedHasScrollWheel = hasWheel;
                _lastCacheUpdate = DateTime.Now;
                
                _logger.LogDebug("Mouse has scroll wheel: {HasScrollWheel}", hasWheel);
                
                return hasWheel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for scroll wheel");
                return true; // Most modern mice have scroll wheels
            }
        }

        private string GetConnectionType()
        {
            try
            {
                // Check if we have cached data that's still valid
                if (IsCacheValid() && !string.IsNullOrEmpty(_cachedConnectionType))
                {
                    return _cachedConnectionType;
                }

                // Get connection type from WMI
                var connectionType = GetConnectionTypeFromWMI();
                
                _cachedConnectionType = connectionType;
                _lastCacheUpdate = DateTime.Now;
                
                _logger.LogDebug("Retrieved mouse connection type: {ConnectionType}", connectionType);
                
                return connectionType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mouse connection type");
                return "Unknown";
            }
        }

        private bool IsCacheValid()
        {
            return DateTime.Now - _lastCacheUpdate < _cacheExpiration;
        }

        private string GetMouseNameFromWMI()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "path Win32_PointingDevice get Name,DeviceID,Description,HardwareType /format:csv",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogWarning("Failed to start wmic process");
                    return "Unknown Mouse";
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("wmic command failed with exit code: {ExitCode}", process.ExitCode);
                    return "Unknown Mouse";
                }

                _logger.LogDebug("WMI output: {Output}", output);

                // Clean up the output and parse it
                var cleanOutput = output.Replace("&amp;", "&");
                var lines = cleanOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Find the best mouse entry
                string bestMouseName = "Standard Mouse";
                bool foundUSBMouse = false;
                
                foreach (var line in lines.Skip(1)) // Skip header
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Split(',');
                    if (parts.Length >= 5)
                    {
                        var description = parts[1]?.Trim();
                        var deviceId = parts[2]?.Trim();
                        var hardwareType = parts[3]?.Trim();
                        var name = parts[4]?.Trim();

                        _logger.LogDebug("Parsing mouse entry - Description: {Description}, DeviceID: {DeviceID}, HardwareType: {HardwareType}, Name: {Name}",
                            description, deviceId, hardwareType, name);

                        // Determine mouse type based on device ID and description
                        if (!string.IsNullOrEmpty(deviceId))
                        {
                            if (deviceId.Contains("USB") && description?.Contains("USB") == true)
                            {
                                if (!foundUSBMouse)
                                {
                                    bestMouseName = DetermineMouseType(deviceId, description ?? "", name ?? "");
                                    foundUSBMouse = true;
                                }
                            }
                            else if (!foundUSBMouse && deviceId.Contains("HID"))
                            {
                                bestMouseName = DetermineMouseType(deviceId, description ?? "", name ?? "");
                            }
                        }
                    }
                }

                return CleanMouseName(bestMouseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mouse name from WMI");
                return "Unknown Mouse";
            }
        }

        private string DetermineMouseType(string deviceId, string description, string name)
        {
            // Try to extract manufacturer information from device ID
            if (!string.IsNullOrEmpty(deviceId))
            {
                if (deviceId.Contains("VID_"))
                {
                    var vidStart = deviceId.IndexOf("VID_") + 4;
                    var vidEnd = deviceId.IndexOf("&", vidStart);
                    if (vidEnd == -1) vidEnd = deviceId.Length;
                    
                    var vid = deviceId.Substring(vidStart, Math.Min(4, vidEnd - vidStart));
                    var manufacturer = GetManufacturerFromVID(vid);
                    
                    if (!string.IsNullOrEmpty(manufacturer))
                    {
                        return $"{manufacturer} Mouse (VID: {vid.ToUpper()})";
                    }
                    else
                    {
                        return $"USB Mouse (VID: {vid.ToUpper()})";
                    }
                }
            }

            // Use description to determine type
            if (!string.IsNullOrEmpty(description))
            {
                if (description.Contains("USB"))
                {
                    return "USB Mouse";
                }
                else if (description.Contains("HID"))
                {
                    return "HID Mouse";
                }
                else if (description.Contains("Bluetooth"))
                {
                    return "Bluetooth Mouse";
                }
            }

            // Fallback to name if available
            if (!string.IsNullOrEmpty(name) && !name.Contains("Standard"))
            {
                return name;
            }

            return "Standard Mouse";
        }

        private string? GetManufacturerFromVID(string vid)
        {
            return vid.ToUpper() switch
            {
                "046D" => "Logitech",
                "045E" => "Microsoft",
                "1532" => "Razer",
                "1B1C" => "Corsair",
                "0458" => "Genius",
                "04F2" => "Chicony",
                "04CA" => "Lite-On",
                "1A2C" => "China Resource Semico",
                "0A5C" => "Broadcom",
                "8087" => "Intel",
                "04D9" => "Holtek",
                "1BCF" => "Sunplus Innovation Technology",
                "17EF" => "Lenovo",
                "413C" => "Dell",
                "03F0" => "HP",
                "0B05" => "ASUS",
                _ => null
            };
        }

        private string CleanMouseName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Unknown Mouse";

            // Remove quotes and extra whitespace
            name = name.Trim().Trim('"');
            
            // If the name is too technical, make it more user-friendly
            if (name.Contains("HID") || name.Contains("USB") || name.Contains("VID_") || name.Contains("PID_"))
            {
                if (name.ToLower().Contains("mouse"))
                {
                    return "USB Mouse";
                }
                return "External Mouse";
            }

            // Ensure it ends with "Mouse" if it doesn't already
            if (!name.ToLower().Contains("mouse"))
            {
                name += " Mouse";
            }

            return name;
        }

        private string GetConnectionTypeFromWMI()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "path Win32_PointingDevice get DeviceID,Description /format:csv",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return "Unknown";
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return "Unknown";
                }

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines.Skip(1)) // Skip header
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        var description = parts[1]?.Trim();
                        var deviceId = parts[2]?.Trim();

                        if (!string.IsNullOrEmpty(deviceId))
                        {
                            if (deviceId.Contains("USB"))
                                return "USB";
                            else if (deviceId.Contains("BLUETOOTH") || description?.Contains("Bluetooth") == true)
                                return "Bluetooth";
                            else if (deviceId.Contains("PS2") || deviceId.Contains("PS/2"))
                                return "PS/2";
                            else if (deviceId.Contains("HID"))
                                return "HID";
                        }
                    }
                }

                return "Integrated";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mouse connection type from WMI");
                return "Unknown";
            }
        }

        private int GetMouseDpiFromRegistry()
        {
            try
            {
                // This is a simplified approach - actual DPI detection would require
                // more complex registry queries or driver-specific APIs
                // For now, we'll return a reasonable default based on system DPI
                
                // Get system DPI as a baseline
                var systemDpi = GetSystemDpi();
                
                // Estimate mouse DPI based on system DPI
                // This is a rough estimation and not accurate for all mice
                return systemDpi > 96 ? 1200 : 800;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mouse DPI from registry");
                return 800; // Standard DPI fallback
            }
        }

        private int GetSystemDpi()
        {
            try
            {
                // Get system DPI - this is a rough approximation
                // A more accurate implementation would use GetDeviceCaps or similar
                return 96; // Standard Windows DPI
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system DPI");
                return 96;
            }
        }
    }
}