using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Domain.Interfaces;

namespace SimBlock.Infrastructure.Windows
{
    /// <summary>
    /// Windows-specific implementation of keyboard information service using WMI
    /// </summary>
    public class WindowsKeyboardInfoService : IKeyboardInfoService
    {
        private readonly ILogger<WindowsKeyboardInfoService> _logger;
        private string _cachedKeyboardName = string.Empty;
        private string _cachedKeyboardLanguage = string.Empty;
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromSeconds(30);

        public WindowsKeyboardInfoService(ILogger<WindowsKeyboardInfoService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetCurrentKeyboardNameAsync()
        {
            return await Task.Run(() => GetCurrentKeyboardName());
        }

        public async Task<string> GetCurrentKeyboardLanguageAsync()
        {
            return await Task.Run(() => GetCurrentKeyboardLanguage());
        }

        private string GetCurrentKeyboardName()
        {
            try
            {
                // Check if we have cached data that's still valid
                if (IsCacheValid() && !string.IsNullOrEmpty(_cachedKeyboardName))
                {
                    return _cachedKeyboardName;
                }

                // Use WMI to get keyboard information
                var keyboardName = GetKeyboardNameFromWMI();
                
                // Update cache
                _cachedKeyboardName = keyboardName;
                _lastCacheUpdate = DateTime.Now;
                
                _logger.LogDebug("Retrieved keyboard name: {KeyboardName}", keyboardName);
                
                return keyboardName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving keyboard name");
                return "Unknown Keyboard";
            }
        }

        private string GetCurrentKeyboardLanguage()
        {
            try
            {
                // Check if we have cached data that's still valid
                if (IsCacheValid() && !string.IsNullOrEmpty(_cachedKeyboardLanguage))
                {
                    return _cachedKeyboardLanguage;
                }

                // For now, we'll use the current input language
                var language = GetCurrentInputLanguage();
                
                // Update cache
                _cachedKeyboardLanguage = language;
                _lastCacheUpdate = DateTime.Now;
                
                _logger.LogDebug("Retrieved keyboard language: {Language}", language);
                
                return language;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving keyboard language");
                return "Unknown";
            }
        }

        private bool IsCacheValid()
        {
            return DateTime.Now - _lastCacheUpdate < _cacheExpiration;
        }

        private string GetKeyboardNameFromWMI()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "path Win32_Keyboard get Name,DeviceID,Description /format:csv",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogWarning("Failed to start wmic process");
                    return "Unknown Keyboard";
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("wmic command failed with exit code: {ExitCode}", process.ExitCode);
                    return "Unknown Keyboard";
                }

                _logger.LogDebug("WMI output: {Output}", output);

                // Clean up the output and parse it
                var cleanOutput = output.Replace("&amp;", "&");
                var lines = cleanOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Find the first valid keyboard entry
                string bestKeyboardName = "Standard Keyboard";
                bool foundUSBKeyboard = false;
                
                foreach (var line in lines.Skip(1)) // Skip header
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Split(',');
                    if (parts.Length >= 4)
                    {
                        var description = parts[1]?.Trim();
                        var deviceId = parts[2]?.Trim();
                        var name = parts[3]?.Trim();

                        _logger.LogDebug("Parsing keyboard entry - Description: {Description}, DeviceID: {DeviceID}, Name: {Name}",
                            description ?? "null", deviceId ?? "null", name ?? "null");

                        // Determine keyboard type based on device ID and description
                        if (!string.IsNullOrEmpty(deviceId))
                        {
                            if (deviceId.Contains("USB") && description?.Contains("USB") == true)
                            {
                                if (!foundUSBKeyboard)
                                {
                                    bestKeyboardName = DetermineKeyboardType(deviceId, description ?? string.Empty, name ?? string.Empty);
                                    foundUSBKeyboard = true;
                                }
                            }
                            else if (!foundUSBKeyboard && deviceId.Contains("HID"))
                            {
                                bestKeyboardName = DetermineKeyboardType(deviceId, description ?? string.Empty, name ?? string.Empty);
                            }
                        }
                    }
                }

                return CleanKeyboardName(bestKeyboardName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting keyboard name from WMI");
                return "Unknown Keyboard";
            }
        }

        private string DetermineKeyboardType(string deviceId, string description, string name)
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
                        return $"{manufacturer} Keyboard (VID: {vid.ToUpper()})";
                    }
                    else
                    {
                        return $"USB Keyboard (VID: {vid.ToUpper()})";
                    }
                }
            }

            // Use description to determine type
            if (!string.IsNullOrEmpty(description))
            {
                if (description.Contains("USB"))
                {
                    return "USB Keyboard";
                }
                else if (description.Contains("HID"))
                {
                    return "HID Keyboard";
                }
            }

            // Fallback to name if available
            if (!string.IsNullOrEmpty(name) && !name.Contains("Enhanced"))
            {
                return name;
            }

            return "Standard Keyboard";
        }

        private string? GetManufacturerFromVID(string? vid)
        {
            if (string.IsNullOrEmpty(vid))
                return null;
                
            return vid.ToUpper() switch
            {
                "0414" => "Logitech",
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
                _ => null
            };
        }

        private string CleanKeyboardName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Unknown Keyboard";

            // Remove quotes and extra whitespace
            name = name.Trim().Trim('"');
            
            // If the name is too technical, make it more user-friendly
            if (name.Contains("HID") || name.Contains("USB") || name.Contains("VID_") || name.Contains("PID_"))
            {
                if (name.ToLower().Contains("keyboard"))
                {
                    return "USB Keyboard";
                }
                return "External Keyboard";
            }

            // Ensure it ends with "Keyboard" if it doesn't already
            if (!name.ToLower().Contains("keyboard"))
            {
                name += " Keyboard";
            }

            return name;
        }

        private string GetCurrentInputLanguage()
        {
            try
            {
                var currentCulture = System.Globalization.CultureInfo.CurrentCulture;
                return currentCulture.TwoLetterISOLanguageName.ToUpper();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current input language");
                return "EN";
            }
        }
    }
}