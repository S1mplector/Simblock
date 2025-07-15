using SimBlock.Core.Domain.Entities;

namespace SimBlock.Presentation.ViewModels
{
    /// <summary>
    /// View model for the main application window
    /// </summary>
    public class MainWindowViewModel
    {
        // Keyboard properties
        public bool IsKeyboardBlocked { get; set; }
        public string KeyboardStatusText { get; set; } = "Keyboard is unlocked";
        public string KeyboardToggleButtonText { get; set; } = "Block Keyboard";
        public DateTime KeyboardLastToggleTime { get; set; }
        
        // Mouse properties
        public bool IsMouseBlocked { get; set; }
        public string MouseStatusText { get; set; } = "Mouse is unlocked";
        public string MouseToggleButtonText { get; set; } = "Block Mouse";
        public DateTime MouseLastToggleTime { get; set; }
        
        public void UpdateFromKeyboardState(KeyboardBlockState state)
        {
            IsKeyboardBlocked = state.IsBlocked;
            KeyboardStatusText = state.IsBlocked ? "Keyboard is BLOCKED" : "Keyboard is unlocked";
            KeyboardToggleButtonText = state.IsBlocked ? "Unblock Keyboard" : "Block Keyboard";
            KeyboardLastToggleTime = state.LastToggleTime;
        }
        
        public void UpdateFromMouseState(MouseBlockState state)
        {
            IsMouseBlocked = state.IsBlocked;
            MouseStatusText = state.IsBlocked ? "Mouse is BLOCKED" : "Mouse is unlocked";
            MouseToggleButtonText = state.IsBlocked ? "Unblock Mouse" : "Block Mouse";
            MouseLastToggleTime = state.LastToggleTime;
        }
    }
}
