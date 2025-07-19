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
        private const int MouseWidth = 100;
        private const int MouseHeight = 140;
        private const int ButtonHeight = 30;
        private const int WheelSize = 16;
        
        // Color scheme
        private Color _blockedColor = Color.Red;
        private Color _allowedColor = Color.LightGreen;
        private Color _neutralColor = Color.LightGray;
        private Color _selectedColor;
        private Color _textColor = Color.Black;
        private Color _mouseBodyColor = Color.FromArgb(220, 220, 220);
        private Color _mouseBodyShadow = Color.FromArgb(180, 180, 180);

        // Events
        public event EventHandler<string>? ComponentClicked;

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
            Size = new Size(220, 220);
            
            // Enable mouse events
            this.MouseClick += OnMouseClick;
            
            // Add tooltip for better user experience
            var tooltip = new ToolTip();
            tooltip.SetToolTip(this, "Mouse blocking visualization - Red components are blocked, Green are allowed, Orange are selected");
        }

        private void InitializeMouseLayout()
        {
            int centerX = Width / 2;
            int startY = 25;
            
            // Main mouse body outline (for reference)
            int mouseX = centerX - MouseWidth / 2;
            _mouseComponents["MouseBody"] = new Rectangle(mouseX, startY, MouseWidth, MouseHeight);
            
            // Left mouse button - more realistic shape
            _mouseComponents["LeftButton"] = new Rectangle(mouseX + 8, startY + 8, MouseWidth / 2 - 12, ButtonHeight);
            _componentLabels["LeftButton"] = "LEFT";
            
            // Right mouse button - more realistic shape
            _mouseComponents["RightButton"] = new Rectangle(mouseX + MouseWidth / 2 + 4, startY + 8, MouseWidth / 2 - 12, ButtonHeight);
            _componentLabels["RightButton"] = "RIGHT";
            
            // Unified wheel/middle button indicator between left and right buttons
            _mouseComponents["WheelMiddle"] = new Rectangle(centerX - WheelSize / 2, startY + ButtonHeight + 8, WheelSize, WheelSize);
            _componentLabels["WheelMiddle"] = "MID";
            
            // X1 Button (side button) - positioned inside mouse body
            _mouseComponents["X1Button"] = new Rectangle(mouseX + 5, startY + 50, 14, 25);
            _componentLabels["X1Button"] = "X1";
            
            // X2 Button (side button) - positioned inside mouse body
            _mouseComponents["X2Button"] = new Rectangle(mouseX + 5, startY + 80, 14, 25);
            _componentLabels["X2Button"] = "X2";
            
            // Mouse Sensor indicator (bottom area) - renamed from Movement
            _mouseComponents["MouseSensor"] = new Rectangle(mouseX + 15, startY + MouseHeight - 30, MouseWidth - 30, 22);
            _componentLabels["MouseSensor"] = "SENSOR";
            
            // Double click indicator (overlaid on left button) - better positioned
            _mouseComponents["DoubleClick"] = new Rectangle(mouseX + 8, startY + 8, MouseWidth / 2 - 12, ButtonHeight);
            _componentLabels["DoubleClick"] = "2×";
        }

        private void InitializeColors()
        {
            // Use theme colors if available
            _blockedColor = _uiSettings.ErrorColor;
            _allowedColor = _uiSettings.SuccessColor;
            _neutralColor = _uiSettings.BackgroundColor;
            _selectedColor = _uiSettings.SelectedColor; // Selected state color
            _textColor = _uiSettings.TextColor;
            _mouseBodyColor = Color.FromArgb(220, 220, 220);
            _mouseBodyShadow = Color.FromArgb(180, 180, 180);
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
            
            // Legend removed per user request
            // DrawLegend(g);
        }

        private void DrawMouseBody(Graphics g)
        {
            var mouseBody = _mouseComponents["MouseBody"];
            
            // Draw mouse body shadow first (offset slightly)
            using (var shadowBrush = new SolidBrush(_mouseBodyShadow))
            {
                var shadowPath = new System.Drawing.Drawing2D.GraphicsPath();
                int radius = 20;
                int shadowOffset = 2;
                
                var shadowRect = new Rectangle(mouseBody.X + shadowOffset, mouseBody.Y + shadowOffset,
                                             mouseBody.Width, mouseBody.Height);
                
                shadowPath.AddArc(shadowRect.X, shadowRect.Y, radius, radius, 180, 90);
                shadowPath.AddArc(shadowRect.Right - radius, shadowRect.Y, radius, radius, 270, 90);
                shadowPath.AddArc(shadowRect.Right - radius, shadowRect.Bottom - radius, radius, radius, 0, 90);
                shadowPath.AddArc(shadowRect.X, shadowRect.Bottom - radius, radius, radius, 90, 90);
                shadowPath.CloseFigure();
                
                g.FillPath(shadowBrush, shadowPath);
            }
            
            // Draw mouse body with gradient effect
            using (var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                mouseBody, _mouseBodyColor, _mouseBodyShadow, 45f))
            using (var pen = new Pen(_textColor, 1.5f))
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                int radius = 20;
                
                path.AddArc(mouseBody.X, mouseBody.Y, radius, radius, 180, 90);
                path.AddArc(mouseBody.Right - radius, mouseBody.Y, radius, radius, 270, 90);
                path.AddArc(mouseBody.Right - radius, mouseBody.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(mouseBody.X, mouseBody.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                
                g.FillPath(gradientBrush, path);
                g.DrawPath(pen, path);
            }
            
            // Add a subtle highlight on the top
            using (var highlightBrush = new SolidBrush(Color.FromArgb(80, Color.White)))
            {
                var highlightPath = new System.Drawing.Drawing2D.GraphicsPath();
                int radius = 20;
                var highlightRect = new Rectangle(mouseBody.X + 5, mouseBody.Y + 5,
                                                mouseBody.Width - 10, mouseBody.Height / 3);
                
                highlightPath.AddArc(highlightRect.X, highlightRect.Y, radius, radius, 180, 90);
                highlightPath.AddArc(highlightRect.Right - radius, highlightRect.Y, radius, radius, 270, 90);
                highlightPath.AddLine(highlightRect.Right, highlightRect.Bottom, highlightRect.X, highlightRect.Bottom);
                highlightPath.CloseFigure();
                
                g.FillPath(highlightBrush, highlightPath);
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
                    if (component == "WheelMiddle")
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
                    if (component == "WheelMiddle")
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
            using (var font = new Font("Arial", component == "WheelMiddle" ? 8 : 6, FontStyle.Bold))
            using (var brush = new SolidBrush(_textColor))
            {
                var textSize = g.MeasureString(label, font);
                float textX = rect.X + (rect.Width - textSize.Width) / 2;
                float textY = rect.Y + (rect.Height - textSize.Height) / 2;
                
                // Special positioning for certain components
                if (component == "MouseSensor")
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
            if (_blockingMode == BlockingMode.Select && _advancedConfig != null)
            {
                // In select mode, show selected components in orange
                if (_advancedConfig.IsComponentSelected(component))
                    return _selectedColor;
                else
                    return _neutralColor;
            }
            
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
                    "WheelMiddle" => _advancedConfig.BlockMiddleButton || _advancedConfig.BlockMouseWheel,
                    "X1Button" => _advancedConfig.BlockX1Button,
                    "X2Button" => _advancedConfig.BlockX2Button,
                    "MouseSensor" => _advancedConfig.BlockMouseMovement,
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
            
            // Draw legend items based on current mode
            if (_blockingMode == BlockingMode.Select)
            {
                DrawLegendItem(g, legendX, legendY, _selectedColor, "Selected");
                DrawLegendItem(g, legendX, legendY + 15, _neutralColor, "Unselected");
            }
            else
            {
                DrawLegendItem(g, legendX, legendY, _blockedColor, "Blocked");
                DrawLegendItem(g, legendX, legendY + 15, _allowedColor, "Allowed");
                DrawLegendItem(g, legendX + 80, legendY, _neutralColor, "Inactive");
            }
            
            // Remove redundant mode indicator - it's displayed elsewhere and not needed here
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
                if (_advancedConfig.BlockMouseMovement) blockedActions.Add("Mouse Sensor");
                if (_advancedConfig.BlockDoubleClick) blockedActions.Add("Double Click");
                
                if (blockedActions.Count == 0)
                    return "No mouse actions are blocked";
                
                return $"Blocked: {string.Join(", ", blockedActions)}";
            }
            
            return "Blocking state unknown";
        }

        /// <summary>
        /// Handles mouse click events on the mouse visualization
        /// </summary>
        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: Mode={_blockingMode}, Location={e.Location}, Config={_advancedConfig != null}");
            
            // Only handle clicks in Select mode
            if (_blockingMode != BlockingMode.Select || _advancedConfig == null)
            {
                System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: Early return - Mode={_blockingMode}, Config={_advancedConfig != null}");
                return;
            }

            // Find which component was clicked
            foreach (var kvp in _mouseComponents)
            {
                var component = kvp.Key;
                var rect = kvp.Value;
                
                // Skip the mouse body as it's not clickable
                if (component == "MouseBody")
                    continue;
                
                if (rect.Contains(e.Location))
                {
                    System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: Found component {component} at {rect}");
                    
                    // Toggle selection for this component
                    _advancedConfig.ToggleComponentSelection(component);
                    System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: Toggled selection for {component}");
                    
                    // Raise the ComponentClicked event
                    ComponentClicked?.Invoke(this, component);
                    System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: Raised ComponentClicked event for {component}");
                    
                    // Refresh the display
                    Invalidate();
                    System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: Invalidated display");
                    break;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: No component found at location {e.Location}");
        }
    }
}