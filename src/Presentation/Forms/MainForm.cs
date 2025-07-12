using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Presentation.ViewModels;

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

        // UI Controls
        private Button _toggleButton = null!;
        private Label _statusLabel = null!;
        private Label _lastToggleLabel = null!;
        private Button _hideToTrayButton = null!;

        public MainForm(IKeyboardBlockerService keyboardBlockerService, ILogger<MainForm> logger)
        {
            _keyboardBlockerService = keyboardBlockerService ?? throw new ArgumentNullException(nameof(keyboardBlockerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = new MainWindowViewModel();

            InitializeComponent();
            InitializeEventHandlers();
            UpdateUI();
            
            // Debug: Show message to confirm window creation
            this.Load += (s, e) => 
            {
                this.BringToFront();
                this.Activate();
                this.Focus();
                MessageBox.Show("SimBlock GUI is now visible!", "SimBlock", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
        }

        private void InitializeComponent()
        {
            // Form properties
            Text = "SimBlock - Keyboard Blocker";
            Size = new System.Drawing.Size(400, 300);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            Visible = true;
            TopMost = false;
            Icon = CreateApplicationIcon();

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
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
                Text = "Emergency unlock: Ctrl+Alt+U",
                Font = new System.Drawing.Font("Segoe UI", 8),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = System.Drawing.Color.DarkGray
            };

            // Add controls to panel
            mainPanel.Controls.Add(_statusLabel, 0, 0);
            mainPanel.Controls.Add(_toggleButton, 0, 1);
            mainPanel.Controls.Add(_lastToggleLabel, 0, 2);
            mainPanel.Controls.Add(_hideToTrayButton, 0, 3);
            mainPanel.Controls.Add(instructionsLabel, 0, 4);

            // Set row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10));

            Controls.Add(mainPanel);
        }

        private void InitializeEventHandlers()
        {
            _toggleButton.Click += OnToggleButtonClick;
            _hideToTrayButton.Click += OnHideToTrayButtonClick;
            _keyboardBlockerService.StateChanged += OnKeyboardStateChanged;

            // Handle form closing
            FormClosing += OnFormClosing;
        }

        private async void OnToggleButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Toggle button clicked");
                await _keyboardBlockerService.ToggleBlockingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling keyboard blocking");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        }

        private void UpdateUI()
        {
            _statusLabel.Text = _viewModel.StatusText;
            _statusLabel.ForeColor = _viewModel.IsKeyboardBlocked ? 
                System.Drawing.Color.Red : System.Drawing.Color.Green;

            _toggleButton.Text = _viewModel.ToggleButtonText;
            _toggleButton.BackColor = _viewModel.IsKeyboardBlocked ? 
                System.Drawing.Color.FromArgb(215, 0, 0) : System.Drawing.Color.FromArgb(0, 120, 215);

            _lastToggleLabel.Text = $"Last toggle: {_viewModel.LastToggleTime:HH:mm:ss}";
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            // Don't actually close, just hide to tray
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private System.Drawing.Icon CreateApplicationIcon()
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
