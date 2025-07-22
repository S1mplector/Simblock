using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;
using SimBlock.Presentation.Configuration;
using System.Drawing.Drawing2D;

namespace SimBlock.Presentation.Controls
{
    /// <summary>
    /// Custom control that displays a modern mouse diagram with color-coded buttons and actions
    /// </summary>
    public class MouseVisualizationControl : UserControl
    {
        private readonly UISettings _uiSettings;
        private BlockingMode _blockingMode = BlockingMode.Simple;
        private AdvancedMouseConfiguration? _advancedConfig;
        private bool _isBlocked = false;
        
        // Modern mouse renderer
        private readonly ModernMouseRenderer _mouseRenderer;
        
        // Drag selection state
        private bool _isDragging = false;
        private bool _mouseDown = false;
        private Point _dragStartPoint;
        private Rectangle _selectionRectangle;
        private const int DragThreshold = 5; // Minimum pixels to move before starting drag

        // Events
        public event EventHandler<string>? ComponentClicked;

        public MouseVisualizationControl(UISettings uiSettings)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _mouseRenderer = new ModernMouseRenderer();
            
            InitializeComponent();
            _mouseRenderer.Initialize(uiSettings);
        }

        private void InitializeComponent()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.DoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
            
            BackColor = Color.Transparent;
            Size = new Size(250, 250);
            
            // Enable mouse events
            this.MouseClick += OnMouseClick;
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            
            // Add tooltip for better user experience
            var tooltip = new ToolTip();
            tooltip.SetToolTip(this, "Mouse blocking visualization - Click or drag to select components");
            
            // Handle DPI changes (invalidate on DPI change event when available)
#if NETFRAMEWORK || NET6_0_OR_GREATER
            this.DpiChangedAfterParent += (s, e) => Invalidate();
#endif
        }

        /// <summary>
        /// Updates the visualization with current blocking state
        /// </summary>
        public void UpdateVisualization(BlockingMode mode, AdvancedMouseConfiguration? config, bool isBlocked)
        {
            _blockingMode = mode;
            _advancedConfig = config;
            _isBlocked = isBlocked;
            
            // Update the renderer's state
            _mouseRenderer.UpdateState(mode, config, isBlocked);
            
            Invalidate(); // Trigger repaint
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            
            try
            {
                // Let the renderer draw the mouse
                _mouseRenderer.Draw(g, Width, Height);
                
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
            }
            catch (Exception ex)
            {
                // Fallback drawing in case of errors
                using (var font = new Font("Arial", 8))
                using (var brush = new SolidBrush(Color.Red))
                {
                    g.DrawString("Error rendering mouse: " + ex.Message, font, brush, 10, 10);
                }
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

            // Find which component was clicked using the renderer
            string? component = _mouseRenderer.GetComponentAt(e.Location);
            
            if (component != null)
            {
                System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: Found component {component}");
                
                // Toggle selection for this component
                _advancedConfig.ToggleComponentSelection(component);
                System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: Toggled selection for {component}");
                
                // Raise the ComponentClicked event
                ComponentClicked?.Invoke(this, component);
                System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: Raised ComponentClicked event for {component}");
                
                // Refresh the display
                Invalidate();
                System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: Invalidated display");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"MouseVisualizationControl.OnMouseClick: No component found at location {e.Location}");
            }
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
                // Get all components that intersect with the selection rectangle
                var components = _mouseRenderer.GetComponentsInRectangle(_selectionRectangle);
                
                foreach (var component in components)
                {
                    _advancedConfig?.ToggleComponentSelection(component);
                }
                
                if (components.Count > 0)
                {
                    // Raise event to notify of selection changes
                    ComponentClicked?.Invoke(this, "DragSelection");
                    Invalidate(); // Clear selection rectangle
                }
            }

            // Reset all drag states
            _mouseDown = false;
            _isDragging = false;
            _selectionRectangle = Rectangle.Empty;
        }
    }
}