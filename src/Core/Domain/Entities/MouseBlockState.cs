using SimBlock.Core.Domain.Enums;

namespace SimBlock.Core.Domain.Entities
{
    /// <summary>
    /// Represents the current state of mouse blocking
    /// </summary>
    public class MouseBlockState
    {
        public bool IsBlocked { get; private set; }
        public DateTime LastToggleTime { get; private set; }
        public string? LastToggleReason { get; private set; }
        
        /// <summary>
        /// Current blocking mode (Simple or Advanced)
        /// </summary>
        public BlockingMode Mode { get; private set; } = BlockingMode.Simple;
        
        /// <summary>
        /// Advanced configuration for selective mouse action blocking
        /// </summary>
        public AdvancedMouseConfiguration? AdvancedConfig { get; private set; }

        public MouseBlockState()
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
        /// Sets the blocking mode to Simple (blocks all mouse actions when enabled)
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
        public void SetAdvancedMode(AdvancedMouseConfiguration config, string? reason = null)
        {
            Mode = BlockingMode.Advanced;
            AdvancedConfig = config?.Clone();
            LastToggleTime = DateTime.UtcNow;
            LastToggleReason = reason;
        }
        
        /// <summary>
        /// Sets the blocking mode to Select with specific configuration
        /// </summary>
        public void SetSelectMode(AdvancedMouseConfiguration config, string? reason = null)
        {
            Mode = BlockingMode.Select;
            AdvancedConfig = config?.Clone();
            
            // Clear blocking settings from Advanced mode to prevent interference with Select mode visualization
            AdvancedConfig?.PrepareForSelectMode();
            
            LastToggleTime = DateTime.UtcNow;
            LastToggleReason = reason;
        }
        
        /// <summary>
        /// Checks if a specific mouse action should be blocked based on current mode and configuration
        /// </summary>
        public bool IsMouseActionBlocked(int mouseMessage, uint mouseData = 0)
        {
            // If not blocked at all, nothing is blocked
            if (!IsBlocked)
                return false;
                
            // In simple mode, all mouse actions are blocked
            if (Mode == BlockingMode.Simple)
                return true;
                
            // In advanced mode, check the configuration
            if (Mode == BlockingMode.Advanced && AdvancedConfig != null)
            {
                // For X button messages, need to check specific button
                const int WM_XBUTTONDOWN = 0x020B;
                const int WM_XBUTTONUP = 0x020C;
                
                if (mouseMessage == WM_XBUTTONDOWN || mouseMessage == WM_XBUTTONUP)
                {
                    return AdvancedConfig.IsXButtonBlocked(mouseData);
                }
                
                return AdvancedConfig.IsMouseActionBlocked(mouseMessage);
            }
            
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
                return "All mouse actions blocked";
                
            if (Mode == BlockingMode.Advanced && AdvancedConfig != null)
            {
                return AdvancedConfig.GetBlockingSummary() + " blocked";
            }
            
            return "Advanced mode (no configuration)";
        }
    }
}