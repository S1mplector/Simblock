using SimBlock.Core.Application.Interfaces;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimBlock.Presentation.Forms
{
    public partial class UpdateDialog : Form
    {
        private readonly IAutoUpdateService _autoUpdateService;
        private UpdateInfo? _updateInfo;

        private Label _titleLabel;
        private Label _currentVersionLabel;
        private Label _newVersionLabel;
        private Label _releaseDateLabel;
        private Label _fileSizeLabel;
        private TextBox _releaseNotesTextBox;
        private ProgressBar _progressBar;
        private Label _progressLabel;
        private Button _updateButton;
        private Button _cancelButton;
        private Button _skipButton;

        public UpdateDialog(IAutoUpdateService autoUpdateService, UpdateInfo updateInfo)
        {
            _autoUpdateService = autoUpdateService;
            _updateInfo = updateInfo;

            InitializeComponent();
            SetupEventHandlers();
            PopulateUpdateInfo();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "SimBlock Update Available";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // Title label
            _titleLabel = new Label
            {
                Text = "A new version of SimBlock is available!",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(450, 25),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // Current version label
            _currentVersionLabel = new Label
            {
                Text = $"Current Version: {_autoUpdateService.GetCurrentVersion()}",
                Location = new Point(20, 60),
                Size = new Size(200, 20)
            };

            // New version label
            _newVersionLabel = new Label
            {
                Location = new Point(20, 85),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // Release date label
            _releaseDateLabel = new Label
            {
                Location = new Point(20, 110),
                Size = new Size(300, 20)
            };

            // File size label
            _fileSizeLabel = new Label
            {
                Location = new Point(20, 135),
                Size = new Size(200, 20)
            };

            // Release notes
            var releaseNotesLabel = new Label
            {
                Text = "Release Notes:",
                Location = new Point(20, 165),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            _releaseNotesTextBox = new TextBox
            {
                Location = new Point(20, 190),
                Size = new Size(450, 120),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = SystemColors.Control
            };

            // Progress bar
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 325),
                Size = new Size(450, 23),
                Visible = false
            };

            // Progress label
            _progressLabel = new Label
            {
                Location = new Point(20, 305),
                Size = new Size(450, 20),
                Visible = false
            };

            // Buttons
            _updateButton = new Button
            {
                Text = "Update Now",
                Location = new Point(285, 365),
                Size = new Size(90, 30),
                UseVisualStyleBackColor = true
            };

            _skipButton = new Button
            {
                Text = "Skip",
                Location = new Point(190, 365),
                Size = new Size(90, 30),
                UseVisualStyleBackColor = true
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(380, 365),
                Size = new Size(90, 30),
                UseVisualStyleBackColor = true,
                DialogResult = DialogResult.Cancel
            };

            // Add controls to form
            this.Controls.AddRange(new Control[]
            {
                _titleLabel,
                _currentVersionLabel,
                _newVersionLabel,
                _releaseDateLabel,
                _fileSizeLabel,
                releaseNotesLabel,
                _releaseNotesTextBox,
                _progressBar,
                _progressLabel,
                _updateButton,
                _skipButton,
                _cancelButton
            });

            this.CancelButton = _cancelButton;
            this.ResumeLayout(false);
        }

        private void SetupEventHandlers()
        {
            _updateButton.Click += UpdateButton_Click;
            _skipButton.Click += SkipButton_Click;
            _cancelButton.Click += CancelButton_Click;

            _autoUpdateService.UpdateProgressChanged += OnUpdateProgressChanged;
        }

        private void PopulateUpdateInfo()
        {
            if (_updateInfo == null) return;

            _newVersionLabel.Text = $"New Version: {_updateInfo.Version}";
            _releaseDateLabel.Text = $"Released: {_updateInfo.PublishedAt:MMM dd, yyyy}";
            _fileSizeLabel.Text = $"Download Size: {FormatFileSize(_updateInfo.FileSize)}";
            _releaseNotesTextBox.Text = _updateInfo.ReleaseNotes;
        }

        private async void UpdateButton_Click(object sender, EventArgs e)
        {
            if (_updateInfo == null) return;

            try
            {
                // Disable buttons and show progress
                _updateButton.Enabled = false;
                _skipButton.Enabled = false;
                _progressBar.Visible = true;
                _progressLabel.Visible = true;
                _progressLabel.Text = "Preparing to download...";

                // Start the update process
                var success = await _autoUpdateService.DownloadAndInstallUpdateAsync(_updateInfo);

                if (!success)
                {
                    MessageBox.Show("Update failed. Please try again later or download manually from GitHub.",
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Re-enable buttons
                    _updateButton.Enabled = true;
                    _skipButton.Enabled = true;
                    _progressBar.Visible = false;
                    _progressLabel.Visible = false;
                }
                // If successful, the application will exit and restart
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed: {ex.Message}", "Update Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Re-enable buttons
                _updateButton.Enabled = true;
                _skipButton.Enabled = true;
                _progressBar.Visible = false;
                _progressLabel.Visible = false;
            }
        }

        private void SkipButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Ignore;
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OnUpdateProgressChanged(object sender, UpdateProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnUpdateProgressChanged(sender, e)));
                return;
            }

            _progressBar.Value = Math.Min(e.ProgressPercentage, 100);
            _progressLabel.Text = e.Status;

            if (e.TotalBytes > 0)
            {
                var downloadedMB = e.BytesReceived / (1024.0 * 1024.0);
                var totalMB = e.TotalBytes / (1024.0 * 1024.0);
                _progressLabel.Text += $" ({downloadedMB:F1} MB / {totalMB:F1} MB)";
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            else
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoUpdateService.UpdateProgressChanged -= OnUpdateProgressChanged;
            }
            base.Dispose(disposing);
        }
    }
}