using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Controls;
using SimBlock.Core.Domain.Interfaces;

namespace SimBlock.Presentation.Forms
{
    public class MacroManagerForm : Form
    {
        private readonly IMacroService _macroService;
        private readonly UISettings _uiSettings;
        private readonly ILogger<MacroManagerForm> _logger;
        private readonly IMacroMappingService _mappingService;
        private readonly IKeyboardHookService _keyboardHookService;
        private readonly IMouseHookService _mouseHookService;
        private readonly IServiceProvider _serviceProvider;

        private TextBox _nameTextBox = null!;
        private ListBox _macroList = null!;
        private RoundedButton _recordButton = null!;
        private RoundedButton _stopButton = null!;
        private RoundedButton _playButton = null!;
        private RoundedButton _saveButton = null!;
        private RoundedButton _refreshButton = null!;
        private RoundedButton _deleteButton = null!;
        private RoundedButton _renameButton = null!;
        private RoundedButton _importButton = null!;
        private RoundedButton _exportButton = null!;
        private RoundedButton _cancelPlayButton = null!;
        private NumericUpDown _loopsUpDown = null!;
        private ComboBox _speedCombo = null!;
        private ComboBox _recordDevicesCombo = null!;
        private Label _statusLabel = null!;
        private RoundedButton _openMappingButton = null!;
        private RoundedButton _openEditorButton = null!;
        private ToolTip _tips = null!;
        private ContextMenuStrip _listMenu = null!;
        private TextBox _searchTextBox = null!;
        private System.Collections.Generic.List<string> _allMacroNames = new System.Collections.Generic.List<string>();

        private Macro? _lastRecordedMacro;
        private CancellationTokenSource? _playCts;

