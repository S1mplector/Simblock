using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Presentation.Managers
{
    /// <summary>
    /// Manages the status bar UI components and updates
    /// </summary>
    public class StatusBarManager : IStatusBarManager
    {
        private readonly UISettings _uiSettings;
        private readonly ILogger<StatusBarManager> _logger;

        // Status bar components
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _timeLabel = null!;
        private ToolStripStatusLabel _blockingDurationLabel = null!;
        private ToolStripStatusLabel _sessionInfoLabel = null!;
        private ToolStripStatusLabel _hookStatusLabel = null!;
        private ToolStripStatusLabel _resourceUsageLabel = null!;

        // State tracking
        private DateTime? _blockingStartTime = null;
        private int _todayBlockCount = 0;
        private DateTime _lastResetDate = DateTime.Today;

        public StatusBarManager(UISettings uiSettings, ILogger<StatusBarManager> logger)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes the status bar and adds it to the parent form
        /// </summary>
        public StatusStrip Initialize(Form parentForm)
        {
            // Create status strip
            _statusStrip = new StatusStrip
            {
                BackColor = _uiSettings.StatusBarBackColor,
                Font = new Font("Segoe UI", 8.25F),
                SizingGrip = false
            };

            CreateStatusLabels();
            parentForm.Controls.Add(_statusStrip);

            return _statusStrip;
        }

        private void CreateStatusLabels()
        {
            // Current time label
            _timeLabel = new ToolStripStatusLabel
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched,
                AutoSize = false,
                Width = _uiSettings.TimeColumnWidth,
                TextAlign = ContentAlignment.MiddleCenter,
                ToolTipText = "Current system time"
            };

            // Blocking duration label
            _blockingDurationLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched,
                AutoSize = false,
                Width = _uiSettings.DurationColumnWidth,
                TextAlign = ContentAlignment.MiddleCenter,
                ToolTipText = "Duration of current blocking session"
            };

            // Session info label
            _sessionInfoLabel = new ToolStripStatusLabel
            {
                Text = "Blocks today: 0",
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched,
                AutoSize = false,
                Width = _uiSettings.SessionColumnWidth,
                TextAlign = ContentAlignment.MiddleCenter,
                ToolTipText = "Number of times keyboard was blocked today"
            };

            // Hook status label
            _hookStatusLabel = new ToolStripStatusLabel
            {
                Text = "Hook: Active",
                ForeColor = _uiSettings.SuccessColor,
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched,
                AutoSize = false,
                Width = _uiSettings.HookStatusColumnWidth,
                TextAlign = ContentAlignment.MiddleCenter,
                ToolTipText = "Keyboard hook service status"
            };

            // Resource usage label
            _resourceUsageLabel = new ToolStripStatusLabel
            {
                Text = "CPU: 0% | RAM: 0MB",
                ForeColor = _uiSettings.NormalColor,
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight,
                ToolTipText = "SimBlock application resource usage (CPU and RAM)"
            };

            // Add all labels to status strip
            _statusStrip.Items.AddRange(new ToolStripItem[] { 
                _timeLabel, 
                _blockingDurationLabel, 
                _sessionInfoLabel, 
                _hookStatusLabel,
                _resourceUsageLabel 
            });
        }

        /// <summary>
        /// Updates the current time display
        /// </summary>
        public void UpdateTime()
        {
            _timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        /// <summary>
        /// Updates the blocking duration display
        /// </summary>
        public void UpdateBlockingDuration()
        {
            // Reset daily counter if new day
            if (DateTime.Today != _lastResetDate)
            {
                _todayBlockCount = 0;
                _lastResetDate = DateTime.Today;
                _sessionInfoLabel.Text = "Blocks today: 0";
            }

            // Update blocking duration if currently blocked
            if (_blockingStartTime.HasValue)
            {
                var duration = DateTime.Now - _blockingStartTime.Value;
                _blockingDurationLabel.Text = $"Blocked: {duration:mm\\:ss}";
                _blockingDurationLabel.ForeColor = _uiSettings.ErrorColor;
            }
        }

        /// <summary>
        /// Updates the hook status display
        /// </summary>
        public void UpdateHookStatus(bool isActive)
        {
            try
            {
                if (isActive)
                {
                    _hookStatusLabel.Text = "Hook: Active";
                    _hookStatusLabel.ForeColor = _uiSettings.SuccessColor;
                }
                else
                {
                    _hookStatusLabel.Text = "Hook: Inactive";
                    _hookStatusLabel.ForeColor = _uiSettings.ErrorColor;
                }
            }
            catch (Exception ex)
            {
                _hookStatusLabel.Text = "Hook: Error";
                _hookStatusLabel.ForeColor = _uiSettings.ErrorColor;
                _logger.LogWarning(ex, "Error updating hook status");
            }
        }

        /// <summary>
        /// Updates the resource usage display
        /// </summary>
        public void UpdateResourceUsage(string resourceText, float cpuUsage, long memoryUsage)
        {
            try
            {
                _resourceUsageLabel.Text = resourceText;
                
                // Change color based on resource usage
                if (cpuUsage > _uiSettings.CpuErrorThreshold || memoryUsage > _uiSettings.MemoryErrorThreshold)
                {
                    _resourceUsageLabel.ForeColor = _uiSettings.ErrorColor;
                }
                else if (cpuUsage > _uiSettings.CpuWarningThreshold || memoryUsage > _uiSettings.MemoryWarningThreshold)
                {
                    _resourceUsageLabel.ForeColor = _uiSettings.WarningColor;
                }
                else
                {
                    _resourceUsageLabel.ForeColor = _uiSettings.NormalColor;
                }
            }
            catch (Exception ex)
            {
                _resourceUsageLabel.Text = "Resource info unavailable";
                _resourceUsageLabel.ForeColor = _uiSettings.InactiveColor;
                _logger.LogWarning(ex, "Error updating resource usage");
            }
        }

        /// <summary>
        /// Updates status bar when keyboard blocking state changes
        /// </summary>
        public void UpdateBlockingState(bool isBlocked)
        {
            try
            {
                if (isBlocked && !_blockingStartTime.HasValue)
                {
                    // Just started blocking
                    _blockingStartTime = DateTime.Now;
                    _todayBlockCount++;
                    _blockingDurationLabel.Text = "Blocked: 00:00";
                    _blockingDurationLabel.ForeColor = _uiSettings.ErrorColor;
                }
                else if (!isBlocked && _blockingStartTime.HasValue)
                {
                    // Just stopped blocking
                    _blockingStartTime = null;
                    _blockingDurationLabel.Text = "Ready";
                    _blockingDurationLabel.ForeColor = _uiSettings.SuccessColor;
                }

                // Update session info
                _sessionInfoLabel.Text = $"Blocks today: {_todayBlockCount}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blocking state in status bar");
            }
        }

        /// <summary>
        /// Gets the current blocking start time
        /// </summary>
        public DateTime? GetBlockingStartTime() => _blockingStartTime;

        /// <summary>
        /// Gets the current block count for today
        /// </summary>
        public int GetTodayBlockCount() => _todayBlockCount;
    }
}
