using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Interfaces;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Controls;

namespace SimBlock.Presentation.Forms
{
    public sealed class MacroMappingForm : Form
    {
        private readonly UISettings _uiSettings;
        private readonly ILogger<MacroMappingForm> _logger;
        private readonly IMacroService _macroService;
        private readonly IMacroMappingService _mappingService;
        private readonly IKeyboardHookService _keyboardHookService;
        private readonly IMouseHookService _mouseHookService;

        private ComboBox _macroCombo = null!;
        private RadioButton _deviceKeyboard = null!;
        private RadioButton _deviceMouse = null!;
        private RadioButton _edgeDown = null!;
        private RadioButton _edgeUp = null!;
        private Label _triggerPreview = null!;
        private RoundedButton _recordTriggerButton = null!;
        private RoundedButton _addBindingButton = null!;
        private RoundedButton _removeBindingButton = null!;
        private RoundedButton _toggleBindingButton = null!;
        private RoundedButton _clearAllButton = null!;
        private ListView _bindingsList = null!;
        private CheckBox _enableMappingsCheck = null!;
        private Label _statusLabel = null!;

        private MacroTrigger? _currentTrigger;
        private bool _listening;

        public MacroMappingForm(
            UISettings uiSettings,
            ILogger<MacroMappingForm> logger,
            IMacroService macroService,
            IMacroMappingService mappingService,
            IKeyboardHookService keyboardHookService,
            IMouseHookService mouseHookService)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _macroService = macroService ?? throw new ArgumentNullException(nameof(macroService));
            _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
            _keyboardHookService = keyboardHookService ?? throw new ArgumentNullException(nameof(keyboardHookService));
            _mouseHookService = mouseHookService ?? throw new ArgumentNullException(nameof(mouseHookService));

            InitializeComponent();
            WireEvents();
        }

        private void InitializeComponent()
        {
            Text = "Macro Mappings";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(700, 480);
            MinimumSize = new Size(660, 420);
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

            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 8,
                RowCount = 2,
                AutoSize = true,
                BackColor = _uiSettings.BackgroundColor
            };

