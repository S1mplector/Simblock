using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;

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
            KeyboardStatusText = GetKeyboardStatusText(state);
            KeyboardToggleButtonText = GetKeyboardToggleButtonText(state);
            KeyboardLastToggleTime = state.LastToggleTime;
        }
        
        public void UpdateFromMouseState(MouseBlockState state)
        {
            IsMouseBlocked = state.IsBlocked;
            MouseStatusText = GetMouseStatusText(state);
            MouseToggleButtonText = GetMouseToggleButtonText(state);
            MouseLastToggleTime = state.LastToggleTime;
        }
        
        private string GetKeyboardStatusText(KeyboardBlockState state)
        {
            if (state.Mode == BlockingMode.Select)
                return "Select keys to block";
            
            return state.IsBlocked ? "Keyboard is BLOCKED" : "Keyboard is unlocked";
        }
        
        private string GetKeyboardToggleButtonText(KeyboardBlockState state)
        {
            if (state.Mode == BlockingMode.Select)
                return "Apply Selection";
            
            return state.IsBlocked ? "Unblock Keyboard" : "Block Keyboard";
        }
        
        private string GetMouseStatusText(MouseBlockState state)
        {
            if (state.Mode == BlockingMode.Select)
                return "Select mouse actions to block";
            
            return state.IsBlocked ? "Mouse is BLOCKED" : "Mouse is unlocked";
        }
        
        private string GetMouseToggleButtonText(MouseBlockState state)
        {
            if (state.Mode == BlockingMode.Select)
                return "Apply Selection";
            
            return state.IsBlocked ? "Unblock Mouse" : "Block Mouse";
        }
    }
}
