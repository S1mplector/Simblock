using System.Windows.Forms;
using SimBlock.Presentation.Configuration;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Applies UI theme colors and styles to WinForms controls.
    /// </summary>
    public interface IThemeApplier
    {
        /// <summary>
        /// Apply the given UI theme to the entire form and its children.
        /// </summary>
        void Apply(Form form, UISettings settings);

        /// <summary>
        /// Returns the theme toggle button text according to the current theme.
        /// </summary>
        string GetThemeButtonText(UISettings settings);
    }
}
