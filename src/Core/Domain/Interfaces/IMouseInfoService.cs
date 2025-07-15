using System.Threading.Tasks;

namespace SimBlock.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for retrieving mouse information
    /// </summary>
    public interface IMouseInfoService
    {
        /// <summary>
        /// Gets the name of the currently active mouse device
        /// </summary>
        /// <returns>The mouse device name</returns>
        Task<string> GetCurrentMouseNameAsync();
        
        /// <summary>
        /// Gets the number of buttons on the current mouse device
        /// </summary>
        /// <returns>The number of mouse buttons</returns>
        Task<int> GetMouseButtonCountAsync();
        
        /// <summary>
        /// Gets the DPI (dots per inch) setting of the current mouse
        /// </summary>
        /// <returns>The mouse DPI value</returns>
        Task<int> GetMouseDpiAsync();
        
        /// <summary>
        /// Gets the mouse scroll wheel information
        /// </summary>
        /// <returns>True if scroll wheel is present, false otherwise</returns>
        Task<bool> HasScrollWheelAsync();
        
        /// <summary>
        /// Gets the mouse connection type (USB, Bluetooth, PS/2, etc.)
        /// </summary>
        /// <returns>The mouse connection type</returns>
        Task<string> GetConnectionTypeAsync();
    }
}