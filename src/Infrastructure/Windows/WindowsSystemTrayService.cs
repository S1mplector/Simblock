using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Domain.Interfaces;

namespace SimBlock.Infrastructure.Windows
{
    /// <summary>
    /// Windows-specific implementation of system tray service
    /// </summary>
    public class WindowsSystemTrayService : ISystemTrayService, IDisposable
    {
        private readonly ILogger<WindowsSystemTrayService> _logger;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private bool _disposed = false;

        public event EventHandler? TrayIconClicked;
        public event EventHandler? ShowWindowRequested;
        public event EventHandler? ExitRequested;
        public event EventHandler? ToggleMouseBlockRequested;
        public event EventHandler? OpenSettingsRequested;

        public bool IsVisible => _notifyIcon?.Visible ?? false;

        public WindowsSystemTrayService(ILogger<WindowsSystemTrayService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _logger.LogInformation("Initializing system tray icon...");
            EnsureContextMenu();

            // Create notify icon
            _notifyIcon = new NotifyIcon
            {
                ContextMenuStrip = _contextMenu,
                Text = "SimBlock - Keyboard Blocker"
            };

            // Ensure a valid icon is set immediately to avoid shell issues
            try
            {
                _notifyIcon.Icon = CreateIcon(isBlocked: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set initial tray icon");
            }

            _notifyIcon.MouseClick += OnTrayIconClick;

            _logger.LogInformation("System tray icon initialized");
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            var showWindowItem = new ToolStripMenuItem("Show Window");
            showWindowItem.Click += (s, e) => ShowWindowRequested?.Invoke(this, EventArgs.Empty);

            var toggleKeyboardItem = new ToolStripMenuItem("Toggle Keyboard Block");
            toggleKeyboardItem.Click += (s, e) => TrayIconClicked?.Invoke(this, EventArgs.Empty);

            var toggleMouseItem = new ToolStripMenuItem("Toggle Mouse Block");
            toggleMouseItem.Click += (s, e) => ToggleMouseBlockRequested?.Invoke(this, EventArgs.Empty);

            var settingsItem = new ToolStripMenuItem("Open Settings");
            settingsItem.Click += (s, e) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);

            menu.Items.AddRange(new ToolStripItem[] {
                showWindowItem,
                new ToolStripSeparator(),
                toggleKeyboardItem,
                toggleMouseItem,
                new ToolStripSeparator(),
                settingsItem,
                new ToolStripSeparator(),
                exitItem
            });

            return menu;
        }

        private void EnsureContextMenu()
        {
            if (_contextMenu == null || _contextMenu.IsDisposed)
            {
                _contextMenu = BuildContextMenu();
                if (_notifyIcon != null)
                {
                    _notifyIcon.ContextMenuStrip = _contextMenu;
                }
            }
        }

        public void Show()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                _logger.LogInformation("System tray icon shown");
            }
        }

        public void Hide()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _logger.LogInformation("System tray icon hidden");
            }
        }

        public void UpdateIcon(bool isBlocked)
        {
            if (_notifyIcon != null)
            {
                // Create a simple icon based on blocking state
                // In a real implementation, you'd load actual icon files
                _notifyIcon.Icon = CreateIcon(isBlocked);
                _logger.LogDebug("System tray icon updated: {IsBlocked}", isBlocked);
            }
        }

        public void UpdateTooltip(string tooltip)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = tooltip;
                _logger.LogDebug("System tray tooltip updated: {Tooltip}", tooltip);
            }
        }

        public void ShowNotification(string title, string message)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
                _logger.LogInformation("Notification shown: {Title} - {Message}", title, message);
            }
        }

        private void OnTrayIconClick(object? sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    TrayIconClicked?.Invoke(this, EventArgs.Empty);
                }
                else if (e.Button == MouseButtons.Right)
                {
                    // Ensure the context menu exists and is assigned
                    EnsureContextMenu();
                    if (_notifyIcon != null && _notifyIcon.ContextMenuStrip == null)
                    {
                        _notifyIcon.ContextMenuStrip = _contextMenu;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling tray icon click");
            }
        }

        private System.Drawing.Icon CreateIcon(bool isBlocked)
        {
            try
            {
                // Try to load the base logo first
                var baseIcon = LoadLogoIcon();
                if (baseIcon != null)
                {
                    // Create a modified version based on blocking state
                    return CreateIconFromBase(baseIcon, isBlocked);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load logo for tray icon, using fallback");
            }

            // Fallback to the original programmatic icon
            return CreateFallbackIcon(isBlocked);
        }

        private System.Drawing.Icon? LoadLogoIcon()
        {
            try
            {
                // Try to load from embedded resources first
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "SimBlock.src.Presentation.Resources.Images.logo.ico";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return new System.Drawing.Icon(stream);
                }

                // Fallback to file system
                string iconPath = Path.Combine(Application.StartupPath, "src", "Presentation", "Resources", "Images", "logo.ico");
                if (File.Exists(iconPath))
                {
                    return new System.Drawing.Icon(iconPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load logo icon");
            }

            return null;
        }

        private System.Drawing.Icon CreateIconFromBase(System.Drawing.Icon baseIcon, bool isBlocked)
        {
            // Create a bitmap from the base icon
            using var baseBitmap = baseIcon.ToBitmap();
            var bitmap = new System.Drawing.Bitmap(16, 16);
            
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                // Draw the base icon scaled to 16x16
                g.DrawImage(baseBitmap, 0, 0, 16, 16);
                
                // Add a small status indicator in the bottom-right corner
                var statusColor = isBlocked ? System.Drawing.Color.Red : System.Drawing.Color.Green;
                using var statusBrush = new System.Drawing.SolidBrush(statusColor);
                using var borderPen = new System.Drawing.Pen(System.Drawing.Color.White, 1);
                
                // Draw a small circle in the bottom-right corner
                g.FillEllipse(statusBrush, 10, 10, 6, 6);
                g.DrawEllipse(borderPen, 10, 10, 6, 6);
            }

            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }

        private System.Drawing.Icon CreateFallbackIcon(bool isBlocked)
        {
            using var bitmap = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                // Fill with red if blocked, green if not
                using var brush = new System.Drawing.SolidBrush(isBlocked ? 
                    System.Drawing.Color.Red : System.Drawing.Color.Green);

                g.FillEllipse(brush, 2, 2, 12, 12);

                // Add a border
                g.DrawEllipse(System.Drawing.Pens.Black, 2, 2, 12, 12);
            }

            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_notifyIcon != null)
                {
                    try
                    {
                        _notifyIcon.MouseClick -= OnTrayIconClick;
                        // Prevent NotifyIcon from touching a disposed ContextMenuStrip
                        _notifyIcon.Visible = false;
                        _notifyIcon.ContextMenuStrip = null;
                    }
                    catch { }
                    _notifyIcon.Dispose();
                }
                try
                {
                    _contextMenu?.Dispose();
                }
                catch { }
                _disposed = true;
                _logger.LogInformation("System tray service disposed");
            }
        }
    }
}
