using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimBlock.Presentation.Controls
{
    public class RoundedButton : Button
    {
        private int _cornerRadius = 8;
        private Color _hoverColor;
        private Color _pressedColor;
        private bool _isHovered;
        private bool _isPressed;

        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        public Color HoverColor
        {
            get => _hoverColor;
            set
            {
                _hoverColor = value;
                Invalidate();
            }
        }

        public Color PressedColor
        {
            get => _pressedColor;
            set
            {
                _pressedColor = value;
                Invalidate();
            }
        }

        public RoundedButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            // Default colors that will be overridden by theme
            BackColor = Color.FromArgb(0, 120, 212);
            ForeColor = Color.White;
            HoverColor = Color.FromArgb(16, 110, 190);
            PressedColor = Color.FromArgb(0, 90, 158);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Size = new Size(120, 40);
            Cursor = Cursors.Hand;

            // Event handlers
            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
            MouseDown += (s, e) => { _isPressed = true; Invalidate(); };
            MouseUp += (s, e) => { _isPressed = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Clear the background with the parent's background color
            if (Parent != null)
            {
                using (var brush = new SolidBrush(Parent.BackColor))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }

            // Determine button state colors
            Color backColor = BackColor;
            if (_isPressed)
                backColor = PressedColor;
            else if (_isHovered)
                backColor = HoverColor;

            // Draw background with a slightly smaller rectangle to prevent edge artifacts
            Rectangle fillRect = new Rectangle(
                ClientRectangle.X,
                ClientRectangle.Y,
                ClientRectangle.Width - 1,
                ClientRectangle.Height - 1);

            using (var path = GetRoundRectangle(fillRect, CornerRadius))
            using (var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Draw text
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                ClientRectangle,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            );
        }

        private static GraphicsPath GetRoundRectangle(Rectangle bounds, int radius)
        {
            // Ensure the radius is not larger than half the width or height
            radius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2);
            
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            GraphicsPath path = new GraphicsPath();

            // Top left arc
            path.AddArc(arc, 180, 90);

            // Top right arc
            arc.X = bounds.Right - diameter - 1; // -1 to prevent drawing outside bounds
            path.AddArc(arc, 270, 90);

            // Bottom right arc
            arc.Y = bounds.Bottom - diameter - 1; // -1 to prevent drawing outside bounds
            path.AddArc(arc, 0, 90);

            // Bottom left arc
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        protected override bool ShowFocusCues => false;
    }
}
