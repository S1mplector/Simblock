using System;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Controls;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Presentation.Managers
{
    /// <summary>
    /// Manages blocking visualization displays for keyboard and mouse
    /// </summary>
    public class BlockingVisualizationManager : IBlockingVisualizationManager
    {
        private readonly UISettings _uiSettings;
        private readonly ILogger<BlockingVisualizationManager> _logger;
        private readonly KeyboardVisualizationControl _keyboardVisualization;
        private readonly MouseVisualizationControl _mouseVisualization;
        
        // Current state tracking
        private BlockingMode _keyboardMode = BlockingMode.Simple;
        private BlockingMode _mouseMode = BlockingMode.Simple;
        private AdvancedKeyboardConfiguration? _keyboardConfig;
        private AdvancedMouseConfiguration? _mouseConfig;
        private bool _keyboardBlocked = false;
        private bool _mouseBlocked = false;

        public event EventHandler<VisualizationUpdateEventArgs>? VisualizationUpdateRequested;

        public BlockingVisualizationManager(UISettings uiSettings, ILogger<BlockingVisualizationManager> logger)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize visualization controls
            _keyboardVisualization = new KeyboardVisualizationControl(_uiSettings);
            _mouseVisualization = new MouseVisualizationControl(_uiSettings);
            
            _logger.LogInformation("BlockingVisualizationManager initialized successfully");
        }

        /// <summary>
        /// Updates the keyboard visualization with current blocking state
        /// </summary>
        public void UpdateKeyboardVisualization(KeyboardBlockState state)
        {
            try
            {
                _keyboardBlocked = state.IsBlocked;
                
                // Update the visualization control
                _keyboardVisualization.UpdateVisualization(_keyboardMode, _keyboardConfig, _keyboardBlocked);
                
                // Log the update for debugging
                _logger.LogDebug("Updated keyboard visualization: Mode={Mode}, Blocked={Blocked}", 
                    _keyboardMode, _keyboardBlocked);
                
                // Notify listeners
                OnVisualizationUpdateRequested("Keyboard", "StateUpdate", state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating keyboard visualization");
            }
        }

        /// <summary>
        /// Updates the mouse visualization with current blocking state
        /// </summary>
        public void UpdateMouseVisualization(MouseBlockState state)
        {
            try
            {
                _mouseBlocked = state.IsBlocked;
                
                // Update the visualization control
                _mouseVisualization.UpdateVisualization(_mouseMode, _mouseConfig, _mouseBlocked);
                
                // Log the update for debugging
                _logger.LogDebug("Updated mouse visualization: Mode={Mode}, Blocked={Blocked}", 
                    _mouseMode, _mouseBlocked);
                
                // Notify listeners
                OnVisualizationUpdateRequested("Mouse", "StateUpdate", state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mouse visualization");
            }
        }

        /// <summary>
        /// Sets the blocking mode for keyboard visualization
        /// </summary>
        public void SetKeyboardBlockingMode(BlockingMode mode, AdvancedKeyboardConfiguration? config = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"BlockingVisualizationManager.SetKeyboardBlockingMode: Mode={mode}, Config={config != null}");
                
                _keyboardMode = mode;
                _keyboardConfig = config;
                
                // For Select mode, ensure we're not actually blocking
                if (mode == BlockingMode.Select)
                {
                    _keyboardBlocked = false;
                    _logger.LogInformation("Keyboard set to Select mode - blocking disabled for selection");
                    System.Diagnostics.Debug.WriteLine($"BlockingVisualizationManager.SetKeyboardBlockingMode: Select mode - Config null: {config == null}");
                }
                
                // Update the visualization with new mode
                _keyboardVisualization.UpdateVisualization(_keyboardMode, _keyboardConfig, _keyboardBlocked);
                System.Diagnostics.Debug.WriteLine($"BlockingVisualizationManager.SetKeyboardBlockingMode: Updated visualization with Mode={_keyboardMode}, Config={_keyboardConfig != null}, Blocked={_keyboardBlocked}");
                
                _logger.LogInformation("Keyboard blocking mode set to {Mode}", mode);
                
                // Notify listeners
                OnVisualizationUpdateRequested("Keyboard", "ModeChange", new { Mode = mode, Config = config });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting keyboard blocking mode");
            }
        }

        /// <summary>
        /// Sets the blocking mode for mouse visualization
        /// </summary>
        public void SetMouseBlockingMode(BlockingMode mode, AdvancedMouseConfiguration? config = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"BlockingVisualizationManager.SetMouseBlockingMode: Mode={mode}, Config={config != null}");
                
                _mouseMode = mode;
                _mouseConfig = config;
                
                // For Select mode, ensure we're not actually blocking
                if (mode == BlockingMode.Select)
                {
                    _mouseBlocked = false;
                    _logger.LogInformation("Mouse set to Select mode - blocking disabled for selection");
                    System.Diagnostics.Debug.WriteLine($"BlockingVisualizationManager.SetMouseBlockingMode: Select mode - Config null: {config == null}");
                }
                
                // Update the visualization with new mode
                _mouseVisualization.UpdateVisualization(_mouseMode, _mouseConfig, _mouseBlocked);
                System.Diagnostics.Debug.WriteLine($"BlockingVisualizationManager.SetMouseBlockingMode: Updated visualization with Mode={_mouseMode}, Config={_mouseConfig != null}, Blocked={_mouseBlocked}");
                
                _logger.LogInformation("Mouse blocking mode set to {Mode}", mode);
                
                // Notify listeners
                OnVisualizationUpdateRequested("Mouse", "ModeChange", new { Mode = mode, Config = config });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting mouse blocking mode");
            }
        }

        /// <summary>
        /// Gets the keyboard visualization control
        /// </summary>
        public Control GetKeyboardVisualizationControl()
        {
            return _keyboardVisualization;
        }

        /// <summary>
        /// Gets the mouse visualization control
        /// </summary>
        public Control GetMouseVisualizationControl()
        {
            return _mouseVisualization;
        }

        /// <summary>
        /// Gets a summary of the current keyboard blocking state
        /// </summary>
        public string GetKeyboardBlockingSummary()
        {
            try
            {
                return _keyboardVisualization.GetBlockingSummary();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting keyboard blocking summary");
                return "Summary unavailable";
            }
        }

        /// <summary>
        /// Gets a summary of the current mouse blocking state
        /// </summary>
        public string GetMouseBlockingSummary()
        {
            try
            {
                return _mouseVisualization.GetBlockingSummary();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mouse blocking summary");
                return "Summary unavailable";
            }
        }

        /// <summary>
        /// Gets the current keyboard blocking mode
        /// </summary>
        public BlockingMode GetKeyboardBlockingMode()
        {
            return _keyboardMode;
        }

        /// <summary>
        /// Gets the current mouse blocking mode
        /// </summary>
        public BlockingMode GetMouseBlockingMode()
        {
            return _mouseMode;
        }

        /// <summary>
        /// Checks if keyboard is currently blocked
        /// </summary>
        public bool IsKeyboardBlocked()
        {
            return _keyboardBlocked;
        }

        /// <summary>
        /// Checks if mouse is currently blocked
        /// </summary>
        public bool IsMouseBlocked()
        {
            return _mouseBlocked;
        }

        /// <summary>
        /// Updates both visualizations with current UI settings (e.g., theme changes)
        /// </summary>
        public void RefreshVisualizationsFromUISettings()
        {
            try
            {
                // Force refresh of both controls to pick up any UI setting changes
                _keyboardVisualization.SetKeyboardLayout(_uiSettings.CurrentKeyboardLayout);
                _keyboardVisualization.UpdateVisualization(_keyboardMode, _keyboardConfig, _keyboardBlocked);
                _mouseVisualization.UpdateVisualization(_mouseMode, _mouseConfig, _mouseBlocked);
                
                _logger.LogDebug("Refreshed visualizations from UI settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing visualizations from UI settings");
            }
        }

        /// <summary>
        /// Creates a container panel that holds both visualization controls
        /// </summary>
        public Panel CreateVisualizationPanel()
        {
            try
            {
                var panel = new Panel
                {
                    Size = new System.Drawing.Size(850, 240),
                    BackColor = _uiSettings.BackgroundColor,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(0, 0, 0, 20) // Add bottom margin for spacing
                };
                
                // Position keyboard visualization on the left with more spacing
                _keyboardVisualization.Location = new System.Drawing.Point(20, 20);
                _keyboardVisualization.Size = new System.Drawing.Size(620, 200);
                panel.Controls.Add(_keyboardVisualization);
                
                // Position mouse visualization on the right with more spacing
                _mouseVisualization.Location = new System.Drawing.Point(650, 20);
                _mouseVisualization.Size = new System.Drawing.Size(180, 200);
                panel.Controls.Add(_mouseVisualization);
                
                return panel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating visualization panel");
                return new Panel(); // Return empty panel as fallback
            }
        }

        /// <summary>
        /// Handles UI theme changes
        /// </summary>
        public void OnThemeChanged(Theme newTheme)
        {
            try
            {
                _logger.LogInformation("Applying theme change to visualizations: {Theme}", newTheme);
                
                // Refresh visualizations to pick up new theme colors
                RefreshVisualizationsFromUISettings();
                
                // Notify listeners
                OnVisualizationUpdateRequested("Both", "ThemeChange", newTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling theme change in visualizations");
            }
        }

        private void OnVisualizationUpdateRequested(string deviceType, string updateType, object? data)
        {
            try
            {
                VisualizationUpdateRequested?.Invoke(this, new VisualizationUpdateEventArgs
                {
                    DeviceType = deviceType,
                    UpdateType = updateType,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising visualization update event");
            }
        }

        /// <summary>
        /// Disposes resources used by the visualization manager
        /// </summary>
        public void Dispose()
        {
            try
            {
                _keyboardVisualization?.Dispose();
                _mouseVisualization?.Dispose();
                
                _logger.LogInformation("BlockingVisualizationManager disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing BlockingVisualizationManager");
            }
        }
    }
}