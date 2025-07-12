using SimBlock.Core.Domain.Entities;

namespace SimBlock.Presentation.ViewModels
{
    /// <summary>
    /// View model for the main application window
    /// </summary>
    public class MainWindowViewModel
    {
        public bool IsKeyboardBlocked { get; set; }
        public string StatusText { get; set; } = "Keyboard is unlocked";
        public string ToggleButtonText { get; set; } = "Block Keyboard";
        public DateTime LastToggleTime { get; set; }
        
        public void UpdateFromState(KeyboardBlockState state)
        {
            IsKeyboardBlocked = state.IsBlocked;
            StatusText = state.IsBlocked ? "Keyboard is BLOCKED" : "Keyboard is unlocked";
            ToggleButtonText = state.IsBlocked ? "Unblock Keyboard" : "Block Keyboard";
            LastToggleTime = state.LastToggleTime;
        }
    }
}
