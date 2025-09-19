using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Controls;

namespace SimBlock.Presentation.Forms
{
    public class MacroManagerForm : Form
    {
        private readonly IMacroService _macroService;
        private readonly UISettings _uiSettings;
        private readonly ILogger<MacroManagerForm> _logger;

        private TextBox _nameTextBox = null!;
        private ListBox _macroList = null!;
        private RoundedButton _recordButton = null!;
        private RoundedButton _stopButton = null!;
        private RoundedButton _playButton = null!;
        private RoundedButton _saveButton = null!;
        private RoundedButton _refreshButton = null!;
        private Label _statusLabel = null!;

        public MacroManagerForm(IMacroService macroService, UISettings uiSettings, ILogger<MacroManagerForm> logger)
        {
            _macroService = macroService ?? throw new ArgumentNullException(nameof(macroService));
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Macro Manager";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(600, 450);
            MinimumSize = new Size(560, 380);
            BackColor = _uiSettings.BackgroundColor;
            ForeColor = _uiSettings.TextColor;

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(16),
                BackColor = _uiSettings.BackgroundColor,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            // Top row: name + buttons
            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 6,
                RowCount = 1,
                AutoSize = true,
                BackColor = _uiSettings.BackgroundColor
            };

            var nameLabel = new Label { Text = "Name:", AutoSize = true, ForeColor = _uiSettings.TextColor };
            _nameTextBox = new TextBox { Width = 180, BackColor = _uiSettings.BackgroundColor, ForeColor = _uiSettings.TextColor };

            _recordButton = new RoundedButton { Text = "Record", Size = new Size(90, 28), BackColor = _uiSettings.PrimaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _stopButton   = new RoundedButton { Text = "Stop",   Size = new Size(90, 28), BackColor = _uiSettings.DangerButtonColor,  ForeColor = Color.White, CornerRadius = 6 };
            _playButton   = new RoundedButton { Text = "Play",   Size = new Size(90, 28), BackColor = _uiSettings.SuccessColor, ForeColor = Color.White, CornerRadius = 6 };
            _saveButton   = new RoundedButton { Text = "Save",   Size = new Size(90, 28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };

            top.Controls.Add(nameLabel, 0, 0);
            top.Controls.Add(_nameTextBox, 1, 0);
            top.Controls.Add(_recordButton, 2, 0);
            top.Controls.Add(_stopButton, 3, 0);
            top.Controls.Add(_playButton, 4, 0);
            top.Controls.Add(_saveButton, 5, 0);

            // Middle row: list + refresh
            var middle = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                BackColor = _uiSettings.BackgroundColor
            };

            _macroList = new ListBox
            {
                Dock = DockStyle.Fill,
                Height = 260,
                BackColor = _uiSettings.BackgroundColor,
                ForeColor = _uiSettings.TextColor
            };
            _refreshButton = new RoundedButton { Text = "Refresh", Size = new Size(90, 28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };

            middle.Controls.Add(_macroList, 0, 0);
            middle.Controls.Add(_refreshButton, 1, 0);

            // Bottom row: status
            _statusLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Text = "Ready",
                ForeColor = _uiSettings.TextColor
            };

            main.Controls.Add(top, 0, 0);
            main.Controls.Add(middle, 0, 1);
            main.Controls.Add(_statusLabel, 0, 2);

            Controls.Add(main);

            // Events
            _recordButton.Click += async (s, e) =>
            {
                var name = string.IsNullOrWhiteSpace(_nameTextBox.Text) ? $"Macro_{DateTime.Now:HHmmss}" : _nameTextBox.Text.Trim();
                _macroService.StartRecording(name);
                _statusLabel.Text = $"Recording '{name}'...";
                _logger.LogInformation("MacroManager: recording started: {Name}", name);
            };

            _stopButton.Click += (s, e) =>
            {
                try
                {
                    var macro = _macroService.StopRecording();
                    _nameTextBox.Text = macro.Name;
                    _statusLabel.Text = $"Recorded '{macro.Name}' with {macro.Events.Count} events.";
                    _logger.LogInformation("MacroManager: recording stopped: {Name} with {Count} events", macro.Name, macro.Events.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MacroManager: stop error");
                    MessageBox.Show($"Failed to stop recording.\n\nError: {ex.Message}", "Macro Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            _playButton.Click += async (s, e) =>
            {
                try
                {
                    var name = _macroList.SelectedItem?.ToString() ?? _nameTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(name)) return;
                    var macro = await _macroService.LoadAsync(name);
                    if (macro != null)
                    {
                        _statusLabel.Text = $"Playing '{macro.Name}'...";
                        await _macroService.PlayAsync(macro);
                        _statusLabel.Text = $"Finished '{macro.Name}'.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MacroManager: play error");
                    MessageBox.Show($"Failed to play macro.\n\nError: {ex.Message}", "Macro Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            _saveButton.Click += async (s, e) =>
            {
                try
                {
                    if (_macroService.CurrentRecording != null && _macroService.IsRecording == false)
                    {
                        await _macroService.SaveAsync(_macroService.CurrentRecording);
                    }
                    else if (!string.IsNullOrWhiteSpace(_nameTextBox.Text))
                    {
                        var macro = await _macroService.LoadAsync(_nameTextBox.Text.Trim());
                        if (macro != null)
                            await _macroService.SaveAsync(macro);
                    }
                    await RefreshListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MacroManager: save error");
                    MessageBox.Show($"Failed to save macro.\n\nError: {ex.Message}", "Macro Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            _refreshButton.Click += async (s, e) => await RefreshListAsync();

            Shown += async (s, e) => await RefreshListAsync();
        }

        private async Task RefreshListAsync()
        {
            try
            {
                var names = await _macroService.ListAsync();
                _macroList.Items.Clear();
                foreach (var n in names)
                    _macroList.Items.Add(n);
                _statusLabel.Text = $"Loaded {names.Count} macros.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MacroManager: list refresh error");
            }
        }
    }
}