        public MacroManagerForm(
            IMacroService macroService,
            UISettings uiSettings,
            ILogger<MacroManagerForm> logger,
            IMacroMappingService mappingService,
            IKeyboardHookService keyboardHookService,
            IMouseHookService mouseHookService,
            IServiceProvider serviceProvider)
        {
            _macroService = macroService ?? throw new ArgumentNullException(nameof(macroService));
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
            _keyboardHookService = keyboardHookService ?? throw new ArgumentNullException(nameof(keyboardHookService));
            _mouseHookService = mouseHookService ?? throw new ArgumentNullException(nameof(mouseHookService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

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
            KeyPreview = true;

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
                ColumnCount = 16,
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
            _cancelPlayButton = new RoundedButton { Text = "Cancel", Size = new Size(90,28), BackColor = _uiSettings.DangerButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _deleteButton = new RoundedButton { Text = "Delete", Size = new Size(90,28), BackColor = _uiSettings.DangerButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _renameButton = new RoundedButton { Text = "Rename", Size = new Size(90,28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _importButton = new RoundedButton { Text = "Import", Size = new Size(90,28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _exportButton = new RoundedButton { Text = "Export", Size = new Size(90,28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _openMappingButton = new RoundedButton { Text = "Mappings...", Size = new Size(110,28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _openEditorButton = new RoundedButton { Text = "Edit Events...", Size = new Size(110,28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };

            _tips = new ToolTip { ShowAlways = true };
            _tips.SetToolTip(_recordButton, "Start recording (Ctrl+R)");
            _tips.SetToolTip(_stopButton, "Stop recording (Ctrl+Shift+R)");
            _tips.SetToolTip(_playButton, "Play selected macro (Enter)");
            _tips.SetToolTip(_cancelPlayButton, "Cancel playback (Esc)");
            _tips.SetToolTip(_saveButton, "Save current macro (Ctrl+S)");
            _tips.SetToolTip(_deleteButton, "Delete selected macro (Del)");
            _tips.SetToolTip(_renameButton, "Rename selected macro (F2)");
            _tips.SetToolTip(_importButton, "Import macro from file");
            _tips.SetToolTip(_exportButton, "Export selected macro to file");
            _tips.SetToolTip(_openMappingButton, "Manage hotkey/button mappings");
            _tips.SetToolTip(_openEditorButton, "Open macro editor");

            var speedLabel = new Label { Text = "Speed:", AutoSize = true, ForeColor = _uiSettings.TextColor };
            _speedCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 70, BackColor = _uiSettings.BackgroundColor, ForeColor = _uiSettings.TextColor };
            _speedCombo.Items.AddRange(new object[] { "0.5x", "1.0x", "1.5x", "2.0x" });
            _speedCombo.SelectedIndex = 1;
            _tips.SetToolTip(_speedCombo, "Playback speed");

            var loopsLabel = new Label { Text = "Loops:", AutoSize = true, ForeColor = _uiSettings.TextColor };
            _loopsUpDown = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 1, Width = 60, BackColor = _uiSettings.BackgroundColor, ForeColor = _uiSettings.TextColor };
            _tips.SetToolTip(_loopsUpDown, "Number of loops");

            var devicesLabel = new Label { Text = "Devices:", AutoSize = true, ForeColor = _uiSettings.TextColor };
            _recordDevicesCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90, BackColor = _uiSettings.BackgroundColor, ForeColor = _uiSettings.TextColor };
            _tips.SetToolTip(_recordDevicesCombo, "Choose devices to record");
            _recordDevicesCombo.Items.AddRange(new object[] { "Both", "Keyboard", "Mouse" });
            _recordDevicesCombo.SelectedIndex = 0;

            top.Controls.Add(nameLabel, 0, 0);
            top.Controls.Add(_nameTextBox, 1, 0);
            top.Controls.Add(_recordButton, 2, 0);
            top.Controls.Add(_stopButton, 3, 0);
            top.Controls.Add(_playButton, 4, 0);
            top.Controls.Add(_cancelPlayButton, 5, 0);
            top.Controls.Add(speedLabel, 6, 0);
            top.Controls.Add(_speedCombo, 7, 0);
            top.Controls.Add(loopsLabel, 8, 0);
            top.Controls.Add(_loopsUpDown, 9, 0);
            top.Controls.Add(_saveButton, 10, 0);
            top.Controls.Add(_renameButton, 11, 0);
            top.Controls.Add(devicesLabel, 12, 0);
            top.Controls.Add(_recordDevicesCombo, 13, 0);
            var filterLabel = new Label { Text = "Search:", AutoSize = true, ForeColor = _uiSettings.TextColor };
            _searchTextBox = new TextBox { Width = 120, BackColor = _uiSettings.BackgroundColor, ForeColor = _uiSettings.TextColor };
            top.Controls.Add(filterLabel, 14, 0);
            top.Controls.Add(_searchTextBox, 15, 0);

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
            var rightButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true, BackColor = _uiSettings.BackgroundColor };
            rightButtons.Controls.Add(_refreshButton);
            rightButtons.Controls.Add(_deleteButton);
            rightButtons.Controls.Add(_importButton);
            rightButtons.Controls.Add(_exportButton);
            rightButtons.Controls.Add(_openMappingButton);
            rightButtons.Controls.Add(_openEditorButton);

            _listMenu = new ContextMenuStrip();
            _listMenu.Items.Add("Play", null, (s, e) => _playButton.PerformClick());
            _listMenu.Items.Add("Edit Events...", null, (s, e) => _openEditorButton.PerformClick());
            _listMenu.Items.Add(new ToolStripSeparator());
            _listMenu.Items.Add("Rename", null, (s, e) => _renameButton.PerformClick());
            _listMenu.Items.Add("Delete", null, (s, e) => _deleteButton.PerformClick());
            _listMenu.Items.Add("Export", null, (s, e) => _exportButton.PerformClick());
            _macroList.ContextMenuStrip = _listMenu;
            _macroList.DoubleClick += (s, e) => _openEditorButton.PerformClick();
            _macroList.SelectedIndexChanged += (s, e) => { if (_macroList.SelectedItem != null) _nameTextBox.Text = _macroList.SelectedItem.ToString()!; UpdateEnabledState(); };
            _macroList.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { _playButton.PerformClick(); e.Handled = true; }
                else if (e.KeyCode == Keys.Delete) { _deleteButton.PerformClick(); e.Handled = true; }
                else if (e.KeyCode == Keys.F2) { _renameButton.PerformClick(); e.Handled = true; }
            };

            middle.Controls.Add(_macroList, 0, 0);
            middle.Controls.Add(rightButtons, 1, 0);

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
                var devices = GetSelectedDevices();
                _macroService.StartRecording(name, devices);
                _statusLabel.Text = $"Recording '{name}'...";
                _logger.LogInformation("MacroManager: recording started: {Name}", name);
                _lastRecordedMacro = null;
                UpdateEnabledState();
            };

            _stopButton.Click += (s, e) =>
            {
                try
                {
                    var macro = _macroService.StopRecording();
                    _nameTextBox.Text = macro.Name;
                    _statusLabel.Text = $"Recorded '{macro.Name}' with {macro.Events.Count} events.";
                    _lastRecordedMacro = macro;
                    _logger.LogInformation("MacroManager: recording stopped: {Name} with {Count} events", macro.Name, macro.Events.Count);
                    // Immediately show recorded events in the Macro Editor
                    try
                    {
                        using var editorForm = _serviceProvider.GetRequiredService<MacroEditorForm>();
                        editorForm.SetMacro(macro);
                        editorForm.ShowDialog(this);
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogError(ex2, "MacroManager: failed to open Macro Editor after recording");
                    }
                    UpdateEnabledState();
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
                        _playCts?.Cancel();
                        _playCts?.Dispose();
                        _playCts = new CancellationTokenSource();
                        UpdateEnabledState();
                        try
                        {
                            var speed = ParseSpeed(_speedCombo.SelectedItem?.ToString());
                            var loops = (int)_loopsUpDown.Value;
                            await _macroService.PlayAsync(macro, _playCts.Token, speed, loops);
                            _statusLabel.Text = _playCts.IsCancellationRequested ? $"Cancelled '{macro.Name}'." : $"Finished '{macro.Name}'.";
                        }
                        finally
                        {
                            _playCts?.Dispose();
                            _playCts = null;
                            UpdateEnabledState();
                        }
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
                    if (_lastRecordedMacro != null)
                    {
                        await _macroService.SaveAsync(_lastRecordedMacro);
                        _statusLabel.Text = $"Saved '{_lastRecordedMacro.Name}'.";
                        _lastRecordedMacro = null;
                    }
                    else if (!string.IsNullOrWhiteSpace(_nameTextBox.Text))
                    {
                        var loaded = await _macroService.LoadAsync(_nameTextBox.Text.Trim());
                        if (loaded != null)
                        {
                            await _macroService.SaveAsync(loaded);
                            _statusLabel.Text = $"Saved '{loaded.Name}'.";
                        }
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
            _searchTextBox.TextChanged += (s, e) => ApplyFilter();

            _cancelPlayButton.Click += (s, e) =>
            {
                try
                {
                    _playCts?.Cancel();
                }
                catch { }
            };

            _deleteButton.Click += async (s, e) =>
            {
                var name = _macroList.SelectedItem?.ToString() ?? _nameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name)) return;
                if (MessageBox.Show($"Delete macro '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    var ok = await _macroService.DeleteAsync(name);
                    if (ok)
                    {
                        _statusLabel.Text = $"Deleted '{name}'.";
                        await RefreshListAsync();
                    }
                }
            };

            _renameButton.Click += async (s, e) =>
            {
                var name = _macroList.SelectedItem?.ToString() ?? _nameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name)) return;
                var input = Microsoft.VisualBasic.Interaction.InputBox("New name:", "Rename Macro", name);
                if (!string.IsNullOrWhiteSpace(input) && !input.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    var ok = await _macroService.RenameAsync(name, input.Trim());
                    if (ok)
                    {
                        _statusLabel.Text = $"Renamed '{name}' to '{input}'.";
                        _nameTextBox.Text = input.Trim();
                        await RefreshListAsync();
                    }
                }
            };

            _importButton.Click += async (s, e) =>
            {
                using var ofd = new OpenFileDialog { Filter = "Macro files (*.json)|*.json|All files|*.*" };
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    var ok = await _macroService.ImportAsync(ofd.FileName, overwrite: false);
                    if (!ok)
                    {
                        if (MessageBox.Show("A macro with the same name may exist. Overwrite?", "Import", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            ok = await _macroService.ImportAsync(ofd.FileName, overwrite: true);
                        }
                    }
                    if (ok)
                    {
                        _statusLabel.Text = "Imported successfully.";
                        await RefreshListAsync();
                    }
                }
            };

            _exportButton.Click += async (s, e) =>
            {
                var name = _macroList.SelectedItem?.ToString() ?? _nameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name)) return;
                using var sfd = new SaveFileDialog { FileName = name + ".json", Filter = "Macro files (*.json)|*.json|All files|*.*" };
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    var ok = await _macroService.ExportAsync(name, sfd.FileName, overwrite: true);
                    if (ok)
                    {
                        _statusLabel.Text = $"Exported to '{sfd.FileName}'.";
                    }
                }
            };

            _openMappingButton.Click += (s, e) =>
            {
                try
                {
                    using var mappingForm = _serviceProvider.GetRequiredService<MacroMappingForm>();
                    mappingForm.ShowDialog(this);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MacroManager: open mappings error");
                    MessageBox.Show($"Failed to open Macro Mappings.\n\nError: {ex.Message}", "Macro Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            _openEditorButton.Click += (s, e) =>
            {
                try
                {
                    using var editorForm = _serviceProvider.GetRequiredService<MacroEditorForm>();
                    editorForm.ShowDialog(this);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MacroManager: open editor error");
                    MessageBox.Show($"Failed to open Macro Editor.\n\nError: {ex.Message}", "Macro Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Shown += async (s, e) => await RefreshListAsync();
            FormClosing += (s, e) => { try { _playCts?.Cancel(); } catch { } };
            UpdateEnabledState();

            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.R) { _recordButton.PerformClick(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.S) { _saveButton.PerformClick(); e.Handled = true; }
                else if (e.KeyCode == Keys.Escape && _cancelPlayButton.Enabled) { _cancelPlayButton.PerformClick(); e.Handled = true; }
            };
        }

        private async Task RefreshListAsync()
        {
            try
            {
                var names = await _macroService.ListAsync();
                _allMacroNames = names.ToList();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MacroManager: list refresh error");
            }
        }

        private void ApplyFilter()
        {
            var q = _searchTextBox?.Text?.Trim() ?? string.Empty;
            _macroList.BeginUpdate();
            _macroList.Items.Clear();
            foreach (var n in _allMacroNames)
            {
                if (string.IsNullOrEmpty(q) || n.Contains(q, StringComparison.OrdinalIgnoreCase))
                    _macroList.Items.Add(n);
            }
            _macroList.EndUpdate();
            _statusLabel.Text = $"Loaded {_macroList.Items.Count} macros.";
        }

        private double ParseSpeed(string? s)
        {
            return s switch
            {
                "0.5x" => 0.5,
                "1.5x" => 1.5,
                "2.0x" => 2.0,
                _ => 1.0
            };
        }

        private MacroRecordingDevices GetSelectedDevices()
        {
            var s = _recordDevicesCombo.SelectedItem?.ToString();
            return s switch
            {
                "Keyboard" => MacroRecordingDevices.Keyboard,
                "Mouse" => MacroRecordingDevices.Mouse,
                _ => MacroRecordingDevices.Both
            };
        }

        private void UpdateEnabledState()
        {
            var recording = _macroService.IsRecording;
            var playing = _macroService.IsPlaying || _playCts != null;
            _recordButton.Enabled = !recording && !playing;
            _stopButton.Enabled = recording;
            _playButton.Enabled = !recording && !playing;
            _cancelPlayButton.Enabled = playing;
            _saveButton.Enabled = !recording;
            _deleteButton.Enabled = !recording && !playing;
            _renameButton.Enabled = !recording && !playing;
            _importButton.Enabled = !recording && !playing;
            _exportButton.Enabled = !recording && !playing && (_macroList.SelectedItem != null || !string.IsNullOrWhiteSpace(_nameTextBox.Text));
            _speedCombo.Enabled = !recording && !playing;
            _loopsUpDown.Enabled = !recording && !playing;
            _recordDevicesCombo.Enabled = !recording && !playing;
        }
    }
}
