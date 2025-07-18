using SimBlock.Core.Domain.Enums;
using System.Windows.Forms;

namespace SimBlock.Core.Domain.Entities
{
    /// <summary>
    /// Represents the current state of keyboard blocking
    /// </summary>
    public class KeyboardBlockState
    {
        public bool IsBlocked { get; private set; }
        public DateTime LastToggleTime { get; private set; }
        public string? LastToggleReason { get; private set; }
        
        /// <summary>
        /// Current blocking mode (Simple or Advanced)
        /// </summary>
        public BlockingMode Mode { get; private set; } = BlockingMode.Simple;
        
        /// <summary>
        /// Advanced configuration for selective key blocking
        /// </summary>
        public AdvancedKeyboardConfiguration? AdvancedConfig { get; private set; }

        public KeyboardBlockState()
        {
            IsBlocked = false;
            LastToggleTime = DateTime.UtcNow;
        }

        public void SetBlocked(bool isBlocked, string? reason = null)
        {
            IsBlocked = isBlocked;
            LastToggleTime = DateTime.UtcNow;
            LastToggleReason = reason;
        }

        public void Toggle(string? reason = null)
        {
            SetBlocked(!IsBlocked, reason);
        }
        
        /// <summary>
        /// Sets the blocking mode to Simple (blocks all keys when enabled)
        /// </summary>
        public void SetSimpleMode(string? reason = null)
        {
            Mode = BlockingMode.Simple;
            AdvancedConfig = null;
            LastToggleTime = DateTime.UtcNow;
            LastToggleReason = reason;
        }
        
        /// <summary>
        /// Sets the blocking mode to Advanced with specific configuration
        /// </summary>
        public void SetAdvancedMode(AdvancedKeyboardConfiguration config, string? reason = null)
        {
            Mode = BlockingMode.Advanced;
            AdvancedConfig = config?.Clone();
            LastToggleTime = DateTime.UtcNow;
            LastToggleReason = reason;
        }
        
        /// <summary>
        /// Sets the blocking mode to Select with specific configuration
        /// </summary>
        public void SetSelectMode(AdvancedKeyboardConfiguration config, string? reason = null)
        {
            Mode = BlockingMode.Select;
            AdvancedConfig = config?.Clone();
            LastToggleTime = DateTime.UtcNow;
            LastToggleReason = reason;
        }
        
        /// <summary>
        /// Checks if a specific key should be blocked based on current mode and configuration
        /// </summary>
        public bool IsKeyBlocked(Keys key)
        {
            // If not blocked at all, nothing is blocked
            if (!IsBlocked)
                return false;
                
            // In simple mode, all keys are blocked
            if (Mode == BlockingMode.Simple)
                return true;
                
            // In advanced mode, check the configuration
            if (Mode == BlockingMode.Advanced && AdvancedConfig != null)
                return AdvancedConfig.IsKeyBlocked(key);
                
            // In select mode, nothing is blocked (it's just for selection)
            if (Mode == BlockingMode.Select)
                return false;
                
            // Default to not blocked if no configuration
            return false;
        }
        
        /// <summary>
        /// Gets a summary of the current blocking state
        /// </summary>
        public string GetBlockingSummary()
        {
            if (!IsBlocked)
                return "Not blocked";
                
            if (Mode == BlockingMode.Simple)
                return "All keys blocked";
                
            if (Mode == BlockingMode.Advanced && AdvancedConfig != null)
            {
                var blockedCount = AdvancedConfig.BlockedKeys.Count;
                var categories = new List<string>();
                
                if (AdvancedConfig.BlockModifierKeys) categories.Add("Modifiers");
                if (AdvancedConfig.BlockFunctionKeys) categories.Add("Function");
                if (AdvancedConfig.BlockNumberKeys) categories.Add("Numbers");
                if (AdvancedConfig.BlockLetterKeys) categories.Add("Letters");
                if (AdvancedConfig.BlockArrowKeys) categories.Add("Arrows");
                if (AdvancedConfig.BlockSpecialKeys) categories.Add("Special");
                
                var summary = $"{blockedCount} individual keys";
                if (categories.Count > 0)
                    summary += $" + {string.Join(", ", categories)}";
                    
                return summary;
            }
            
            return "Advanced mode (no configuration)";
        }
    }
}
