using SimBlock.Presentation.Configuration;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Interface for managing application themes
    /// </summary>
    public interface IThemeManager
    {
        /// <summary>
        /// Event raised when the theme changes
        /// </summary>
        event EventHandler<Theme>? ThemeChanged;

        /// <summary>
        /// Gets the current theme
        /// </summary>
        Theme CurrentTheme { get; }

        /// <summary>
        /// Sets the theme and applies it to the UI settings
        /// </summary>
        void SetTheme(Theme theme);

        /// <summary>
        /// Toggles between light and dark themes
        /// </summary>
        void ToggleTheme();

        /// <summary>
        /// Registers UI components that need theme updates
        /// </summary>
        void RegisterComponents(Form mainForm, IUILayoutManager layoutManager, IStatusBarManager statusBarManager);

        /// <summary>
        /// Applies the current theme to all UI components
        /// </summary>
        void ApplyThemeToAllComponents();
    }
}