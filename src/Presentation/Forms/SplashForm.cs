using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Presentation.Forms
{
    /// <summary>
    /// Splash screen form displayed during application initialization
    /// </summary>
    public partial class SplashForm : Form
    {
        private readonly UISettings _uiSettings;
        private readonly IThemeManager _themeManager;
        private readonly ILogoManager _logoManager;
        private readonly ILogger<SplashForm> _logger;

        // UI Controls
        private PictureBox _logoBox = null!;
        private Label _titleLabel = null!;
        private Label _versionLabel = null!;
        private Panel _progressPanel = null!;
        private Label _statusLabel = null!;

        // Progress tracking
        private int _currentProgress = 0;
        private string _currentStatus = "Initializing...";
        private bool _isLoadingApplication = false;
        private System.Windows.Forms.Timer _spinnerTimer = null!;
        private int _spinnerAngle = 0;

        public SplashForm(
            UISettings uiSettings,
            IThemeManager themeManager,
            ILogoManager logoManager,
            ILogger<SplashForm> logger)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            _logoManager = logoManager ?? throw new ArgumentNullException(nameof(logoManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponent();
            ApplyTheme();

            // Initialize spinner timer
            _spinnerTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _spinnerTimer.Tick += OnSpinnerTick;

            // Subscribe to theme changes
            _themeManager.ThemeChanged += OnThemeChanged;
        }

        /// <summary>
        /// Shows loading application state with spinner
        /// </summary>
        public void ShowLoadingApplication()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ShowLoadingApplication));
                return;
            }

            _isLoadingApplication = true;
            _currentStatus = "Loading application...";
            _statusLabel.Text = _currentStatus;
            _spinnerTimer.Start();
            _progressPanel.Invalidate();
        }

        /// <summary>
        /// Updates the progress bar and status text
        /// </summary>
        /// <param name="percentage">Progress percentage (0-100)</param>
        /// <param name="status">Status message</param>
        public void UpdateProgress(int percentage, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgress(percentage, status)));
                return;
            }

            try
            {
                _currentProgress = Math.Max(0, Math.Min(100, percentage));
                _currentStatus = status ?? "Initializing...";

                _statusLabel.Text = _currentStatus;
                _progressPanel.Invalidate(); // Trigger repaint of custom progress bar

                _logger.LogDebug("Splash screen progress updated: {Progress}% - {Status}", _currentProgress, _currentStatus);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating splash screen progress");
            }
        }

        private void InitializeComponent()
        {
            // Form properties
            Text = "SimBlock";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(400, 300);
            BackColor = _uiSettings.BackgroundColor;
            ShowInTaskbar = false;
            TopMost = true;

            // Create main layout panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                Padding = new Padding(30),
                BackColor = _uiSettings.BackgroundColor
            };

            // Set row styles for proper spacing
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F)); // Logo
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Title
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F)); // Version
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F)); // Progress bar
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F)); // Status

            // Logo
            _logoBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            try
            {
                // Use CreateLogoPictureBox and extract the image
                using var tempLogoBox = _logoManager.CreateLogoPictureBox();
                if (tempLogoBox.Image != null)
                {
                    _logoBox.Image = new Bitmap(tempLogoBox.Image);
                }
                _logger.LogDebug("Splash screen logo loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load logo for splash screen");
            }

            // Title label
            _titleLabel = new Label
            {
                Text = "SimBlock",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = _uiSettings.TextColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Version label
            _versionLabel = new Label
            {
                Text = $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = _uiSettings.InactiveColor,
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Custom progress panel (for modern flat progress bar)
            _progressPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Height = 20
            };
            _progressPanel.Paint += OnProgressPanelPaint;

            // Status label
            _statusLabel = new Label
            {
                Text = _currentStatus,
                Font = new Font("Segoe UI", 9F),
                ForeColor = _uiSettings.TextColor,
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Add controls to layout
            mainPanel.Controls.Add(_logoBox, 0, 0);
            mainPanel.Controls.Add(_titleLabel, 0, 1);
            mainPanel.Controls.Add(_versionLabel, 0, 2);
            mainPanel.Controls.Add(_progressPanel, 0, 3);
            mainPanel.Controls.Add(_statusLabel, 0, 4);

            Controls.Add(mainPanel);

            _logger.LogDebug("Splash screen components initialized");
        }

        private void OnSpinnerTick(object? sender, EventArgs e)
        {
            _spinnerAngle = (_spinnerAngle + 15) % 360;
            _progressPanel.Invalidate();
        }

        private void OnProgressPanelPaint(object? sender, PaintEventArgs e)
        {
            try
            {
                var g = e.Graphics;
                var rect = _progressPanel.ClientRectangle;

                if (_isLoadingApplication)
                {
                    // Draw spinner
                    var centerX = rect.Width / 2;
                    var centerY = rect.Height / 2;
                    var radius = 8;

                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    for (int i = 0; i < 8; i++)
                    {
                        var angle = (_spinnerAngle + i * 45) * Math.PI / 180;
                        var alpha = (int)(255 * (1.0 - i / 8.0));
                        var color = Color.FromArgb(alpha, _uiSettings.PrimaryButtonColor);

                        var x = centerX + (int)(Math.Cos(angle) * radius);
                        var y = centerY + (int)(Math.Sin(angle) * radius);

                        using (var brush = new SolidBrush(color))
                        {
                            g.FillEllipse(brush, x - 2, y - 2, 4, 4);
                        }
                    }
                }
                else
                {
                    // Draw progress bar
                    var progressRect = new Rectangle(
                        rect.X + 20,
                        rect.Y + (rect.Height - 8) / 2,
                        rect.Width - 40,
                        8
                    );

                    // Draw background (unfilled portion)
                    using (var backgroundBrush = new SolidBrush(_uiSettings.InactiveColor))
                    {
                        g.FillRectangle(backgroundBrush, progressRect);
                    }

                    // Calculate filled width
                    var filledWidth = (int)(progressRect.Width * (_currentProgress / 100.0));
                    if (filledWidth > 0)
                    {
                        var filledRect = new Rectangle(
                            progressRect.X,
                            progressRect.Y,
                            filledWidth,
                            progressRect.Height
                        );

                        // Draw filled portion with gradient for modern look
                        using (var fillBrush = new LinearGradientBrush(
                            filledRect,
                            _uiSettings.PrimaryButtonColor,
                            Color.FromArgb(Math.Min(255, _uiSettings.PrimaryButtonColor.R + 30),
                                         Math.Min(255, _uiSettings.PrimaryButtonColor.G + 30),
                                         Math.Min(255, _uiSettings.PrimaryButtonColor.B + 30)),
                            LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(fillBrush, filledRect);
                        }
                    }

                    // Draw border for clean look
                    using (var borderPen = new Pen(_uiSettings.TextColor, 1))
                    {
                        g.DrawRectangle(borderPen, progressRect);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error painting progress bar");
            }
        }

        private void OnThemeChanged(object? sender, Theme theme)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnThemeChanged(sender, theme)));
                return;
            }

            ApplyTheme();
        }

        private void ApplyTheme()
        {
            try
            {
                BackColor = _uiSettings.BackgroundColor;

                if (_titleLabel != null)
                    _titleLabel.ForeColor = _uiSettings.TextColor;

                if (_versionLabel != null)
                    _versionLabel.ForeColor = _uiSettings.InactiveColor;

                if (_statusLabel != null)
                    _statusLabel.ForeColor = _uiSettings.TextColor;

                // Update background color of all panels
                foreach (Control control in Controls)
                {
                    if (control is TableLayoutPanel panel)
                    {
                        panel.BackColor = _uiSettings.BackgroundColor;
                    }
                }

                // Repaint progress bar with new colors
                _progressPanel?.Invalidate();

                _logger.LogDebug("Splash screen theme applied: {Theme}", _uiSettings.CurrentTheme);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying theme to splash screen");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // Unsubscribe from events
                    if (_themeManager != null)
                    {
                        _themeManager.ThemeChanged -= OnThemeChanged;
                    }

                    // Stop and dispose spinner timer
                    _spinnerTimer?.Stop();
                    _spinnerTimer?.Dispose();

                    // Dispose logo image
                    _logoBox?.Image?.Dispose();

                    _logger.LogDebug("Splash screen disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during splash screen disposal");
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Shows the splash screen with a smooth appearance
        /// </summary>
        /// <returns>Task representing the show operation</returns>
        public async Task ShowSplashAsync()
        {
            try
            {
                if (InvokeRequired)
                {
                    await Task.Run(() => Invoke(new Action(() => ShowSplashAsync().Wait())));
                    return;
                }

                Show();
                BringToFront();
                Activate();

                _logger.LogInformation("Splash screen displayed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing splash screen");
            }
        }

        /// <summary>
        /// Closes the splash screen smoothly
        /// </summary>
        /// <returns>Task representing the close operation</returns>
        public async Task CloseSplashAsync()
        {
            try
            {
                if (InvokeRequired)
                {
                    await Task.Run(() => Invoke(new Action(() => CloseSplashAsync().Wait())));
                    return;
                }

                Hide();
                _logger.LogInformation("Splash screen closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing splash screen");
            }
        }
    }
}