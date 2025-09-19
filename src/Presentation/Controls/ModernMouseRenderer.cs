using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SimBlock.Core.Domain.Enums;
using SimBlock.Core.Domain.Entities;
using SimBlock.Presentation.Configuration;

namespace SimBlock.Presentation.Controls
{
    /// <summary>
    /// Renders a modern, realistic mouse visualization
    /// </summary>
    public class ModernMouseRenderer
    {
        // Mouse component definitions
        private readonly Dictionary<string, Rectangle> _mouseComponents = new();
        private readonly Dictionary<string, string> _componentLabels = new();
        
        // Drawing constants
        private const int MouseWidth = 120;
        private const int MouseHeight = 180;
        private const int ButtonHeight = 28;
        private const int WheelSize = 24;
        private const int ButtonRounding = 6;
        private const int BodyRounding = 12;
        
        // Color scheme
        private Color _blockedColor = Color.FromArgb(255, 95, 87);
        private Color _allowedColor = Color.FromArgb(40, 201, 151);
        private Color _neutralColor = Color.FromArgb(240, 240, 240);
        private Color _selectedColor = Color.FromArgb(0, 122, 204);
        private Color _textColor = Color.FromArgb(64, 64, 64);
        private Color _mouseBodyColor = Color.FromArgb(45, 45, 48);
        private Color _mouseBodyHighlight = Color.FromArgb(60, 60, 64);
        private Color _mouseBodyShadow = Color.FromArgb(30, 30, 32);
        private Color _buttonColor = Color.FromArgb(64, 64, 64);
        private Color _wheelColor = Color.FromArgb(80, 80, 80);
        private Color _sensorColor = Color.FromArgb(20, 20, 20);
        
        // State
        private bool _isDarkTheme = true;
        private bool _isBlocked = false;
        private BlockingMode _blockingMode = BlockingMode.Simple;
        private AdvancedMouseConfiguration? _advancedConfig;
        
        public ModernMouseRenderer()
        {
            InitializeMouseLayout();
        }
        
        public void Initialize(UISettings uiSettings)
        {
            // Use theme colors if available
            _blockedColor = uiSettings.ErrorColor;
            _allowedColor = uiSettings.SuccessColor;
            _selectedColor = uiSettings.SelectedColor;
            _textColor = uiSettings.TextColor;
            
            // Determine if we're using a dark theme
            _isDarkTheme = IsDarkColor(uiSettings.BackgroundColor);
            
            // Adjust colors based on theme
            if (!_isDarkTheme)
            {
                _mouseBodyColor = Color.FromArgb(220, 220, 220);
                _mouseBodyHighlight = Color.FromArgb(235, 235, 235);
                _mouseBodyShadow = Color.FromArgb(180, 180, 180);
                _buttonColor = Color.FromArgb(200, 200, 200);
                _wheelColor = Color.FromArgb(170, 170, 170);
                _sensorColor = Color.FromArgb(100, 100, 100);
            }
        }
        
