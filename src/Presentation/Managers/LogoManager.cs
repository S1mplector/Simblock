using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Presentation.Managers
{
    /// <summary>
    /// Manages logo and icon creation and state changes
    /// </summary>
    public class LogoManager : ILogoManager
    {
        private readonly UISettings _uiSettings;
        private readonly ILogger<LogoManager> _logger;
        private bool _disposed = false;

        public LogoManager(UISettings uiSettings, ILogger<LogoManager> logger)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a PictureBox with the logo image
        /// </summary>
        public PictureBox CreateLogoPictureBox()
        {
            return new PictureBox
            {
                Size = _uiSettings.LogoSize,
                Anchor = AnchorStyles.None,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = CreateLogoImage()
            };
        }

        /// <summary>
        /// Creates a PictureBox with the mouse image
        /// </summary>
        public PictureBox CreateMousePictureBox()
        {
            return new PictureBox
            {
                Size = _uiSettings.LogoSize,
                Anchor = AnchorStyles.None,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = CreateMouseImage()
            };
        }

        /// <summary>
        /// Creates the application icon
        /// </summary>
        public Icon CreateApplicationIcon()
        {
            try
            {
                // Try to load the logo.ico file from embedded resources
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "SimBlock.src.Presentation.Resources.Images.logo.ico";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return new Icon(stream);
                }
                else
                {
                    // Fallback to file system if embedded resource not found
                    string iconPath = Path.Combine(Application.StartupPath, "src", "Presentation", "Resources", "Images", "logo.ico");
                    
                    if (File.Exists(iconPath))
                    {
                        return new Icon(iconPath);
                    }
                    else
                    {
                        // Final fallback to the original programmatic icon
                        return CreateFallbackIcon();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load logo.ico, using fallback icon");
                return CreateFallbackIcon();
            }
        }

        /// <summary>
        /// Updates the logo appearance based on blocking state
        /// </summary>
        public void UpdateLogoState(PictureBox logoPictureBox, bool isBlocked, bool isMouseIcon = false)
        {
            if (logoPictureBox?.Image == null) return;

            try
            {
                // Create a new image based on the current state and icon type
                var originalImage = isMouseIcon ? CreateMouseImage() : CreateLogoImage();
                if (originalImage == null) return;

                // Dispose previous image to prevent memory leaks
                var oldImage = logoPictureBox.Image;
                
                if (isBlocked)
                {
                    // Create a dimmed and slightly red-tinted version for blocked state
                    logoPictureBox.Image = CreateBlockedStateImage(originalImage);
                }
                else
                {
                    // Use the original image for unlocked state
                    logoPictureBox.Image = originalImage;
                }

                // Dispose the old image after setting the new one
                if (oldImage != null && oldImage != logoPictureBox.Image)
                {
                    oldImage.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating logo appearance");
            }
        }

        /// <summary>
        /// Creates the logo image from resources or fallback
        /// </summary>
        private Image? CreateLogoImage()
        {
            try
            {
                // Try to load the logo.ico file from embedded resources
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "SimBlock.src.Presentation.Resources.Images.logo.ico";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var icon = new Icon(stream);
                    return icon.ToBitmap();
                }
                else
                {
                    // Fallback to file system if embedded resource not found
                    string iconPath = Path.Combine(Application.StartupPath, "src", "Presentation", "Resources", "Images", "logo.ico");
                    
                    if (File.Exists(iconPath))
                    {
                        using var icon = new Icon(iconPath);
                        return icon.ToBitmap();
                    }
                    else
                    {
                        // Final fallback to the original programmatic icon
                        return CreateFallbackLogoImage();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load logo image, using fallback");
                return CreateFallbackLogoImage();
            }
        }

        /// <summary>
        /// Creates the mouse image from resources or fallback
        /// </summary>
        private Image? CreateMouseImage()
        {
            try
            {
                // Try to load the mouse.png file from embedded resources
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "SimBlock.src.Presentation.Resources.Images.mouse.png";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return Image.FromStream(stream);
                }
                else
                {
                    // Fallback to file system if embedded resource not found
                    string imagePath = Path.Combine(Application.StartupPath, "src", "Presentation", "Resources", "Images", "mouse.png");
                    
                    if (File.Exists(imagePath))
                    {
                        return Image.FromFile(imagePath);
                    }
                    else
                    {
                        // Final fallback to a programmatic mouse icon
                        return CreateFallbackMouseImage();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load mouse image, using fallback");
                return CreateFallbackMouseImage();
            }
        }

        /// <summary>
        /// Creates a fallback logo image programmatically
        /// </summary>
        private Image CreateFallbackLogoImage()
        {
            var bitmap = new Bitmap(_uiSettings.LogoSize.Width, _uiSettings.LogoSize.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillEllipse(Brushes.Blue, 6, 6, 36, 36);
                g.DrawString("K", new Font("Arial", 24, FontStyle.Bold), 
                    Brushes.White, 12, 8);
            }
            return bitmap;
        }

        /// <summary>
        /// Creates a fallback mouse image programmatically
        /// </summary>
        private Image CreateFallbackMouseImage()
        {
            var bitmap = new Bitmap(_uiSettings.LogoSize.Width, _uiSettings.LogoSize.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillEllipse(Brushes.Gray, 6, 6, 36, 36);
                g.DrawString("M", new Font("Arial", 24, FontStyle.Bold),
                    Brushes.White, 12, 8);
            }
            return bitmap;
        }

        /// <summary>
        /// Creates a fallback icon programmatically
        /// </summary>
        private Icon CreateFallbackIcon()
        {
            using var bitmap = new Bitmap(_uiSettings.IconSize.Width, _uiSettings.IconSize.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.FillEllipse(Brushes.Blue, 4, 4, 24, 24);
                g.DrawString("K", new Font("Arial", 16, FontStyle.Bold), 
                    Brushes.White, 8, 6);
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }

        /// <summary>
        /// Creates a dimmed and red-tinted version of the image for blocked state
        /// </summary>
        private Image CreateBlockedStateImage(Image originalImage)
        {
            var bitmap = new Bitmap(originalImage.Width, originalImage.Height);
            
            using (var g = Graphics.FromImage(bitmap))
            {
                // Draw the original image with reduced opacity (dimmed)
                var colorMatrix = new ColorMatrix();
                
                // Dim the image and add red tint
                colorMatrix.Matrix00 = 0.8f; // Red
                colorMatrix.Matrix11 = 0.4f; // Green (reduced)
                colorMatrix.Matrix22 = 0.4f; // Blue (reduced)
                colorMatrix.Matrix33 = 0.6f; // Alpha (transparency for dimming)
                colorMatrix.Matrix44 = 1.0f;
                
                var attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                
                g.DrawImage(originalImage, 
                    new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                    0, 0, originalImage.Width, originalImage.Height, 
                    GraphicsUnit.Pixel, attributes);
            }
            
            return bitmap;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