            _macroCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, BackColor = _uiSettings.BackgroundColor, ForeColor = _uiSettings.TextColor };
            _deviceKeyboard = new RadioButton { Text = "Keyboard", Checked = true, AutoSize = true, ForeColor = _uiSettings.TextColor };
            _deviceMouse = new RadioButton { Text = "Mouse", AutoSize = true, ForeColor = _uiSettings.TextColor };
            _edgeDown = new RadioButton { Text = "Down", Checked = true, AutoSize = true, ForeColor = _uiSettings.TextColor };
            _edgeUp = new RadioButton { Text = "Up", AutoSize = true, ForeColor = _uiSettings.TextColor };
            _recordTriggerButton = new RoundedButton { Text = "Record Trigger", Size = new Size(120, 28), BackColor = _uiSettings.PrimaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _triggerPreview = new Label { Text = "No trigger", AutoSize = true, ForeColor = _uiSettings.InactiveColor };
            _addBindingButton = new RoundedButton { Text = "Add/Update", Size = new Size(110, 28), BackColor = _uiSettings.SuccessColor, ForeColor = Color.White, CornerRadius = 6 };

            top.Controls.Add(new Label { Text = "Macro:", AutoSize = true, ForeColor = _uiSettings.TextColor }, 0, 0);
            top.Controls.Add(_macroCombo, 1, 0);
            top.Controls.Add(new Label { Text = "Device:", AutoSize = true, ForeColor = _uiSettings.TextColor }, 2, 0);
            var devPanel = new FlowLayoutPanel { AutoSize = true, BackColor = _uiSettings.BackgroundColor };
            devPanel.Controls.Add(_deviceKeyboard);
            devPanel.Controls.Add(_deviceMouse);
            top.Controls.Add(devPanel, 3, 0);
            top.Controls.Add(new Label { Text = "Edge:", AutoSize = true, ForeColor = _uiSettings.TextColor }, 4, 0);
            var edgePanel = new FlowLayoutPanel { AutoSize = true, BackColor = _uiSettings.BackgroundColor };
            edgePanel.Controls.Add(_edgeDown);
            edgePanel.Controls.Add(_edgeUp);
            top.Controls.Add(edgePanel, 5, 0);
            top.Controls.Add(_recordTriggerButton, 6, 0);
            top.Controls.Add(_addBindingButton, 7, 0);
            top.Controls.Add(new Label { Text = "Trigger:", AutoSize = true, ForeColor = _uiSettings.TextColor }, 0, 1);
            top.Controls.Add(_triggerPreview, 1, 1);
            top.SetColumnSpan(_triggerPreview, 7);

            // Middle: bindings list + buttons on right
            var middle = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = _uiSettings.BackgroundColor,
                AutoSize = true
            };

            _bindingsList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                BackColor = _uiSettings.BackgroundColor,
                ForeColor = _uiSettings.TextColor
            };
            _bindingsList.Columns.Add("Enabled", 80);
            _bindingsList.Columns.Add("Macro", 180);
            _bindingsList.Columns.Add("Trigger", 300);

            var rightButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true, BackColor = _uiSettings.BackgroundColor };
            _removeBindingButton = new RoundedButton { Text = "Remove", Size = new Size(90, 28), BackColor = _uiSettings.DangerButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _toggleBindingButton = new RoundedButton { Text = "Enable/Disable", Size = new Size(120, 28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _clearAllButton = new RoundedButton { Text = "Clear All", Size = new Size(90, 28), BackColor = _uiSettings.SecondaryButtonColor, ForeColor = Color.White, CornerRadius = 6 };
            _enableMappingsCheck = new CheckBox { Text = "Enable mappings", Checked = true, AutoSize = true, ForeColor = _uiSettings.TextColor };
            rightButtons.Controls.Add(_enableMappingsCheck);
            rightButtons.Controls.Add(_removeBindingButton);
            rightButtons.Controls.Add(_toggleBindingButton);
            rightButtons.Controls.Add(_clearAllButton);

            middle.Controls.Add(_bindingsList, 0, 0);
            middle.Controls.Add(rightButtons, 1, 0);

            // Bottom: status
            _statusLabel = new Label { Text = "Ready", AutoSize = true, ForeColor = _uiSettings.TextColor };

            main.Controls.Add(top, 0, 0);
            main.Controls.Add(middle, 0, 1);
            main.Controls.Add(_statusLabel, 0, 2);

            Controls.Add(main);

            Shown += async (s, e) =>
            {
                await LoadMacrosAsync();
                await RefreshBindingsAsync();
                var list = await _mappingService.ListBindingsAsync();
                _enableMappingsCheck.Checked = _mappingService.Enabled;
            };

            FormClosing += (s, e) => StopListening();
        }

        private void WireEvents()
        {
            _recordTriggerButton.Click += (s, e) => ToggleListening();
            _addBindingButton.Click += async (s, e) => await AddBindingAsync();
            _removeBindingButton.Click += async (s, e) => await RemoveSelectedAsync();
            _toggleBindingButton.Click += async (s, e) => await ToggleSelectedAsync();
            _clearAllButton.Click += async (s, e) => await ClearAllAsync();
            _enableMappingsCheck.CheckedChanged += (s, e) => _mappingService.SetEnabled(_enableMappingsCheck.Checked);
            _mappingService.BindingsChanged += async (s, e) => await RefreshBindingsAsync();
        }

        private void ToggleListening()
        {
            if (_listening)
            {
                StopListening();
                return;
            }

            _currentTrigger = null;
            _triggerPreview.Text = "Listening... press a key or mouse button";
            _triggerPreview.ForeColor = _uiSettings.WarningColor;
            _listening = true;
            _keyboardHookService.KeyEvent += OnKey;
            _mouseHookService.MouseEvent += OnMouse;
        }

        private void StopListening()
        {
            if (!_listening) return;
            _listening = false;
            try
            {
                _keyboardHookService.KeyEvent -= OnKey;
                _mouseHookService.MouseEvent -= OnMouse;
            }
            catch { }
            if (_currentTrigger == null)
            {
                _triggerPreview.Text = "No trigger";
                _triggerPreview.ForeColor = _uiSettings.InactiveColor;
            }
        }

        private void OnKey(object? sender, KeyboardHookEventArgs e)
        {
            if (!_listening) return;
            try
            {
                var wantDown = _edgeDown.Checked;
                if ((wantDown && e.IsKeyDown) || (!wantDown && e.IsKeyUp))
                {
                    _currentTrigger = new MacroTrigger
                    {
                        Device = MacroTriggerDevice.Keyboard,
                        VirtualKeyCode = (int)e.VkCode,
                        Ctrl = e.Ctrl,
                        Alt = e.Alt,
                        Shift = e.Shift,
                        OnKeyDown = wantDown
                    };
                    _triggerPreview.Text = _currentTrigger.ToString();
                    _triggerPreview.ForeColor = _uiSettings.SuccessColor;
                    StopListening();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing keyboard trigger");
                StopListening();
            }
        }

        private void OnMouse(object? sender, MouseHookEventArgs e)
        {
            if (!_listening) return;
            try
            {
                var wantDown = _edgeDown.Checked;
                bool isDown = e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_LBUTTONDOWN ||
                              e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_RBUTTONDOWN ||
                              e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_MBUTTONDOWN;
                bool isUp = e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_LBUTTONUP ||
                            e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_RBUTTONUP ||
                            e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_MBUTTONUP;
                if ((wantDown && isDown) || (!wantDown && isUp))
                {
                    int? button = null;
                    if (e.LeftButton) button = 0;
                    else if (e.RightButton) button = 1;
                    else if (e.MiddleButton) button = 2;
                    if (!button.HasValue) return;
                    _currentTrigger = new MacroTrigger
                    {
                        Device = MacroTriggerDevice.Mouse,
                        Button = button,
                        OnButtonDown = wantDown
                    };
                    _triggerPreview.Text = _currentTrigger.ToString();
                    _triggerPreview.ForeColor = _uiSettings.SuccessColor;
                    StopListening();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing mouse trigger");
                StopListening();
            }
        }

        private async Task LoadMacrosAsync()
        {
            try
            {
                var names = await _macroService.ListAsync();
                _macroCombo.Items.Clear();
                foreach (var name in names)
                    _macroCombo.Items.Add(name);
                if (_macroCombo.Items.Count > 0)
                    _macroCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load macros for mapping");
            }
        }

        private async Task RefreshBindingsAsync()
        {
            try
            {
                var bindings = await _mappingService.ListBindingsAsync();
                _bindingsList.BeginUpdate();
                _bindingsList.Items.Clear();
                foreach (var b in bindings)
                {
                    var item = new ListViewItem(b.Enabled ? "Yes" : "No");
                    item.Tag = b;
                    item.SubItems.Add(b.MacroName);
                    item.SubItems.Add(b.Trigger?.ToString() ?? "");
                    _bindingsList.Items.Add(item);
                }
                _bindingsList.EndUpdate();
                _statusLabel.Text = $"Loaded {bindings.Count} bindings.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh bindings list");
            }
        }

        private async Task AddBindingAsync()
        {
            try
            {
                var macroName = _macroCombo.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(macroName))
                {
                    MessageBox.Show("Please select a macro.", "Macro Mapping", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (_currentTrigger == null)
                {
                    MessageBox.Show("Please record a trigger.", "Macro Mapping", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var binding = new MacroBinding
                {
                    MacroName = macroName,
                    Trigger = _currentTrigger,
                    Enabled = true
                };

                var ok = await _mappingService.AddOrUpdateBindingAsync(binding);
                if (ok)
                {
                    _statusLabel.Text = $"Mapped '{macroName}' to {_currentTrigger}";
                    await RefreshBindingsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add binding");
            }
        }

        private async Task RemoveSelectedAsync()
        {
            if (_bindingsList.SelectedItems.Count == 0) return;
            var b = _bindingsList.SelectedItems[0].Tag as MacroBinding;
            if (b == null) return;
            if (MessageBox.Show($"Remove mapping for '{b.MacroName}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await _mappingService.RemoveBindingAsync(b.Id);
                await RefreshBindingsAsync();
            }
        }

        private async Task ToggleSelectedAsync()
        {
            if (_bindingsList.SelectedItems.Count == 0) return;
            var b = _bindingsList.SelectedItems[0].Tag as MacroBinding;
            if (b == null) return;
            await _mappingService.EnableBindingAsync(b.Id, !b.Enabled);
            await RefreshBindingsAsync();
        }

        private async Task ClearAllAsync()
        {
            if (MessageBox.Show("Clear all mappings?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                await _mappingService.ClearAllBindingsAsync();
                await RefreshBindingsAsync();
            }
        }
    }
}