        // Builds a tapered, convex main-button path that blends with the body and center split
        private static GraphicsPath CreateMainButtonPath(Rectangle rect, bool isLeft)
        {
            // Return a classic rounded-rectangle button path for a simpler look
            int radius = ButtonRounding;
            var path = new GraphicsPath { FillMode = FillMode.Winding };
            // Top-left arc
            path.AddArc(rect.Left, rect.Top, radius * 2, radius * 2, 180, 90);
            // Top-right arc
            path.AddArc(rect.Right - radius * 2, rect.Top, radius * 2, radius * 2, 270, 90);
            // Bottom-right arc
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            // Bottom-left arc
            path.AddArc(rect.Left, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
        
        public void UpdateState(BlockingMode mode, AdvancedMouseConfiguration? config, bool isBlocked)
        {
            _blockingMode = mode;
            _advancedConfig = config;
            _isBlocked = isBlocked;
        }
        
        public void Draw(Graphics g, int width, int height)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            
            // Center the mouse in the available space
            int centerX = width / 2;
            int startY = 20;
            
            // Update component positions based on the new size
            UpdateComponentPositions(centerX, startY);
            
            // Draw mouse body with shadow
            DrawMouseBody(g, centerX, startY);
            
            // Draw mouse components
            DrawMouseComponents(g);
        }
        
        public string? GetComponentAt(Point location)
        {
            foreach (var kvp in _mouseComponents)
            {
                if (kvp.Key != "MouseBody" && kvp.Value.Contains(location))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a list of component keys whose bounding rectangles intersect with the given selection rectangle.
        /// </summary>
        /// <param name="selectionRect">Rectangle in control coordinates</param>
        public List<string> GetComponentsInRectangle(Rectangle selectionRect)
        {
            var result = new List<string>();
            foreach (var kvp in _mouseComponents)
            {
                if (kvp.Value.IntersectsWith(selectionRect))
                    result.Add(kvp.Key);
            }
            return result;
        }
        
        private void InitializeMouseLayout()
        {
            // Component positions will be updated in Draw based on actual size
            _componentLabels["LeftButton"] = "L";
            _componentLabels["RightButton"] = "R";
            _componentLabels["WheelMiddle"] = "M";
            _componentLabels["X1Button"] = "X1";
            _componentLabels["X2Button"] = "X2";
            _componentLabels["MouseSensor"] = "SENSOR";
            _componentLabels["DoubleClick"] = "2×";
        }
        
        private void UpdateComponentPositions(int centerX, int startY)
        {
            int mouseX = centerX - MouseWidth / 2;
            
            // Clear and update component positions
            _mouseComponents.Clear();
            
            // Main mouse body outline
            _mouseComponents["MouseBody"] = new Rectangle(mouseX, startY, MouseWidth, MouseHeight);
            
            // Main buttons sit flush with body; slightly taller and closer to tip
            int buttonTop = startY + 6;
            int innerGap = 10; // narrow center channel
            int buttonHeight = ButtonHeight + 10;
            int leftWidth = MouseWidth / 2 - innerGap - 6;
            int rightWidth = MouseWidth / 2 - innerGap - 6;

            // Left mouse button
            _mouseComponents["LeftButton"] = new Rectangle(
                mouseX + 6,
                buttonTop,
                leftWidth,
                buttonHeight);
            
            // Right mouse button
            _mouseComponents["RightButton"] = new Rectangle(
                mouseX + MouseWidth / 2 + innerGap,
                buttonTop,
                rightWidth,
                buttonHeight);
            
            // Wheel/middle button - position centered between L and R buttons
            _mouseComponents["WheelMiddle"] = new Rectangle(
                centerX - WheelSize / 2, 
                startY + (ButtonHeight / 2) - (WheelSize / 2) + 5, // Magic number, to move it down
                WheelSize, 
                WheelSize);
            
            // Side buttons (X1, X2) - positioned ergonomically
            _mouseComponents["X1Button"] = new Rectangle(
                mouseX + 8, 
                startY + 55, 
                18, 
                30);
                
            _mouseComponents["X2Button"] = new Rectangle(
                mouseX + 8, 
                startY + 90, 
                18, 
                30);
            
            // Mouse sensor (positioned above the bottom)
            _mouseComponents["MouseSensor"] = new Rectangle(
                mouseX + 20, 
                startY + MouseHeight - 95,  // Moved up by 25 pixels, to be at the center of the "mouse"
                MouseWidth - 40, 
                22);
            
            // Double click indicator (overlaid on left button)
            _mouseComponents["DoubleClick"] = _mouseComponents["LeftButton"];
        }
        
        private void DrawMouseBody(Graphics g, int centerX, int startY)
        {
            int mouseX = centerX - MouseWidth / 2;
            var mouseBody = new Rectangle(mouseX, startY, MouseWidth, MouseHeight);
            
            // Build a more realistic mouse silhouette (tapered top, wider bottom)
            using (var bodyPath = CreateMouseSilhouettePath(mouseX, startY, MouseWidth, MouseHeight))
            {
                // Shadow with slight offset
                using (var shadowPath = CreateMouseSilhouettePath(mouseX + 3, startY + 3, MouseWidth, MouseHeight))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Body gradient
                using (var bodyBrush = new LinearGradientBrush(
                    new Point(mouseX, startY),
                    new Point(mouseX, startY + MouseHeight),
                    _mouseBodyHighlight,
                    _mouseBodyColor))
                {
                    g.FillPath(bodyBrush, bodyPath);
                }

                // Subtle top gloss highlight
                var highlightRect = new Rectangle(mouseX + 10, startY + 8, MouseWidth - 20, MouseHeight / 3);
                var oldClip = g.Clip; // save
                g.SetClip(bodyPath);
                using (var gloss = new LinearGradientBrush(
                    new Point(highlightRect.Left, highlightRect.Top),
                    new Point(highlightRect.Left, highlightRect.Bottom),
                    Color.FromArgb(70, Color.White),
                    Color.FromArgb(0, Color.White)))
                {
                    g.FillRectangle(gloss, highlightRect);
                }
                g.Clip = oldClip; // restore

                // Body border
                using (var borderPen = new Pen(_isDarkTheme ? Color.FromArgb(80, 80, 80) : Color.FromArgb(160, 160, 160), 1.5f))
                {
                    g.DrawPath(borderPen, bodyPath);
                }

                // Center seam line to suggest two shell halves
                int cx = mouseX + MouseWidth / 2;
                using (var seamPen = new Pen(Color.FromArgb(60, 0, 0, 0), 1f))
                {
                    g.DrawLine(seamPen, new Point(cx, startY + (int)(MouseHeight * 0.10f)), new Point(cx, startY + (int)(MouseHeight * 0.95f)));
                }
            }
        }
        
        private Color GetComponentColor(string componentKey)
        {
            // Determine color for each component based on current state
            bool isBlocked = _blockingMode == BlockingMode.Simple && _isBlocked;
            if (_blockingMode == BlockingMode.Advanced && _advancedConfig != null)
            {
                // Evaluate blocked state per component in advanced mode
                isBlocked = componentKey switch
                {
                    "LeftButton" => _advancedConfig.BlockLeftButton,
                    "RightButton" => _advancedConfig.BlockRightButton,
                    "WheelMiddle" => _advancedConfig.BlockMiddleButton || _advancedConfig.BlockMouseWheel,
                    "X1Button" => _advancedConfig.BlockX1Button,
                    "X2Button" => _advancedConfig.BlockX2Button,
                    "MouseSensor" => _advancedConfig.BlockMouseMovement,
                    "DoubleClick" => _advancedConfig.BlockDoubleClick,
                    _ => false
                } && _isBlocked;
            }

            // Selected state only matters in Select mode
            bool isSelected = _blockingMode == BlockingMode.Select && _advancedConfig != null && _advancedConfig.IsComponentSelected(componentKey);

            if (isSelected)
                return _selectedColor;
            if (isBlocked)
                return _blockedColor;
            return _buttonColor; // neutral/default
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
                
                // Draw component background with appropriate shape
                using (var brush = new SolidBrush(componentColor))
                {
                    if (component == "WheelMiddle")
                    {
                        // Draw wheel with 3D effect
                        DrawWheel(g, rect, componentColor);
                    }
                    else if (component == "MouseSensor")
                    {
                        // Draw sensor with high-tech look
                        DrawSensor(g, rect, componentColor);
                    }
                    else if (component == "X1Button" || component == "X2Button")
                    {
                        // Draw side buttons directly without clipping to avoid artifacts
                        DrawSideButton(g, rect, componentColor, component == "X1Button");
                    }
                    else if (component == "LeftButton" || component == "RightButton")
                    {
                        // Draw main buttons directly without clipping to avoid visual corner artifacts
                        DrawMainButton(g, rect, componentColor, component == "LeftButton");
                    }
                    else if (component == "DoubleClick")
                    {
                        // Draw double click indicator
                        DrawDoubleClickIndicator(g, rect, componentColor);
                    }
                }
                
                // Draw component label if applicable
                if (component != "DoubleClick") // Skip label for double click indicator
                {
                    DrawComponentLabel(g, rect, label, component);
                }
            }
        }
        
        private void DrawWheel(Graphics g, Rectangle rect, Color baseColor)
        {
            // Wheel outer ring - use baseColor for selection/highlight state
            using (var wheelBrush = new LinearGradientBrush(
                rect,
                Color.FromArgb(
                    Math.Min(255, baseColor.R + 30),
                    Math.Min(255, baseColor.G + 30),
                    Math.Min(255, baseColor.B + 30)),
                baseColor,
                90f))
            using (var wheelPen = new Pen(Color.FromArgb(40, 40, 40), 1.5f))
            {
                g.FillEllipse(wheelBrush, rect);
                g.DrawEllipse(wheelPen, rect);
            }
            
            // Wheel inner circle
            var innerRect = new Rectangle(
                rect.X + 4, 
                rect.Y + 4, 
                rect.Width - 8, 
                rect.Height - 8);
                
            using (var innerBrush = new LinearGradientBrush(
                innerRect,
                Color.FromArgb(80, 80, 80),
                Color.FromArgb(50, 50, 50),
                270f))
            using (var innerPen = new Pen(Color.FromArgb(30, 30, 30), 1f))
            {
                g.FillEllipse(innerBrush, innerRect);
                g.DrawEllipse(innerPen, innerRect);
            }
            
            // Wheel scroll lines
            int centerX = rect.X + rect.Width / 2;
            int centerY = rect.Y + rect.Height / 2;
            int radius = rect.Width / 2 - 2;
            
            // Use a contrasting color for the wheel lines based on the base color
            int avgColor = (baseColor.R + baseColor.G + baseColor.B) / 3;
            Color lineColor = avgColor > 128 ? 
                Color.FromArgb(60, 60, 60) : 
                Color.FromArgb(200, 200, 200);
            
            using (var linePen = new Pen(lineColor, 1.2f))
            {
                // Horizontal line
                g.DrawLine(linePen, 
                    centerX - radius + 3, centerY,
                    centerX + radius - 3, centerY);
                
                // Vertical line (shorter)
                g.DrawLine(linePen,
                    centerX, centerY - radius / 2,
                    centerX, centerY + radius / 2);
            }
        }
        
        private void DrawSensor(Graphics g, Rectangle rect, Color baseColor)
        {
            // Just draw a small vector at the center of the mouse
            int centerX = rect.X + rect.Width / 2;
            int centerY = rect.Y + rect.Height / 2;
            
            // Draw a small crosshair for the sensor
            bool isSelected = baseColor == _selectedColor;
            Color lineColor = isSelected ? 
                Color.FromArgb(200, 0, 200, 255) : 
                Color.FromArgb(100, 0, 200, 255);
            
            using (var pen = new Pen(lineColor, 1.5f) { EndCap = LineCap.ArrowAnchor })
            {
                // Draw a small crosshair
                int size = 8;
                g.DrawLine(pen, centerX - size, centerY, centerX + size, centerY);
                g.DrawLine(pen, centerX, centerY - size, centerX, centerY + size);
            }
        }
        
        private void DrawSideButton(Graphics g, Rectangle rect, Color baseColor, bool isUpper)
        {
            // Simple rounded-rectangle side button with subtle gradient and top gloss
            int radius = 4;
            using (var buttonPath = CreateRoundedRectanglePath(rect, radius))
            {
                // Base gradient
                using (var buttonBrush = new LinearGradientBrush(
                    new Point(rect.Left, rect.Top),
                    new Point(rect.Left, rect.Bottom),
                    Color.FromArgb(
                        Math.Min(255, baseColor.R + 25),
                        Math.Min(255, baseColor.G + 25),
                        Math.Min(255, baseColor.B + 25)),
                    Color.FromArgb(
                        Math.Max(0, baseColor.R - 10),
                        Math.Max(0, baseColor.G - 10),
                        Math.Max(0, baseColor.B - 10))))
                {
                    g.FillPath(buttonBrush, buttonPath);
                }

                // Border
                using (var borderPen = new Pen(Color.FromArgb(70, 70, 70), 1f))
                {
                    g.DrawPath(borderPen, buttonPath);
                }

                // Top gloss clipped to button
                var oldClip = g.Clip;
                g.SetClip(buttonPath);
                var glossHeight = Math.Max(5, rect.Height / 2);
                var glossRect = new Rectangle(rect.Left + 1, rect.Top + 1, rect.Width - 2, glossHeight);
                using (var gloss = new LinearGradientBrush(
                    glossRect,
                    Color.FromArgb(60, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    90f))
                {
                    g.FillRectangle(gloss, glossRect);
                }
                g.Clip = oldClip;
            }
        }
        
        private void DrawMainButton(Graphics g, Rectangle rect, Color baseColor, bool isLeftButton)
        {
            // Simple rounded-rectangle button with subtle vertical gradient and top gloss
            using (var buttonPath = CreateMainButtonPath(rect, isLeftButton))
            {
                // Base gradient
                using (var buttonBrush = new LinearGradientBrush(
                    new Point(rect.Left, rect.Top),
                    new Point(rect.Left, rect.Bottom),
                    Color.FromArgb(
                        Math.Min(255, baseColor.R + 25),
                        Math.Min(255, baseColor.G + 25),
                        Math.Min(255, baseColor.B + 25)),
                    Color.FromArgb(
                        Math.Max(0, baseColor.R - 10),
                        Math.Max(0, baseColor.G - 10),
                        Math.Max(0, baseColor.B - 10))))
                {
                    g.FillPath(buttonBrush, buttonPath);
                }

                // Border
                using (var borderPen = new Pen(Color.FromArgb(70, 70, 70), 1f))
                {
                    g.DrawPath(borderPen, buttonPath);
                }

                // Top gloss clipped to the button shape to avoid artifacts
                var oldClip = g.Clip;
                g.SetClip(buttonPath);
                var glossHeight = Math.Max(6, rect.Height / 2);
                var glossRect = new Rectangle(rect.Left + 1, rect.Top + 1, rect.Width - 2, glossHeight);
                using (var gloss = new LinearGradientBrush(
                    glossRect,
                    Color.FromArgb(60, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    90f))
                {
                    g.FillRectangle(gloss, glossRect);
                }
                g.Clip = oldClip;
            }
        }
        
        private void DrawDoubleClickIndicator(Graphics g, Rectangle rect, Color baseColor)
        {
            // Draw a subtle highlight over the left button
            using (var highlightBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
            using (var highlightPen = new Pen(Color.FromArgb(120, 255, 255, 255), 1.5f))
            {
                // Draw a rounded rectangle highlight
                using (var path = CreateRoundedRectanglePath(rect, ButtonRounding))
                {
                    g.FillPath(highlightBrush, path);
                    g.DrawPath(highlightPen, path);
                }
                
                // Draw "2×" text in the corner
                using (var font = new Font("Arial", 8, FontStyle.Bold))
                using (var textBrush = new SolidBrush(_textColor))
                {
                    var textSize = g.MeasureString("2×", font);
                    float textX = rect.Right - textSize.Width - 4;
                    float textY = rect.Top + 4;
                    
                    g.DrawString("2×", font, textBrush, textX, textY);
                }
            }
        }
        
        private void DrawComponentLabel(Graphics g, Rectangle rect, string label, string component)
        {
            // Skip labels for some components that don't need them
            if (component == "DoubleClick" || component == "MouseSensor")
                return;
                
            using (var font = new Font("Arial", 7, FontStyle.Bold))
            using (var brush = new SolidBrush(_textColor))
            {
                var textSize = g.MeasureString(label, font);
                float textX = rect.X + (rect.Width - textSize.Width) / 2;
                float textY = rect.Y + (rect.Height - textSize.Height) / 2;
                
                // Adjust position for specific components
                if (component == "WheelMiddle")
                {
                    // Center in the wheel
                    textY -= 1; // Slight vertical adjustment
                }
                else if (component == "X1Button" || component == "X2Button")
                {
                    // Nudge label left to keep it fully inside the visible (clipped) area
                    const int pad = 4;
                    textX = rect.X + pad;
                    textY = rect.Y + (rect.Height - textSize.Height) / 2;
                }
                
                // Add text shadow for better visibility
                using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    g.DrawString(label, font, shadowBrush, textX + 1, textY + 1);
                }
                
                // Draw the actual text
                g.DrawString(label, font, brush, textX, textY);
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
        
        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            return CreateRoundedRectanglePath(rect.X, rect.Y, rect.Width, rect.Height, radius);
        }
        
        private static GraphicsPath CreateRoundedRectanglePath(int x, int y, int width, int height, int radius)
        {
            var path = new GraphicsPath();
            
            // Top-left arc
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            
            // Top-right arc
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            
            // Bottom-right arc
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            
            // Bottom-left arc
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            
            // Close the path
            path.CloseFigure();
            
            return path;
        }

        // Creates a classic rounded-rectangle mouse silhouette (simple rectangular style)
        private static GraphicsPath CreateMouseSilhouettePath(int x, int y, int width, int height)
        {
            int radius = BodyRounding;
            var rect = new Rectangle(x, y, width, height);
            var path = new GraphicsPath { FillMode = FillMode.Winding };
            // Top-left arc
            path.AddArc(rect.Left, rect.Top, radius * 2, radius * 2, 180, 90);
            // Top-right arc
            path.AddArc(rect.Right - radius * 2, rect.Top, radius * 2, radius * 2, 270, 90);
            // Bottom-right arc
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            // Bottom-left arc
            path.AddArc(rect.Left, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
        
        private static bool IsDarkColor(Color color)
        {
            // Calculate the perceptive luminance (aka luma) - human eye favors green color
            double luma = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            
            // Return true for dark colors, false for light colors
            return luma < 0.5;
        }
    }
}
