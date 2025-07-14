namespace SimBlock.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for system tray operations
    /// </summary>
    public interface ISystemTrayService
    {
        bool IsVisible { get; }
        
        void Show();
        void Hide();
        void UpdateIcon(bool isBlocked);
        void UpdateTooltip(string tooltip);
        void ShowNotification(string title, string message);
        
        event EventHandler? TrayIconClicked;
        event EventHandler? ShowWindowRequested;
        event EventHandler? ExitRequested;
    }
}
