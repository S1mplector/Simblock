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
    /// Custom control that displays a mouse diagram with color-coded buttons and actions
    /// </summary>
    public class MouseVisualizationControl : UserControl
    {
        private readonly UISettings _uiSettings;
        private BlockingMode _blockingMode = BlockingMode.Simple;
        private AdvancedMouseConfiguration? _advancedConfig;
        private bool _isBlocked = false;
        
        // Mouse component definitions
        private readonly Dictionary<string, Rectangle> _mouseComponents = new();
        private readonly Dictionary<string, string> _componentLabels = new();
        
        // Drawing constants
        private const int MouseWidth = 80;
        private const int MouseHeight = 120;
        private const int ButtonHeight = 25;
        private const int WheelSize = 20;
        
        // Color scheme
        private Color _blockedColor = Color.Red;
        private Color _allowedColor = Color.LightGreen;
        private Color _neutralColor = Color.LightGray;
        private Color _textColor = Color.Black;
        private Color _mouseBodyColor = Color.Silver;

        public MouseVisualizationControl(UISettings uiSettings)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            
            InitializeComponent();
            InitializeMouseLayout();
            InitializeColors();
        }

        private void InitializeComponent()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | 
                    ControlStyles.ResizeRedraw, true);
            
            BackColor = Color.Transparent;
            Size = new Size(200, 200);
            
            // Add tooltip for better user experience
            var tooltip = new ToolTip();
            tooltip.SetToolTip(this, "Mouse blocking visualization - Red components are blocked, Green are allowed");
        }

        private void InitializeMouseLayout()
        {
            int centerX = Width / 2;
            int startY = 20;
            
            // Main mouse body outline (for reference)
            int mouseX = centerX - MouseWidth / 2;
            _mouseComponents["MouseBody"] = new Rectangle(mouseX, startY, MouseWidth, MouseHeight);
            
            // Left mouse button
            _mouseComponents["LeftButton"] = new Rectangle(mouseX + 5, startY + 5, MouseWidth / 2 - 10, ButtonHeight);
            _componentLabels["LeftButton"] = "L";
            
            // Right mouse button
            _mouseComponents["RightButton"] = new Rectangle(mouseX + MouseWidth / 2 + 5, startY + 5, MouseWidth / 2 - 10, ButtonHeight);
            _componentLabels["RightButton"] = "R";
            
            // Middle button / Wheel
            _mouseComponents["MiddleButton"] = new Rectangle(centerX - WheelSize / 2, startY + ButtonHeight + 5, WheelSize, WheelSize);
            _componentLabels["MiddleButton"] = "M";
            
            // Mouse wheel (visual representation)
            _mouseComponents["MouseWheel"] = new Rectangle(centerX - WheelSize / 2, startY + ButtonHeight + 5, WheelSize, WheelSize);
            _componentLabels["MouseWheel"] = "⚙";
            
            // X1 Button (side button)
            _mouseComponents["X1Button"] = new Rectangle(mouseX - 15, startY + 40, 12, 20);
            _componentLabels["X1Button"] = "X1";
            
            // X2 Button (side button)
            _mouseComponents["X2Button"] = new Rectangle(mouseX - 15, startY + 65, 12, 20);
            _componentLabels["X2Button"] = "X2";
            
            // Mouse Movement indicator (bottom area)
            _mouseComponents["MouseMovement"] = new Rectangle(mouseX + 10, startY + MouseHeight - 25, MouseWidth - 20, 20);
            _componentLabels["MouseMovement"] = "Movement";
            
            // Double click indicator (overlaid on left button)
            _mouseComponents["DoubleClick"] = new Rectangle(mouseX + 5, startY + 5, MouseWidth / 2 - 10, ButtonHeight);
            _componentLabels["DoubleClick"] = "2x";
        }

        private void InitializeColors()
        {
            // Use theme colors if available
            _blockedColor = _uiSettings.ErrorColor;
            _allowedColor = _uiSettings.SuccessColor;
            _neutralColor = _uiSettings.BackgroundColor;
            _textColor = _uiSettings.TextColor;
            _mouseBodyColor = Color.FromArgb(200, 200, 200);
        }

        /// <summary>
        /// Updates the visualization with current blocking state
        /// </summary>
        public void UpdateVisualization(BlockingMode mode, AdvancedMouseConfiguration? config, bool isBlocked)
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
            
            // Draw mouse body outline
            DrawMouseBody(g);
            
            // Draw mouse components
            DrawMouseComponents(g);
            
            // Draw legend
            DrawLegend(g);
        }

        private void DrawMouseBody(Graphics g)
        {
            var mouseBody = _mouseComponents["MouseBody"];
            
            // Draw mouse body with rounded corners
            using (var brush = new SolidBrush(_mouseBodyColor))
            using (var pen = new Pen(_textColor, 2))
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                int radius = 15;
                
                path.AddArc(mouseBody.X, mouseBody.Y, radius, radius, 180, 90);
                path.AddArc(mouseBody.Right - radius, mouseBody.Y, radius, radius, 270, 90);
                path.AddArc(mouseBody.Right - radius, mouseBody.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(mouseBody.X, mouseBody.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
        }

        private void DrawMouseComponents(Graphics g)
        {
            // Draw each mouse component
            foreach (var kvp in _mouseComponents)
            {
                var component = kvp.Key;
                var rect = kvp.Value;
                
                // Skip the mouse body as it's drawn separately
                if (component == "MouseBody")
                    continue;
                
                // Don't draw double click separately if it overlaps with left button
                if (component == "DoubleClick" && !ShouldDrawDoubleClick())
                    continue;
                
                var label = _componentLabels.ContainsKey(component) ? _componentLabels[component] : component;
                
                // Determine component color based on blocking state
                Color componentColor = GetComponentColor(component);
                
                // Draw component background
                using (var brush = new SolidBrush(componentColor))
                {
                    if (component == "MouseWheel" || component == "MiddleButton")
                    {
                        g.FillEllipse(brush, rect);
                    }
                    else
                    {
                        g.FillRectangle(brush, rect);
                    }
                }
                
                // Draw component border
                using (var pen = new Pen(_textColor, 1))
                {
                    if (component == "MouseWheel" || component == "MiddleButton")
                    {
                        g.DrawEllipse(pen, rect);
                    }
                    else
                    {
                        g.DrawRectangle(pen, rect);
                    }
                }
                
                // Draw component label
                DrawComponentLabel(g, rect, label, component);
            }
        }

        private bool ShouldDrawDoubleClick()
        {
            // Only draw double click indicator if it's specifically blocked in advanced mode
            return _blockingMode == BlockingMode.Advanced && 
                   _advancedConfig != null && 
                   _advancedConfig.BlockDoubleClick &&
                   !_advancedConfig.BlockLeftButton; // Don't draw if left button is already blocked
        }

        private void DrawComponentLabel(Graphics g, Rectangle rect, string label, string component)
        {
            using (var font = new Font("Arial", component == "MouseWheel" ? 10 : 6, FontStyle.Bold))
            using (var brush = new SolidBrush(_textColor))
            {
                var textSize = g.MeasureString(label, font);
                float textX = rect.X + (rect.Width - textSize.Width) / 2;
                float textY = rect.Y + (rect.Height - textSize.Height) / 2;
                
                // Special positioning for certain components
                if (component == "MouseMovement")
                {
                    textY = rect.Y + 2;
                }
                else if (component == "DoubleClick")
                {
                    textX = rect.Right - textSize.Width - 2;
                    textY = rect.Y + 2;
                }
                
                g.DrawString(label, font, brush, textX, textY);
            }
        }

        private Color GetComponentColor(string component)
        {
            if (!_isBlocked)
            {
                return _neutralColor;
            }
            
            if (_blockingMode == BlockingMode.Simple)
            {
                // In simple mode, all mouse actions are blocked
                return _blockedColor;
            }
            
            if (_advancedConfig != null)
            {
                // In advanced mode, check if this specific component is blocked
                bool isComponentBlocked = component switch
                {
                    "LeftButton" => _advancedConfig.BlockLeftButton,
                    "RightButton" => _advancedConfig.BlockRightButton,
                    "MiddleButton" => _advancedConfig.BlockMiddleButton,
                    "X1Button" => _advancedConfig.BlockX1Button,
                    "X2Button" => _advancedConfig.BlockX2Button,
                    "MouseWheel" => _advancedConfig.BlockMouseWheel,
                    "MouseMovement" => _advancedConfig.BlockMouseMovement,
                    "DoubleClick" => _advancedConfig.BlockDoubleClick,
                    _ => false
                };
                
                return isComponentBlocked ? _blockedColor : _allowedColor;
            }
            
            return _neutralColor;
        }

        private void DrawLegend(Graphics g)
        {
            int legendY = Height - 35;
            int legendX = 10;
            
            // Draw legend items
            DrawLegendItem(g, legendX, legendY, _blockedColor, "Blocked");
            DrawLegendItem(g, legendX, legendY + 15, _allowedColor, "Allowed");
            DrawLegendItem(g, legendX + 80, legendY, _neutralColor, "Inactive");
            
            // Draw mode indicator
            using (var font = new Font("Arial", 8, FontStyle.Bold))
            using (var brush = new SolidBrush(_textColor))
            {
                string modeText = $"Mode: {_blockingMode}";
                g.DrawString(modeText, font, brush, legendX + 80, legendY + 15);
            }
        }

        private void DrawLegendItem(Graphics g, int x, int y, Color color, string text)
        {
            // Draw color square
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x, y, 10, 10);
            }
            
            using (var pen = new Pen(_textColor, 1))
            {
                g.DrawRectangle(pen, x, y, 10, 10);
            }
            
            // Draw label
            using (var font = new Font("Arial", 7))
            using (var brush = new SolidBrush(_textColor))
            {
                g.DrawString(text, font, brush, x + 12, y - 1);
            }
        }

        /// <summary>
        /// Gets a summary of the current blocking state
        /// </summary>
        public string GetBlockingSummary()
        {
            if (!_isBlocked)
                return "No mouse actions are currently blocked";
            
            if (_blockingMode == BlockingMode.Simple)
                return "All mouse input is blocked";
            
            if (_advancedConfig != null)
            {
                var blockedActions = new List<string>();
                
                if (_advancedConfig.BlockLeftButton) blockedActions.Add("Left Button");
                if (_advancedConfig.BlockRightButton) blockedActions.Add("Right Button");
                if (_advancedConfig.BlockMiddleButton) blockedActions.Add("Middle Button");
                if (_advancedConfig.BlockX1Button) blockedActions.Add("X1 Button");
                if (_advancedConfig.BlockX2Button) blockedActions.Add("X2 Button");
                if (_advancedConfig.BlockMouseWheel) blockedActions.Add("Mouse Wheel");
                if (_advancedConfig.BlockMouseMovement) blockedActions.Add("Mouse Movement");
                if (_advancedConfig.BlockDoubleClick) blockedActions.Add("Double Click");
                
                if (blockedActions.Count == 0)
                    return "No mouse actions are blocked";
                
                return $"Blocked: {string.Join(", ", blockedActions)}";
            }
            
            return "Blocking state unknown";
        }
    }
}