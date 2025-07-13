using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Presentation.ViewModels;
using SimBlock.Infrastructure.Windows;

namespace SimBlock.Presentation.Forms
{
    /// <summary>
    /// Main application window
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly IKeyboardBlockerService _keyboardBlockerService;
        private readonly ILogger<MainForm> _logger;
        private readonly MainWindowViewModel _viewModel;
        private readonly ResourceMonitor _resourceMonitor;

        // UI Controls
        private Button _toggleButton = null!;
        private Label _statusLabel = null!;
        private PictureBox _logoIcon = null!;
        private Label _lastToggleLabel = null!;
        private Button _hideToTrayButton = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _timeLabel = null!;
        private ToolStripStatusLabel _blockingDurationLabel = null!;
        private ToolStripStatusLabel _sessionInfoLabel = null!;
        private ToolStripStatusLabel _hookStatusLabel = null!;
        private ToolStripStatusLabel _resourceUsageLabel = null!;

        // Status bar tracking
        private System.Windows.Forms.Timer _statusTimer = null!;
        private DateTime? _blockingStartTime = null;
        private int _todayBlockCount = 0;
        private DateTime _lastResetDate = DateTime.Today;

        public MainForm(IKeyboardBlockerService keyboardBlockerService, ILogger<MainForm> logger)
        {
            _keyboardBlockerService = keyboardBlockerService ?? throw new ArgumentNullException(nameof(keyboardBlockerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = new MainWindowViewModel();
            _resourceMonitor = new ResourceMonitor();

            InitializeComponent();
            InitializeEventHandlers();
            UpdateUI();
            
            // Ensure window is visible and focused on startup
            this.Load += (s, e) => 
            {
                this.BringToFront();
                this.Activate();
                this.Focus();
            };
        }

        private void InitializeComponent()
        {
            // Form properties
            Text = "SimBlock - Keyboard Blocker";
            Size = new System.Drawing.Size(550, 300);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            Visible = true;
            TopMost = false;
            Icon = CreateApplicationIcon();
            KeyPreview = true; // Enable keyboard shortcuts

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6, // Increased to accommodate logo
                Padding = new Padding(20)
            };

            // Status label
            _statusLabel = new Label
            {
                Text = "Keyboard is unlocked",
                Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = System.Drawing.Color.Green
            };

            // Logo icon
            _logoIcon = new PictureBox
            {
                Size = new System.Drawing.Size(48, 48),
                Anchor = AnchorStyles.None,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = CreateLogoImage()
            };

            // Toggle button
            _toggleButton = new Button
            {
                Text = "Block Keyboard",
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                Size = new System.Drawing.Size(200, 50),
                Anchor = AnchorStyles.None,
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _toggleButton.FlatAppearance.BorderSize = 0;

            // Last toggle time label
            _lastToggleLabel = new Label
            {
                Text = "Last toggle: Never",
                Font = new System.Drawing.Font("Segoe UI", 9),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = System.Drawing.Color.Gray
            };

            // Hide to tray button
            _hideToTrayButton = new Button
            {
                Text = "Hide to Tray",
                Font = new System.Drawing.Font("Segoe UI", 10),
                Size = new System.Drawing.Size(120, 30),
                Anchor = AnchorStyles.None,
                BackColor = System.Drawing.Color.Gray,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _hideToTrayButton.FlatAppearance.BorderSize = 0;

            // Instructions label
            var instructionsLabel = new Label
            {
                Text = "Space: Toggle • Esc: Hide • F1: Help • Emergency: Ctrl+Alt+U (3x)",
                Font = new System.Drawing.Font("Segoe UI", 8),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = System.Drawing.Color.DarkGray
            };

            // Add controls to panel
            mainPanel.Controls.Add(_statusLabel, 0, 0);
            mainPanel.Controls.Add(_logoIcon, 0, 1);
            mainPanel.Controls.Add(_toggleButton, 0, 2);
            mainPanel.Controls.Add(_lastToggleLabel, 0, 3);
            mainPanel.Controls.Add(_hideToTrayButton, 0, 4);
            mainPanel.Controls.Add(instructionsLabel, 0, 5);

            // Set row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); // Status text
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15)); // Logo
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); // Toggle button
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10)); // Last toggle
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15)); // Hide button
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10)); // Instructions

            Controls.Add(mainPanel);
            
            // Initialize status bar
            InitializeStatusBar();
        }

        private void InitializeStatusBar()
        {
            // Create status strip
            _statusStrip = new StatusStrip
            {
                BackColor = System.Drawing.Color.FromArgb(240, 240, 240),
                Font = new System.Drawing.Font("Segoe UI", 8.25F),
                SizingGrip = false
            };

            // Current time label
            _timeLabel = new ToolStripStatusLabel
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched,
                AutoSize = false,
                Width = 60,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                ToolTipText = "Current system time"
            };

            // Blocking duration label
            _blockingDurationLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched,
                AutoSize = false,
                Width = 100,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                ToolTipText = "Duration of current blocking session"
            };

            // Session info label
            _sessionInfoLabel = new ToolStripStatusLabel
            {
                Text = "Blocks today: 0",
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched,
                AutoSize = false,
                Width = 100,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                ToolTipText = "Number of times keyboard was blocked today"
            };

            // Hook status label
            _hookStatusLabel = new ToolStripStatusLabel
            {
                Text = "Hook: Active",
                ForeColor = System.Drawing.Color.Green,
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched,
                AutoSize = false,
                Width = 80,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                ToolTipText = "Keyboard hook service status"
            };

            // Resource usage label
            _resourceUsageLabel = new ToolStripStatusLabel
            {
                Text = "CPU: 0% | RAM: 0MB",
                ForeColor = System.Drawing.Color.Blue,
                Spring = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                ToolTipText = "SimBlock application resource usage (CPU and RAM)"
            };

            // Add labels to status strip
            _statusStrip.Items.AddRange(new ToolStripItem[] { 
                _timeLabel, 
                _blockingDurationLabel, 
                _sessionInfoLabel, 
                _hookStatusLabel,
                _resourceUsageLabel 
            });

            // Add status strip to form
            Controls.Add(_statusStrip);

            // Initialize timer for real-time updates
            _statusTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000, // Update every second
                Enabled = true
            };
            _statusTimer.Tick += OnStatusTimerTick;
        }

        private void InitializeEventHandlers()
        {
            _toggleButton.Click += OnToggleButtonClick;
            _hideToTrayButton.Click += OnHideToTrayButtonClick;
            _keyboardBlockerService.StateChanged += OnKeyboardStateChanged;

            // Handle form closing
            FormClosing += OnFormClosing;
            
            // Handle keyboard shortcuts
            KeyDown += OnKeyDown;
        }

        private async void OnToggleButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Toggle button clicked");
                
                // Disable button during operation to prevent double-clicks
                _toggleButton.Enabled = false;
                _toggleButton.Text = "Processing...";
                
                await _keyboardBlockerService.ToggleBlockingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling keyboard blocking");
                MessageBox.Show($"Failed to toggle keyboard blocking.\n\nError: {ex.Message}", 
                    "SimBlock Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable button and update UI
                _toggleButton.Enabled = true;
                UpdateUI();
            }
        }

        private async void OnHideToTrayButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Hide to tray button clicked");
                await _keyboardBlockerService.HideToTrayAsync();
                Hide();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hiding to tray");
            }
        }

        private void OnKeyboardStateChanged(object? sender, KeyboardBlockState state)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnKeyboardStateChanged(sender, state)));
                return;
            }

            _viewModel.UpdateFromState(state);
            UpdateUI();
            UpdateStatusBar(state);
        }

        private void OnStatusTimerTick(object? sender, EventArgs e)
        {
            // Update current time
            _timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");

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
                _blockingDurationLabel.ForeColor = System.Drawing.Color.Red;
            }

            // Update hook status periodically
            UpdateHookStatus();
            
            // Update resource usage
            UpdateResourceUsage();
        }

        private void UpdateHookStatus()
        {
            try
            {
                // Check if the keyboard service is working properly
                var currentState = _keyboardBlockerService.CurrentState;
                if (currentState != null)
                {
                    _hookStatusLabel.Text = "Hook: Active";
                    _hookStatusLabel.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    _hookStatusLabel.Text = "Hook: Inactive";
                    _hookStatusLabel.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                _hookStatusLabel.Text = "Hook: Error";
                _hookStatusLabel.ForeColor = System.Drawing.Color.Red;
                _logger.LogWarning(ex, "Error checking hook status");
            }
        }

        private void UpdateResourceUsage()
        {
            try
            {
                var resourceString = _resourceMonitor.GetCompactResourceString();
                _resourceUsageLabel.Text = resourceString;
                
                // Change color based on resource usage
                var cpuUsage = _resourceMonitor.GetCpuUsage();
                var appMemoryUsage = _resourceMonitor.GetTaskManagerMemoryUsage();
                
                if (cpuUsage > 5 || appMemoryUsage > 80)
                {
                    _resourceUsageLabel.ForeColor = System.Drawing.Color.Red;
                }
                else if (cpuUsage > 2 || appMemoryUsage > 50)
                {
                    _resourceUsageLabel.ForeColor = System.Drawing.Color.Orange;
                }
                else
                {
                    _resourceUsageLabel.ForeColor = System.Drawing.Color.Blue;
                }
            }
            catch (Exception ex)
            {
                _resourceUsageLabel.Text = "Resource info unavailable";
                _resourceUsageLabel.ForeColor = System.Drawing.Color.Gray;
                _logger.LogWarning(ex, "Error updating resource usage");
            }
        }

        private void UpdateStatusBar(KeyboardBlockState state)
        {
            try
            {
                // Update blocking duration
                if (state.IsBlocked && !_blockingStartTime.HasValue)
                {
                    // Just started blocking
                    _blockingStartTime = DateTime.Now;
                    _todayBlockCount++;
                    _blockingDurationLabel.Text = "Blocked: 00:00";
                    _blockingDurationLabel.ForeColor = System.Drawing.Color.Red;
                }
                else if (!state.IsBlocked && _blockingStartTime.HasValue)
                {
                    // Just stopped blocking
                    _blockingStartTime = null;
                    _blockingDurationLabel.Text = "Ready";
                    _blockingDurationLabel.ForeColor = System.Drawing.Color.Green;
                }

                // Update session info
                _sessionInfoLabel.Text = $"Blocks today: {_todayBlockCount}";

                // Update hook status based on service state
                _hookStatusLabel.Text = "Hook: Active";
                _hookStatusLabel.ForeColor = System.Drawing.Color.Green;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status bar");
            }
        }

        private void UpdateUI()
        {
            _statusLabel.Text = _viewModel.StatusText;
            _statusLabel.ForeColor = _viewModel.IsKeyboardBlocked ? 
                System.Drawing.Color.Red : System.Drawing.Color.Green;

            _toggleButton.Text = _viewModel.ToggleButtonText;
            
            // Only update button color if it's enabled (not processing)
            if (_toggleButton.Enabled)
            {
                _toggleButton.BackColor = _viewModel.IsKeyboardBlocked ? 
                    System.Drawing.Color.FromArgb(215, 0, 0) : System.Drawing.Color.FromArgb(0, 120, 215);
            }

            _lastToggleLabel.Text = $"Last toggle: {_viewModel.LastToggleTime:HH:mm:ss}";
            
            // Update logo based on keyboard state
            UpdateLogoAppearance();
        }

        private void UpdateLogoAppearance()
        {
            if (_logoIcon?.Image == null) return;

            try
            {
                // Create a new image based on the current state
                var originalImage = CreateLogoImage();
                if (originalImage == null) return;

                if (_viewModel.IsKeyboardBlocked)
                {
                    // Create a dimmed and slightly red-tinted version for blocked state
                    _logoIcon.Image = CreateBlockedStateImage(originalImage);
                }
                else
                {
                    // Use the original image for unlocked state
                    _logoIcon.Image = originalImage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating logo appearance");
            }
        }

        private System.Drawing.Image? CreateLogoImage()
        {
            try
            {
                // Try to load the logo.ico file from embedded resources
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "SimBlock.src.Presentation.Resources.Images.logo.ico";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var icon = new System.Drawing.Icon(stream);
                    return icon.ToBitmap();
                }
                else
                {
                    // Fallback to file system if embedded resource not found
                    string iconPath = Path.Combine(Application.StartupPath, "src", "Presentation", "Resources", "Images", "logo.ico");
                    
                    if (File.Exists(iconPath))
                    {
                        using var icon = new System.Drawing.Icon(iconPath);
                        return icon.ToBitmap();
                    }
                    else
                    {
                        // Final fallback to the original programmatic icon
                        return CreateFallbackLogoImage();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load logo image, using fallback");
                return CreateFallbackLogoImage();
            }
        }

        private System.Drawing.Image CreateFallbackLogoImage()
        {
            var bitmap = new System.Drawing.Bitmap(48, 48);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillEllipse(System.Drawing.Brushes.Blue, 6, 6, 36, 36);
                g.DrawString("K", new System.Drawing.Font("Arial", 24, System.Drawing.FontStyle.Bold), 
                    System.Drawing.Brushes.White, 12, 8);
            }
            return bitmap;
        }

        private System.Drawing.Image CreateBlockedStateImage(System.Drawing.Image originalImage)
        {
            var bitmap = new System.Drawing.Bitmap(originalImage.Width, originalImage.Height);
            
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                // Draw the original image with reduced opacity (dimmed)
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                
                // Dim the image and add red tint
                colorMatrix.Matrix00 = 0.8f; // Red
                colorMatrix.Matrix11 = 0.4f; // Green (reduced)
                colorMatrix.Matrix22 = 0.4f; // Blue (reduced)
                colorMatrix.Matrix33 = 0.6f; // Alpha (transparency for dimming)
                colorMatrix.Matrix44 = 1.0f;
                
                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                
                g.DrawImage(originalImage, 
                    new System.Drawing.Rectangle(0, 0, originalImage.Width, originalImage.Height),
                    0, 0, originalImage.Width, originalImage.Height, 
                    System.Drawing.GraphicsUnit.Pixel, attributes);
            }
            
            return bitmap;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Space bar to toggle
            if (e.KeyCode == Keys.Space)
            {
                e.Handled = true;
                OnToggleButtonClick(sender, EventArgs.Empty);
            }
            // Escape to hide to tray
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                OnHideToTrayButtonClick(sender, EventArgs.Empty);
            }
            // F1 for help/about
            else if (e.KeyCode == Keys.F1)
            {
                e.Handled = true;
                ShowHelp();
            }
        }

        private void ShowHelp()
        {
            string helpText = @"SimBlock - Keyboard Blocker

Keyboard Shortcuts:
• Space - Toggle keyboard blocking
• Escape - Hide to system tray
• F1 - Show this help

Emergency Unlock:
• Ctrl+Alt+U (3 times) - Emergency unlock (works even when blocked)
• Must be pressed 3 times within 2 seconds

Status Bar Information:
• Current time and blocking duration
• Daily blocking session count
• Keyboard hook service status
• SimBlock application resource usage (CPU and RAM)

Tips:
• The application minimizes to system tray when closed
• Right-click the tray icon for quick access
• The tray icon shows current blocking status
• Emergency unlock requires 3 consecutive presses for safety
• Resource usage colors: Blue (normal), Orange (moderate), Red (high)
• Memory usage matches Task Manager's ""Memory"" column exactly";

            MessageBox.Show(helpText, "SimBlock Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            // Don't actually close, just hide to tray
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                // Actually closing - dispose resources
                _statusTimer?.Stop();
                _statusTimer?.Dispose();
                _logoIcon?.Image?.Dispose();
                _logoIcon?.Dispose();
                _resourceMonitor?.Dispose();
            }
        }

        private System.Drawing.Icon CreateApplicationIcon()
        {
            try
            {
                // Try to load the logo.ico file from embedded resources
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "SimBlock.src.Presentation.Resources.Images.logo.ico";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return new System.Drawing.Icon(stream);
                }
                else
                {
                    // Fallback to file system if embedded resource not found
                    string iconPath = Path.Combine(Application.StartupPath, "src", "Presentation", "Resources", "Images", "logo.ico");
                    
                    if (File.Exists(iconPath))
                    {
                        return new System.Drawing.Icon(iconPath);
                    }
                    else
                    {
                        // Final fallback to the original programmatic icon
                        return CreateFallbackIcon();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load logo.ico, using fallback icon");
                return CreateFallbackIcon();
            }
        }

        private System.Drawing.Icon CreateFallbackIcon()
        {
            using var bitmap = new System.Drawing.Bitmap(32, 32);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                g.FillEllipse(System.Drawing.Brushes.Blue, 4, 4, 24, 24);
                g.DrawString("K", new System.Drawing.Font("Arial", 16, System.Drawing.FontStyle.Bold), 
                    System.Drawing.Brushes.White, 8, 6);
            }
            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }
    }
}
