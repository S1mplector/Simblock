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
        private Color _textColor = Color.Black;

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
            Size = new Size(600, 150);
            
            // Add tooltip for better user experience
            var tooltip = new ToolTip();
            tooltip.SetToolTip(this, "Keyboard blocking visualization - Red keys are blocked, Green keys are allowed");
        }

        private void InitializeKeyLayout()
        {
            // Function keys row (F1-F12)
            int x = 10;
            int y = 10;
            
            for (int i = 1; i <= 12; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), $"F{i}");
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = $"F{i}";
                x += KeyWidth + KeySpacing;
            }
            
            // Number row (1-9, 0)
            x = 10;
            y += RowHeight;
            
            for (int i = 1; i <= 9; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), $"D{i}");
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = i.ToString();
                x += KeyWidth + KeySpacing;
            }
            
            // Add 0 key
            _keyLayout[Keys.D0] = new Rectangle(x, y, KeyWidth, KeyHeight);
            _keyLabels[Keys.D0] = "0";
            
            // QWERTY row
            x = 10;
            y += RowHeight;
            string qwertyRow = "QWERTYUIOP";
            
            for (int i = 0; i < qwertyRow.Length; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), qwertyRow[i].ToString());
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = qwertyRow[i].ToString();
                x += KeyWidth + KeySpacing;
            }
            
            // ASDF row
            x = 25; // Slight offset for typical keyboard layout
            y += RowHeight;
            string asdfRow = "ASDFGHJKL";
            
            for (int i = 0; i < asdfRow.Length; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), asdfRow[i].ToString());
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = asdfRow[i].ToString();
                x += KeyWidth + KeySpacing;
            }
            
            // ZXCV row
            x = 40; // Larger offset for typical keyboard layout
            y += RowHeight;
            string zxcvRow = "ZXCVBNM";
            
            for (int i = 0; i < zxcvRow.Length; i++)
            {
                var key = (Keys)Enum.Parse(typeof(Keys), zxcvRow[i].ToString());
                _keyLayout[key] = new Rectangle(x, y, KeyWidth, KeyHeight);
                _keyLabels[key] = zxcvRow[i].ToString();
                x += KeyWidth + KeySpacing;
            }
            
            // Add special keys
            AddSpecialKeys();
        }

        private void AddSpecialKeys()
        {
            int bottomRowY = 10 + (RowHeight * 5);
            int currentX = 10;
            
            // Bottom row modifier keys (left to right)
            // Left Control
            _keyLayout[Keys.LControlKey] = new Rectangle(currentX, bottomRowY, KeyWidth + 8, KeyHeight);
            _keyLabels[Keys.LControlKey] = "Ctrl";
            currentX += KeyWidth + 8 + KeySpacing;
            
            // Left Alt
            _keyLayout[Keys.LMenu] = new Rectangle(currentX, bottomRowY, KeyWidth + 8, KeyHeight);
            _keyLabels[Keys.LMenu] = "Alt";
            currentX += KeyWidth + 8 + KeySpacing;
            
            // Space bar (larger)
            _keyLayout[Keys.Space] = new Rectangle(currentX, bottomRowY, KeyWidth * 5, KeyHeight);
            _keyLabels[Keys.Space] = "Space";
            currentX += KeyWidth * 5 + KeySpacing;
            
            // Right Alt (Alt Gr)
            _keyLayout[Keys.RMenu] = new Rectangle(currentX, bottomRowY, KeyWidth + 8, KeyHeight);
            _keyLabels[Keys.RMenu] = "Alt Gr";
            currentX += KeyWidth + 8 + KeySpacing;
            
            // Windows key (Right Windows)
            _keyLayout[Keys.RWin] = new Rectangle(currentX, bottomRowY, KeyWidth + 8, KeyHeight);
            _keyLabels[Keys.RWin] = "Win";
            currentX += KeyWidth + 8 + KeySpacing;
            
            // Right Control
            _keyLayout[Keys.RControlKey] = new Rectangle(currentX, bottomRowY, KeyWidth + 8, KeyHeight);
            _keyLabels[Keys.RControlKey] = "Ctrl";
            currentX += KeyWidth + 8 + KeySpacing * 3; // Extra spacing before arrows
            
            // Arrow keys (positioned to the right with proper spacing)
            int arrowX = currentX;
            int arrowY = 10 + (RowHeight * 4);
            
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
            
            // Shift key (left side of ZXCV row)
            _keyLayout[Keys.LShiftKey] = new Rectangle(10, 10 + (RowHeight * 4), KeyWidth + 8, KeyHeight);
            _keyLabels[Keys.LShiftKey] = "Shift";
            
            // Add common keys that might be missing
            // Tab key (left of QWERTY row)
            _keyLayout[Keys.Tab] = new Rectangle(10, 10 + (RowHeight * 2), KeyWidth + 8, KeyHeight);
            _keyLabels[Keys.Tab] = "Tab";
            
            // Caps Lock (left of ASDF row)
            _keyLayout[Keys.CapsLock] = new Rectangle(10, 10 + (RowHeight * 3), KeyWidth + 8, KeyHeight);
            _keyLabels[Keys.CapsLock] = "Caps";
            
            // Enter key (right of ASDF row)
            _keyLayout[Keys.Enter] = new Rectangle(25 + (KeyWidth + KeySpacing) * 9, 10 + (RowHeight * 3), KeyWidth + 8, KeyHeight);
            _keyLabels[Keys.Enter] = "Enter";
            
            // Backspace (right of number row)
            _keyLayout[Keys.Back] = new Rectangle(10 + (KeyWidth + KeySpacing) * 10, 10 + RowHeight, KeyWidth + 8, KeyHeight);
            _keyLabels[Keys.Back] = "⌫";
        }

        private void InitializeColors()
        {
            // Use theme colors if available
            _blockedColor = _uiSettings.ErrorColor;
            _allowedColor = _uiSettings.SuccessColor;
            _neutralColor = _uiSettings.BackgroundColor;
            _textColor = _uiSettings.TextColor;
        }

        /// <summary>
        /// Updates the visualization with current blocking state
        /// </summary>
        public void UpdateVisualization(BlockingMode mode, AdvancedKeyboardConfiguration? config, bool isBlocked)
        {
            _blockingMode = mode;
            _advancedConfig = config;
            _isBlocked = isBlocked;
            
            Invalidate(); // Trigger repaint
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
            
            // Draw legend
            DrawLegend(g);
        }

        private Color GetKeyColor(Keys key)
        {
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
            int legendY = Height - 30;
            int legendX = 10;
            
            // Draw legend items
            DrawLegendItem(g, legendX, legendY, _blockedColor, "Blocked");
            DrawLegendItem(g, legendX + 80, legendY, _allowedColor, "Allowed");
            DrawLegendItem(g, legendX + 160, legendY, _neutralColor, "Inactive");
            
            // Draw mode indicator
            using (var font = new Font("Arial", 8, FontStyle.Bold))
            using (var brush = new SolidBrush(_textColor))
            {
                string modeText = $"Mode: {_blockingMode}";
                g.DrawString(modeText, font, brush, legendX + 250, legendY + 5);
            }
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
    }
}