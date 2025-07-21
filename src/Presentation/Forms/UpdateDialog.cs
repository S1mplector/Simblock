using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimBlock.Core.Application.Interfaces;

namespace SimBlock.Presentation.Forms
{
    public partial class UpdateDialog : Form
    {
        private readonly IAutoUpdateService _autoUpdateService;
        private UpdateInfo? _updateInfo;

        private Label _titleLabel = null!;
        private Label _currentVersionLabel = null!;
        private Label _newVersionLabel = null!;
        private Label _releaseDateLabel = null!;
        private Label _fileSizeLabel = null!;
        private RichTextBox _releaseNotesTextBox = null!;
        private ProgressBar _progressBar = null!;
        private Label _progressLabel = null!;
        private Button _updateButton = null!;
        private Button _cancelButton = null!;
        private Button _skipButton = null!;

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
            this.Size = new Size(600, 455);
            this.StartPosition = FormStartPosition.CenterScreen;
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

            _releaseNotesTextBox = new RichTextBox
            {
                Location = new Point(20, 190),
                Size = new Size(450, 120),
                ReadOnly = true,
                BackColor = SystemColors.Control,
                BorderStyle = BorderStyle.FixedSingle,
                DetectUrls = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            _releaseNotesTextBox.LinkClicked += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = e.LinkText,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open link: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
            
            // Apply markdown styling to the release notes
            ApplyMarkdownStyling(_updateInfo.ReleaseNotes);
        }

        private void ApplyMarkdownStyling(string markdownText)
        {
            if (string.IsNullOrEmpty(markdownText))
            {
                _releaseNotesTextBox.Text = "No release notes available.";
                return;
            }

            // Save current position and selection
            var savedSelectionStart = _releaseNotesTextBox.SelectionStart;
            var savedSelectionLength = _releaseNotesTextBox.SelectionLength;
            var savedScrollPosition = _releaseNotesTextBox.GetPositionFromCharIndex(0);

            // Clear existing text and formatting
            _releaseNotesTextBox.Clear();
            _releaseNotesTextBox.SelectionFont = new Font("Segoe UI", 9);
            _releaseNotesTextBox.SelectionColor = SystemColors.ControlText;

            // Simple markdown parsing
            var lines = markdownText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            bool inCodeBlock = false;
            bool inList = false;
            int listIndent = 0;

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                
                // Handle code blocks
                if (trimmedLine.StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    _releaseNotesTextBox.SelectionBackColor = inCodeBlock ? Color.FromArgb(240, 240, 240) : SystemColors.Control;
                    _releaseNotesTextBox.SelectionFont = new Font("Consolas", 9);
                    _releaseNotesTextBox.AppendText(Environment.NewLine);
                    continue;
                }

                if (inCodeBlock)
                {
                    _releaseNotesTextBox.AppendText(line + Environment.NewLine);
                    continue;
                }

                // Reset formatting for new line
                _releaseNotesTextBox.SelectionFont = new Font("Segoe UI", 9);
                _releaseNotesTextBox.SelectionColor = SystemColors.ControlText;
                _releaseNotesTextBox.SelectionBullet = false;

                // Handle headers
                if (trimmedLine.StartsWith("### "))
                {
                    _releaseNotesTextBox.SelectionFont = new Font("Segoe UI", 11, FontStyle.Bold);
                    _releaseNotesTextBox.AppendText(trimmedLine.Substring(4) + Environment.NewLine);
                    continue;
                }
                else if (trimmedLine.StartsWith("## "))
                {
                    _releaseNotesTextBox.SelectionFont = new Font("Segoe UI", 12, FontStyle.Bold);
                    _releaseNotesTextBox.AppendText(trimmedLine.Substring(3) + Environment.NewLine);
                    continue;
                }
                else if (trimmedLine.StartsWith("# "))
                {
                    _releaseNotesTextBox.SelectionFont = new Font("Segoe UI", 14, FontStyle.Bold);
                    _releaseNotesTextBox.AppendText(trimmedLine.Substring(2) + Environment.NewLine);
                    continue;
                }

                // Handle lists
                if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
                {
                    if (!inList)
                    {
                        inList = true;
                        _releaseNotesTextBox.SelectionBullet = true;
                        listIndent = line.IndexOf(trimmedLine[0]);
                    }
                    _releaseNotesTextBox.AppendText(trimmedLine.Substring(2) + Environment.NewLine);
                    continue;
                }
                else if (inList && !string.IsNullOrWhiteSpace(line) && line.TrimStart().Length > 0)
                {
                    // Handle list item continuation
                    _releaseNotesTextBox.SelectionHangingIndent = 20;
                    _releaseNotesTextBox.AppendText(line.TrimStart() + Environment.NewLine);
                    continue;
                }
                else
                {
                    inList = false;
                    _releaseNotesTextBox.SelectionBullet = false;
                }

                // Handle bold and italic (simple implementation)
                string processedLine = line;
                int boldStart, boldEnd;
                
                // Process bold text
                while ((boldStart = processedLine.IndexOf("**")) >= 0 && 
                       (boldEnd = processedLine.IndexOf("**", boldStart + 2)) > boldStart)
                {
                    // Text before bold
                    _releaseNotesTextBox.AppendText(processedLine.Substring(0, boldStart));
                    
                    // Bold text
                    _releaseNotesTextBox.SelectionFont = new Font(_releaseNotesTextBox.Font, FontStyle.Bold);
                    _releaseNotesTextBox.AppendText(processedLine.Substring(boldStart + 2, boldEnd - boldStart - 2));
                    _releaseNotesTextBox.SelectionFont = new Font(_releaseNotesTextBox.Font, FontStyle.Regular);
                    
                    // Remaining text
                    processedLine = processedLine.Substring(boldEnd + 2);
                }
                
                // Process italic text
                while ((boldStart = processedLine.IndexOf('*')) >= 0 && 
                       boldStart < processedLine.Length - 1 &&
                       processedLine[boldStart + 1] != ' ')
                {
                    // Find matching closing asterisk
                    boldEnd = processedLine.IndexOf('*', boldStart + 1);
                    if (boldEnd < 0) break;
                    
                    // Text before italic
                    _releaseNotesTextBox.AppendText(processedLine.Substring(0, boldStart));
                    
                    // Italic text
                    _releaseNotesTextBox.SelectionFont = new Font(_releaseNotesTextBox.Font, FontStyle.Italic);
                    _releaseNotesTextBox.AppendText(processedLine.Substring(boldStart + 1, boldEnd - boldStart - 1));
                    _releaseNotesTextBox.SelectionFont = new Font(_releaseNotesTextBox.Font, FontStyle.Regular);
                    
                    // Remaining text
                    processedLine = processedLine.Substring(boldEnd + 1);
                }
                
                // Add any remaining text
                _releaseNotesTextBox.AppendText(processedLine + Environment.NewLine);
            }

            // Restore position and selection
            _releaseNotesTextBox.SelectionStart = savedSelectionStart;
            _releaseNotesTextBox.SelectionLength = savedSelectionLength;
            
            try
            {
                // Try to restore scroll position
                if (savedScrollPosition != Point.Empty)
                {
                    _releaseNotesTextBox.ScrollToCaret();
                }
            }
            catch
            {
                // Ignore scroll position restoration errors
            }
        }

        private async void UpdateButton_Click(object? sender, EventArgs e)
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

        private void SkipButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Ignore;
            this.Close();
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OnUpdateProgressChanged(object? sender, UpdateProgressEventArgs e)
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