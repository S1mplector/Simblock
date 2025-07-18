using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Interface for managing logo and icon creation and state changes
    /// </summary>
    public interface ILogoManager : IDisposable
    {
        /// <summary>
        /// Creates a PictureBox with the logo image
        /// </summary>
        PictureBox CreateLogoPictureBox();

        /// <summary>
        /// Creates a PictureBox with the mouse image
        /// </summary>
        PictureBox CreateMousePictureBox();

        /// <summary>
        /// Creates the application icon
        /// </summary>
        Icon CreateApplicationIcon();

        /// <summary>
        /// Updates the logo appearance based on blocking state
        /// </summary>
        void UpdateLogoState(PictureBox logoPictureBox, bool isBlocked, bool isMouseIcon = false);
    }
}
