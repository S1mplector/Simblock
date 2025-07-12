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
        public event EventHandler? ExitRequested;

        public bool IsVisible => _notifyIcon?.Visible ?? false;

        public WindowsSystemTrayService(ILogger<WindowsSystemTrayService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _logger.LogInformation("Initializing system tray icon...");

            // Create context menu
            _contextMenu = new ContextMenuStrip();
            
            var toggleItem = new ToolStripMenuItem("Toggle Keyboard Block");
            toggleItem.Click += (s, e) => TrayIconClicked?.Invoke(this, EventArgs.Empty);
            
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            
            _contextMenu.Items.AddRange(new ToolStripItem[] { toggleItem, new ToolStripSeparator(), exitItem });

            // Create notify icon
            _notifyIcon = new NotifyIcon
            {
                ContextMenuStrip = _contextMenu,
                Text = "SimBlock - Keyboard Blocker"
            };

            _notifyIcon.MouseClick += OnTrayIconClick;

            _logger.LogInformation("System tray icon initialized");
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
            if (e.Button == MouseButtons.Left)
            {
                TrayIconClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        private System.Drawing.Icon CreateIcon(bool isBlocked)
        {
            // Create a simple 16x16 icon
            var bitmap = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                // Fill with red if blocked, green if not
                var brush = new System.Drawing.SolidBrush(isBlocked ? 
                    System.Drawing.Color.Red : System.Drawing.Color.Green);
                g.FillEllipse(brush, 2, 2, 12, 12);
                
                // Add a border
                g.DrawEllipse(System.Drawing.Pens.Black, 2, 2, 12, 12);
                
                brush.Dispose();
            }

            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
                _disposed = true;
                _logger.LogInformation("System tray service disposed");
            }
        }
    }
}
