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
    public sealed class MacroEditorForm : Form
    {
        private readonly UISettings _uiSettings;
        private readonly ILogger<MacroEditorForm> _logger;
        private readonly IMacroService _macroService;

        private ComboBox _macroCombo = null!;
        private RoundedButton _refreshButton = null!;
        private RoundedButton _loadButton = null!;
        private RoundedButton _saveButton = null!;
        private RoundedButton _saveAsButton = null!;
        private RoundedButton _removeSelectedButton = null!;
        private RoundedButton _clearButton = null!;
        private RoundedButton _playButton = null!;
        private ListView _eventsList = null!;
        private Label _statusLabel = null!;

        private Macro? _currentMacro;

        public MacroEditorForm(UISettings uiSettings, ILogger<MacroEditorForm> logger, IMacroService macroService)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _macroService = macroService ?? throw new ArgumentNullException(nameof(macroService));

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Macro Editor";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(800, 520);
            MinimumSize = new Size(740, 460);
            BackColor = _uiSettings.BackgroundColor;
            ForeColor = _uiSettings.TextColor;

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(14),
                BackColor = _uiSettings.BackgroundColor,
                AutoSize = true
            };

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = _uiSettings.BackgroundColor };
            _macroCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, BackColor = _uiSettings.BackgroundColor, ForeColor = _uiSettings.TextColor };
            _refreshButton = new RoundedButton { Text = "Refresh", Size = new Size(90, 28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _loadButton = new RoundedButton { Text = "Load", Size = new Size(90, 28), BackColor = _uiSettings.PrimaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _saveButton = new RoundedButton { Text = "Save", Size = new Size(90, 28), BackColor = _uiSettings.PrimaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _saveAsButton = new RoundedButton { Text = "Save As...", Size = new Size(110, 28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _removeSelectedButton = new RoundedButton { Text = "Remove Selected", Size = new Size(140, 28), BackColor = _uiSettings.DangerButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _clearButton = new RoundedButton { Text = "Clear", Size = new Size(90, 28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _playButton = new RoundedButton { Text = "Play", Size = new Size(90, 28), BackColor = _uiSettings.SuccessColor, ForeColor = Color.White, CornerRadius = 6 };

            top.Controls.Add(new Label { Text = "Macro:", AutoSize = true, ForeColor = _uiSettings.TextColor });
            top.Controls.Add(_macroCombo);
            top.Controls.Add(_refreshButton);
            top.Controls.Add(_loadButton);
            top.Controls.Add(_saveButton);
            top.Controls.Add(_saveAsButton);
            top.Controls.Add(_removeSelectedButton);
            top.Controls.Add(_clearButton);
            top.Controls.Add(_playButton);

            _eventsList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                BackColor = _uiSettings.BackgroundColor,
                ForeColor = _uiSettings.TextColor
            };
            _eventsList.Columns.Add("#", 40);
            _eventsList.Columns.Add("Time(ms)", 80);
            _eventsList.Columns.Add("Device", 80);
            _eventsList.Columns.Add("Type", 100);
            _eventsList.Columns.Add("VK/Button", 80);
            _eventsList.Columns.Add("Mods", 80);
            _eventsList.Columns.Add("X", 60);
            _eventsList.Columns.Add("Y", 60);
            _eventsList.Columns.Add("Wheel", 60);

            _statusLabel = new Label { Text = "Ready", AutoSize = true, ForeColor = _uiSettings.TextColor };

            main.Controls.Add(top, 0, 0);
            main.Controls.Add(_eventsList, 0, 1);
            main.Controls.Add(_statusLabel, 0, 2);

            Controls.Add(main);

            Shown += async (s, e) => await LoadMacroNamesAsync();

            _refreshButton.Click += async (s, e) => await LoadMacroNamesAsync();
            _loadButton.Click += async (s, e) => await LoadSelectedMacroAsync();
            _saveButton.Click += async (s, e) => await SaveAsync();
            _saveAsButton.Click += async (s, e) => await SaveAsAsync();
            _removeSelectedButton.Click += (s, e) => RemoveSelectedEvents();
            _clearButton.Click += (s, e) => ClearEvents();
            _playButton.Click += async (s, e) => await PlayAsync();
        }

        public void SetMacro(Macro macro)
        {
            _currentMacro = macro;
            PopulateEvents();
            _statusLabel.Text = $"Loaded recorded macro '{macro.Name}' with {macro.Events.Count} events.";
            if (_macroCombo.Items.Count == 0)
            {
                _macroCombo.Items.Add(macro.Name);
                _macroCombo.SelectedIndex = 0;
            }
        }

        private async Task LoadMacroNamesAsync()
        {
            try
            {
                var names = await _macroService.ListAsync();
                var selected = _macroCombo.SelectedItem?.ToString();
                _macroCombo.Items.Clear();
                foreach (var n in names)
                    _macroCombo.Items.Add(n);
                if (!string.IsNullOrWhiteSpace(selected))
                {
                    var idx = _macroCombo.Items.IndexOf(selected);
                    if (idx >= 0) _macroCombo.SelectedIndex = idx;
                }
                if (_macroCombo.Items.Count > 0 && _macroCombo.SelectedIndex < 0)
                    _macroCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load macro names");
            }
        }

        private async Task LoadSelectedMacroAsync()
        {
            try
            {
                var name = _macroCombo.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(name)) return;
                var macro = await _macroService.LoadAsync(name);
                if (macro == null)
                {
                    MessageBox.Show($"Macro '{name}' not found.", "Macro Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _currentMacro = macro;
                PopulateEvents();
                _statusLabel.Text = $"Loaded '{name}' with {macro.Events.Count} events.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load macro");
            }
        }

        private void PopulateEvents()
        {
            _eventsList.BeginUpdate();
            _eventsList.Items.Clear();
            if (_currentMacro != null)
            {
                int index = 1;
                foreach (var ev in _currentMacro.Events.OrderBy(e => e.TimestampMs))
                {
                    var item = new ListViewItem(index.ToString());
                    item.SubItems.Add(ev.TimestampMs.ToString());
                    item.SubItems.Add(ev.Device.ToString());
                    item.SubItems.Add(ev.Type.ToString());
                    item.SubItems.Add(ev.Device == MacroEventDevice.Keyboard ? (ev.VirtualKeyCode?.ToString() ?? "") : (ev.Button?.ToString() ?? ""));
                    var mods = (ev.Ctrl ? "Ctrl+" : string.Empty) + (ev.Alt ? "Alt+" : string.Empty) + (ev.Shift ? "Shift" : string.Empty);
                    item.SubItems.Add(mods);
                    item.SubItems.Add(ev.X?.ToString() ?? "");
                    item.SubItems.Add(ev.Y?.ToString() ?? "");
                    item.SubItems.Add(ev.WheelDelta?.ToString() ?? "");
                    item.Tag = ev;
                    _eventsList.Items.Add(item);
                    index++;
                }
            }
            _eventsList.EndUpdate();
        }

        private void RemoveSelectedEvents()
        {
            if (_currentMacro == null) return;
            if (_eventsList.SelectedItems.Count == 0) return;
            foreach (ListViewItem item in _eventsList.SelectedItems)
            {
                var ev = item.Tag as MacroEvent;
                if (ev != null)
                {
                    _currentMacro.Events.Remove(ev);
                }
            }
            PopulateEvents();
            _statusLabel.Text = "Removed selected events.";
        }

        private void ClearEvents()
        {
            if (_currentMacro == null) return;
            if (MessageBox.Show("Clear all events?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _currentMacro.Events.Clear();
                PopulateEvents();
                _statusLabel.Text = "Cleared all events.";
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                if (_currentMacro == null) return;
                await _macroService.SaveAsync(_currentMacro);
                _statusLabel.Text = $"Saved '{_currentMacro.Name}'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save macro");
            }
        }

        private async Task SaveAsAsync()
        {
            try
            {
                if (_currentMacro == null) return;
                var input = Microsoft.VisualBasic.Interaction.InputBox("New name:", "Save Macro As", _currentMacro.Name);
                if (!string.IsNullOrWhiteSpace(input))
                {
                    _currentMacro.Name = input.Trim();
                    await _macroService.SaveAsync(_currentMacro);
                    await LoadMacroNamesAsync();
                    _statusLabel.Text = $"Saved as '{_currentMacro.Name}'.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save macro as");
            }
        }

        private async Task PlayAsync()
        {
            try
            {
                if (_currentMacro == null) return;
                await _macroService.PlayAsync(_currentMacro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play macro");
            }
        }
    }
}
