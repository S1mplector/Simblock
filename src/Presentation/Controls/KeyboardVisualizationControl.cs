using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;
using SimBlock.Presentation.Configuration;

namespace SimBlock.Presentation.Controls
{
    /// <summary>
    /// Custom control that displays a mini keyboard diagram with color-coded keys
    /// </summary>
    public class KeyboardVisualizationControl : UserControl
    {
        private readonly UISettings _uiSettings;
        private BlockingMode _blockingMode = BlockingMode.Simple;
        private AdvancedKeyboardConfiguration? _advancedConfig;
        private bool _isBlocked = false;
        
        // Key layout definitions
        private readonly Dictionary<Keys, Rectangle> _keyLayout = new();
        private readonly Dictionary<Keys, string> _keyLabels = new();
        
        // Drawing constants
        private const int KeyWidth = 25;
        private const int KeyHeight = 25;
        private const int KeySpacing = 2;
        private const int RowHeight = KeyHeight + KeySpacing;
        
        // Color scheme
        private Color _blockedColor = Color.Red;
        private Color _allowedColor = Color.LightGreen;
        private Color _neutralColor = Color.LightGray;
        private Color _selectedColor;
        private Color _textColor = Color.Black;
        
        // Drag selection state
        private bool _isDragging = false;
        private bool _mouseDown = false;
        private Point _dragStartPoint;
        private Rectangle _selectionRectangle;
        private const int DragThreshold = 5; // Minimum pixels to move before starting drag

        // Events
        public event EventHandler<Keys>? KeyClicked;

        public KeyboardVisualizationControl(UISettings uiSettings)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            
            InitializeComponent();
            InitializeKeyLayout();
            InitializeColors();
        }

        private void InitializeComponent()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.DoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
            
            BackColor = Color.Transparent;
            Size = new Size(750, 220); // Increased height to prevent intersection
            
