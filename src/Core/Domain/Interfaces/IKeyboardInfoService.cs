using System.Threading.Tasks;

namespace SimBlock.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for retrieving keyboard information
    /// </summary>
    public interface IKeyboardInfoService
    {
        /// <summary>
        /// Gets the name of the currently active keyboard layout
        /// </summary>
        /// <returns>The keyboard layout name</returns>
        Task<string> GetCurrentKeyboardNameAsync();
        
        /// <summary>
        /// Gets the language/culture identifier of the current keyboard layout
        /// </summary>
        /// <returns>The keyboard layout identifier</returns>
        Task<string> GetCurrentKeyboardLanguageAsync();
    }
}