            // Enable mouse events
            this.MouseClick += OnMouseClick;
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            
            // Add tooltip for better user experience
            var tooltip = new ToolTip();
            tooltip.SetToolTip(this, "Keyboard blocking visualization - Click or drag to select keys");
        }

        private void InitializeKeyLayout()
        {
            _keyLayout.Clear();
            _keyLabels.Clear();
            
            int startX = 10;
            int startY = 10;
            
            // Row 1: ESC + Function keys with proper spacing
            int x = startX;
            int y = startY;
            
            // ESC key
            _keyLayout[Keys.Escape] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.Escape] = "Esc";
            x += KeyWidth + KeySpacing * 2; // Extra space after ESC
            
            // F1-F4 group
            for (int i = 1; i <= 4; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), $"F{i}");
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = $"F{i}";
                x += KeyWidth + KeySpacing;
            }
            x += KeySpacing; // Extra space between F4 and F5
            
            // F5-F8 group
            for (int i = 5; i <= 8; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), $"F{i}");
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = $"F{i}";
                x += KeyWidth + KeySpacing;
            }
            x += KeySpacing; // Extra space between F8 and F9
            
            // F9-F12 group
            for (int i = 9; i <= 12; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), $"F{i}");
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = $"F{i}";
                x += KeyWidth + KeySpacing;
            }
            
            // Row 2: Number row
            x = startX;
            y += RowHeight + 5; // Extra space after function keys
            
            // Backtick key
            _keyLayout[Keys.Oemtilde] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.Oemtilde] = "~";
            x += KeyWidth + KeySpacing;
            
            // Number keys 1-9
            for (int i = 1; i <= 9; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), $"D{i}");
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = i.ToString();
                x += KeyWidth + KeySpacing;
            }
            
            // 0 key
            _keyLayout[Keys.D0] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.D0] = "0";
            x += KeyWidth + KeySpacing;
            
            // Minus key
            _keyLayout[Keys.OemMinus] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.OemMinus] = "-";
            x += KeyWidth + KeySpacing;
            
            // Equals key
            _keyLayout[Keys.Oemplus] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.Oemplus] = "=";
            x += KeyWidth + KeySpacing;
            
            // Backspace key (wider)
            _keyLayout[Keys.Back] = new Rectangle(x, y, KeyWidth * 2, KeyHeight);
            _keyLabels[Keys.Back] = "Backspace";
            
            // Row 3: Tab + QWERTY row
            x = startX;
            y += RowHeight;
            
            // Tab key (wider)
            _keyLayout[Keys.Tab] = new Rectangle(x, y, KeyWidth + 15, KeyHeight);
            _keyLabels[Keys.Tab] = "Tab";
            x += KeyWidth + 15 + KeySpacing;
            
            // QWERTY row
            string qwertyRow = "QWERTYUIOP";
            for (int i = 0; i < qwertyRow.Length; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), qwertyRow[i].ToString());
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = qwertyRow[i].ToString();
                x += KeyWidth + KeySpacing;
            }
            
            // [ key
            _keyLayout[Keys.OemOpenBrackets] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.OemOpenBrackets] = "[";
            x += KeyWidth + KeySpacing;
            
            // ] key
            _keyLayout[Keys.OemCloseBrackets] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.OemCloseBrackets] = "]";
            x += KeyWidth + KeySpacing;
            
            // \ key
            _keyLayout[Keys.OemBackslash] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.OemBackslash] = "\\";
            
            // Row 4: Caps Lock + ASDF row
            x = startX;
            y += RowHeight;
            
            // Caps Lock key (wider)
            _keyLayout[Keys.CapsLock] = new Rectangle(x, y, KeyWidth + 20, KeyHeight);
            _keyLabels[Keys.CapsLock] = "Caps Lock";
            x += KeyWidth + 20 + KeySpacing;
            
            // ASDF row
            string asdfRow = "ASDFGHJKL";
            for (int i = 0; i < asdfRow.Length; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), asdfRow[i].ToString());
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = asdfRow[i].ToString();
                x += KeyWidth + KeySpacing;
            }
            
            // ; key
            _keyLayout[Keys.OemSemicolon] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.OemSemicolon] = ";";
            x += KeyWidth + KeySpacing;
            
            // ' key
            _keyLayout[Keys.OemQuotes] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.OemQuotes] = "'";
            x += KeyWidth + KeySpacing;
            
            // Enter key (wider)
            _keyLayout[Keys.Enter] = new Rectangle(x, y, KeyWidth + 25, KeyHeight);
            _keyLabels[Keys.Enter] = "Enter";
            
            // Row 5: Shift + ZXCV row
            x = startX;
            y += RowHeight;
            
            // Left Shift key (wider)
            _keyLayout[Keys.LShiftKey] = new Rectangle(x, y, KeyWidth + 30, KeyHeight);
            _keyLabels[Keys.LShiftKey] = "Shift";
            x += KeyWidth + 30 + KeySpacing;
            
            // ZXCV row
            string zxcvRow = "ZXCVBNM";
            for (int i = 0; i < zxcvRow.Length; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), zxcvRow[i].ToString());
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = zxcvRow[i].ToString();
                x += KeyWidth + KeySpacing;
            }
            
            // , key
            _keyLayout[Keys.Oemcomma] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.Oemcomma] = ",";
            x += KeyWidth + KeySpacing;
            
            // . key
            _keyLayout[Keys.OemPeriod] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.OemPeriod] = ".";
            x += KeyWidth + KeySpacing;
            
            // / key
            _keyLayout[Keys.OemQuestion] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.OemQuestion] = "/";
            x += KeyWidth + KeySpacing;
            
            // Right Shift key (wider)
            _keyLayout[Keys.RShiftKey] = new Rectangle(x, y, KeyWidth + 30, KeyHeight);
            _keyLabels[Keys.RShiftKey] = "Shift";
            
            // Row 6: Bottom row with modifiers
            x = startX;
            y += RowHeight;
            
            // Left Ctrl
            _keyLayout[Keys.LControlKey] = new Rectangle(x, y, KeyWidth + 10, KeyHeight);
            _keyLabels[Keys.LControlKey] = "Ctrl";
            x += KeyWidth + 10 + KeySpacing;
            
            // Left Win
            _keyLayout[Keys.LWin] = new Rectangle(x, y, KeyWidth + 10, KeyHeight);
            _keyLabels[Keys.LWin] = "Win";
            x += KeyWidth + 10 + KeySpacing;
            
            // Left Alt
            _keyLayout[Keys.LMenu] = new Rectangle(x, y, KeyWidth + 10, KeyHeight);
            _keyLabels[Keys.LMenu] = "Alt";
            x += KeyWidth + 10 + KeySpacing;
            
            // Space bar (much wider)
            _keyLayout[Keys.Space] = new Rectangle(x, y, KeyWidth * 6, KeyHeight);
            _keyLabels[Keys.Space] = "Space";
            x += KeyWidth * 6 + KeySpacing;
            
            // Right Alt
            _keyLayout[Keys.RMenu] = new Rectangle(x, y, KeyWidth + 10, KeyHeight);
            _keyLabels[Keys.RMenu] = "Alt";
            x += KeyWidth + 10 + KeySpacing;
            
            // Right Win
            _keyLayout[Keys.RWin] = new Rectangle(x, y, KeyWidth + 10, KeyHeight);
            _keyLabels[Keys.RWin] = "Win";
            x += KeyWidth + 10 + KeySpacing;
            
            // Menu key
            _keyLayout[Keys.Apps] = new Rectangle(x, y, KeyWidth + 10, KeyHeight);
            _keyLabels[Keys.Apps] = "Menu";
            x += KeyWidth + 10 + KeySpacing;
            
            // Right Ctrl
            _keyLayout[Keys.RControlKey] = new Rectangle(x, y, KeyWidth + 10, KeyHeight);
            _keyLabels[Keys.RControlKey] = "Ctrl";
            
            // Arrow keys (positioned to the right of the main keyboard)
            int arrowX = startX + KeyWidth * 16; // Position to the right, moved left
            int arrowY = y - RowHeight; // One row above the bottom row
            
            // Up arrow (centered above left/down/right)
            _keyLayout[Keys.Up] = new Rectangle(arrowX + KeyWidth + KeySpacing, arrowY, KeyWidth, KeyHeight);
            _keyLabels[Keys.Up] = "↑";
            
            // Left, Down, Right arrows (bottom row)
            _keyLayout[Keys.Left] = new Rectangle(arrowX, arrowY + RowHeight, KeyWidth, KeyHeight);
            _keyLabels[Keys.Left] = "←";
            
            _keyLayout[Keys.Down] = new Rectangle(arrowX + KeyWidth + KeySpacing, arrowY + RowHeight, KeyWidth, KeyHeight);
            _keyLabels[Keys.Down] = "↓";
            
            _keyLayout[Keys.Right] = new Rectangle(arrowX + (KeyWidth + KeySpacing) * 2, arrowY + RowHeight, KeyWidth, KeyHeight);
            _keyLabels[Keys.Right] = "→";
        }


        private void InitializeColors()
        {
            // Use theme colors if available
            _blockedColor = _uiSettings.ErrorColor;
            _allowedColor = _uiSettings.SuccessColor;
            _neutralColor = _uiSettings.BackgroundColor;
            _selectedColor = _uiSettings.SelectedColor; // Selected state color
            _textColor = _uiSettings.TextColor;
        }

        /// <summary>
        /// Updates the visualization with current blocking state
        /// </summary>
        public void UpdateVisualization(BlockingMode mode, AdvancedKeyboardConfiguration? config, bool isBlocked)
        {
            System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.UpdateVisualization: Mode={mode}, Config={config != null}, IsBlocked={isBlocked}");
            if (config != null)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.UpdateVisualization: Config has {config.SelectedKeys.Count} selected keys");
            }
            
            _blockingMode = mode;
            _advancedConfig = config;
            _isBlocked = isBlocked;
            
            Invalidate(); // Trigger repaint
            System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.UpdateVisualization: Invalidated display");
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Draw each key
            foreach (var kvp in _keyLayout)
            {
                var key = kvp.Key;
                var rect = kvp.Value;
                var label = _keyLabels.ContainsKey(key) ? _keyLabels[key] : key.ToString();
                
                // Determine key color based on blocking state
                Color keyColor = GetKeyColor(key);
                
                // Draw key background
                using (var brush = new SolidBrush(keyColor))
                {
                    g.FillRectangle(brush, rect);
                }
                
                // Draw key border
                using (var pen = new Pen(_textColor, 1))
                {
                    g.DrawRectangle(pen, rect);
                }
                
                // Draw key label
                using (var font = new Font("Arial", 6, FontStyle.Bold))
                using (var brush = new SolidBrush(_textColor))
                {
                    var textSize = g.MeasureString(label, font);
                    var textX = rect.X + (rect.Width - textSize.Width) / 2;
                    var textY = rect.Y + (rect.Height - textSize.Height) / 2;
                    
                    g.DrawString(label, font, brush, textX, textY);
                }
            }
            
            // Draw drag selection rectangle if dragging
            if (_isDragging && !_selectionRectangle.IsEmpty)
            {
                using (var brush = new SolidBrush(Color.FromArgb(50, 0, 120, 215))) // Semi-transparent blue
                using (var pen = new Pen(Color.FromArgb(100, 0, 120, 215), 1)) // Blue border
                {
                    g.FillRectangle(brush, _selectionRectangle);
                    g.DrawRectangle(pen, _selectionRectangle);
                }
            }
            
            // Legend moved to bottom of visualization group box in SettingsForm
            // DrawLegend(g);
        }

        private Color GetKeyColor(Keys key)
        {
            if (_blockingMode == BlockingMode.Select && _advancedConfig != null)
            {
                // In select mode, show selected keys in orange
                bool isSelected = _advancedConfig.IsKeySelected(key);
                Color color = isSelected ? _selectedColor : _neutralColor;
                System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.GetKeyColor: Key={key}, Selected={isSelected}, Color={color}, SelectedColor={_selectedColor}");
                return color;
            }
            
            if (!_isBlocked)
            {
                return _neutralColor;
            }
            
            if (_blockingMode == BlockingMode.Simple)
            {
                // In simple mode, all keys are blocked
                return _blockedColor;
            }
            
            if (_advancedConfig != null)
            {
                // In advanced mode, check if this specific key is blocked
                bool isKeyBlocked = _advancedConfig.IsKeyBlocked(key);
                return isKeyBlocked ? _blockedColor : _allowedColor;
            }
            
            return _neutralColor;
        }

        private void DrawLegend(Graphics g)
        {
            int legendY = Height - 18; // Positioned to avoid intersection with keyboard while leaving space for text
            int legendX = 10;
            
            // Draw legend items
            DrawLegendItem(g, legendX, legendY, _blockedColor, "Blocked");
            DrawLegendItem(g, legendX + 80, legendY, _allowedColor, "Allowed");
            DrawLegendItem(g, legendX + 160, legendY, _neutralColor, "Inactive");
            
            // Show selected color in Select mode
            if (_blockingMode == BlockingMode.Select)
            {
                DrawLegendItem(g, legendX + 240, legendY, _selectedColor, "Selected");
            }
            
            // Remove redundant mode indicator - it's displayed elsewhere and not needed here
        }

        private void DrawLegendItem(Graphics g, int x, int y, Color color, string text)
        {
            // Draw color square
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x, y, 12, 12);
            }
            
            using (var pen = new Pen(_textColor, 1))
            {
                g.DrawRectangle(pen, x, y, 12, 12);
            }
            
            // Draw label
            using (var font = new Font("Arial", 8))
            using (var brush = new SolidBrush(_textColor))
            {
                g.DrawString(text, font, brush, x + 15, y - 2);
            }
        }

        /// <summary>
        /// Gets a summary of the current blocking state
        /// </summary>
        public string GetBlockingSummary()
        {
            if (!_isBlocked)
                return "No keys are currently blocked";
            
            if (_blockingMode == BlockingMode.Simple)
                return "All keyboard input is blocked";
            
            if (_advancedConfig != null)
            {
                int blockedCount = 0;
                int totalCount = _keyLayout.Count;
                
                foreach (var key in _keyLayout.Keys)
                {
                    if (_advancedConfig.IsKeyBlocked(key))
                        blockedCount++;
                }
                
                return $"{blockedCount} out of {totalCount} keys are blocked";
            }
            
            return "Blocking state unknown";
        }

        /// <summary>
        /// Handles mouse click events on the keyboard visualization
        /// </summary>
        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.OnMouseClick: Mode={_blockingMode}, Location={e.Location}, Config={_advancedConfig != null}");
            
            // Only handle clicks in Select mode
            if (_blockingMode != BlockingMode.Select || _advancedConfig == null)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.OnMouseClick: Early return - Mode={_blockingMode}, Config={_advancedConfig != null}");
                return;
            }

            // Find which key was clicked
            foreach (var kvp in _keyLayout)
            {
                var key = kvp.Key;
                var rect = kvp.Value;
                
                if (rect.Contains(e.Location))
                {
                    System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.OnMouseClick: Found key {key} at {rect}");
                    
                    // Toggle selection for this key
                    _advancedConfig.ToggleKeySelection(key);
                    System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.OnMouseClick: Toggled selection for {key}");
                    
                    // Raise the KeyClicked event
                    KeyClicked?.Invoke(this, key);
                    System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.OnMouseClick: Raised KeyClicked event for {key}");
                    
                    // Refresh the display
                    Invalidate();
                    System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.OnMouseClick: Invalidated display");
                    break;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"KeyboardVisualizationControl.OnMouseClick: No key found at location {e.Location}");
        }

        /// <summary>
        /// Handles mouse down events for drag selection
        /// </summary>
        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (_blockingMode != BlockingMode.Select || _advancedConfig == null || e.Button != MouseButtons.Left)
                return;

            _mouseDown = true;
            _dragStartPoint = e.Location;
            _selectionRectangle = Rectangle.Empty;
            // Don't set _isDragging = true yet - wait for movement threshold
        }

        /// <summary>
        /// Handles mouse move events for drag selection
        /// </summary>
        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_mouseDown)
                return;

            // Check if we've moved beyond the drag threshold
            int deltaX = Math.Abs(e.X - _dragStartPoint.X);
            int deltaY = Math.Abs(e.Y - _dragStartPoint.Y);
            
            if (!_isDragging && (deltaX > DragThreshold || deltaY > DragThreshold))
            {
                // Start dragging only after threshold is exceeded
                _isDragging = true;
            }
            
            if (_isDragging)
            {
                // Calculate selection rectangle
                int x = Math.Min(_dragStartPoint.X, e.X);
                int y = Math.Min(_dragStartPoint.Y, e.Y);
                int width = Math.Abs(e.X - _dragStartPoint.X);
                int height = Math.Abs(e.Y - _dragStartPoint.Y);
                
                _selectionRectangle = new Rectangle(x, y, width, height);
                Invalidate(); // Trigger repaint to show selection rectangle
            }
        }

        /// <summary>
        /// Handles mouse up events for drag selection
        /// </summary>
        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (!_mouseDown)
                return;

            // Only process drag selection if we actually started dragging
            if (_isDragging && !_selectionRectangle.IsEmpty)
            {
                foreach (var kvp in _keyLayout)
                {
                    var key = kvp.Key;
                    var keyRect = kvp.Value;
                    
                    if (_selectionRectangle.IntersectsWith(keyRect))
                    {
                        _advancedConfig?.ToggleKeySelection(key);
                    }
                }
                
                // Raise event to notify of selection changes
                KeyClicked?.Invoke(this, Keys.None);
                Invalidate(); // Clear selection rectangle
            }

            // Reset all drag states
            _mouseDown = false;
            _isDragging = false;
            _selectionRectangle = Rectangle.Empty;
        }
    }
}