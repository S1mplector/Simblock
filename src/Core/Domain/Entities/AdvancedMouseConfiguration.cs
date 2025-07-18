namespace SimBlock.Core.Domain.Entities
{
    /// <summary>
    /// Configuration for advanced mouse blocking - specifies which individual mouse actions to block
    /// </summary>
    public class AdvancedMouseConfiguration
    {
        /// <summary>
        /// Block left mouse button clicks
        /// </summary>
        public bool BlockLeftButton { get; set; } = false;
        
        /// <summary>
        /// Block right mouse button clicks
        /// </summary>
        public bool BlockRightButton { get; set; } = false;
        
        /// <summary>
        /// Block middle mouse button clicks
        /// </summary>
        public bool BlockMiddleButton { get; set; } = false;
        
        /// <summary>
        /// Block X1 mouse button clicks (back button)
        /// </summary>
        public bool BlockX1Button { get; set; } = false;
        
        /// <summary>
        /// Block X2 mouse button clicks (forward button)
        /// </summary>
        public bool BlockX2Button { get; set; } = false;
        
        /// <summary>
        /// Block mouse wheel scrolling (both vertical and horizontal)
        /// </summary>
        public bool BlockMouseWheel { get; set; } = false;
        
        /// <summary>
        /// Block mouse movement
        /// </summary>
        public bool BlockMouseMovement { get; set; } = false;
        
        /// <summary>
        /// Block double-click events
        /// </summary>
        public bool BlockDoubleClick { get; set; } = false;

        /// <summary>
        /// Creates a deep copy of this configuration
        /// </summary>
        public AdvancedMouseConfiguration Clone()
        {
            return new AdvancedMouseConfiguration
            {
                BlockLeftButton = BlockLeftButton,
                BlockRightButton = BlockRightButton,
                BlockMiddleButton = BlockMiddleButton,
                BlockX1Button = BlockX1Button,
                BlockX2Button = BlockX2Button,
                BlockMouseWheel = BlockMouseWheel,
                BlockMouseMovement = BlockMouseMovement,
                BlockDoubleClick = BlockDoubleClick
            };
        }

        /// <summary>
        /// Checks if a specific mouse action should be blocked based on the configuration
        /// </summary>
        public bool IsMouseActionBlocked(int mouseMessage)
        {
            // Import constants from NativeMethods
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_LBUTTONUP = 0x0202;
            const int WM_RBUTTONDOWN = 0x0204;
            const int WM_RBUTTONUP = 0x0205;
            const int WM_MBUTTONDOWN = 0x0207;
            const int WM_MBUTTONUP = 0x0208;
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_MOUSEWHEEL = 0x020A;
            const int WM_MOUSEHWHEEL = 0x020E;
            const int WM_XBUTTONDOWN = 0x020B;
            const int WM_XBUTTONUP = 0x020C;
            const int WM_LBUTTONDBLCLK = 0x0203;
            const int WM_RBUTTONDBLCLK = 0x0206;
            const int WM_MBUTTONDBLCLK = 0x0209;

            return mouseMessage switch
            {
                WM_LBUTTONDOWN or WM_LBUTTONUP => BlockLeftButton,
                WM_RBUTTONDOWN or WM_RBUTTONUP => BlockRightButton,
                WM_MBUTTONDOWN or WM_MBUTTONUP => BlockMiddleButton,
                WM_XBUTTONDOWN or WM_XBUTTONUP => BlockX1Button || BlockX2Button, // Both X buttons
                WM_MOUSEMOVE => BlockMouseMovement,
                WM_MOUSEWHEEL or WM_MOUSEHWHEEL => BlockMouseWheel,
                WM_LBUTTONDBLCLK or WM_RBUTTONDBLCLK or WM_MBUTTONDBLCLK => BlockDoubleClick,
                _ => false
            };
        }

        /// <summary>
        /// Checks if a specific X button should be blocked
        /// </summary>
        public bool IsXButtonBlocked(uint mouseData)
        {
            // Extract which X button was pressed from mouseData
            const uint XBUTTON1 = 0x0001;
            const uint XBUTTON2 = 0x0002;
            
            uint xButton = (mouseData >> 16) & 0xFFFF;
            
            return xButton switch
            {
                XBUTTON1 => BlockX1Button,
                XBUTTON2 => BlockX2Button,
                _ => false
            };
        }

        /// <summary>
        /// Gets a summary of what mouse actions are blocked
        /// </summary>
        public string GetBlockingSummary()
        {
            var blockedActions = new List<string>();
            
            if (BlockLeftButton) blockedActions.Add("Left Button");
            if (BlockRightButton) blockedActions.Add("Right Button");
            if (BlockMiddleButton) blockedActions.Add("Middle Button");
            if (BlockX1Button) blockedActions.Add("X1 Button");
            if (BlockX2Button) blockedActions.Add("X2 Button");
            if (BlockMouseWheel) blockedActions.Add("Mouse Wheel");
            if (BlockMouseMovement) blockedActions.Add("Mouse Movement");
            if (BlockDoubleClick) blockedActions.Add("Double Click");
            
            return blockedActions.Count == 0 ? "None" : string.Join(", ", blockedActions);
        }

        /// <summary>
        /// Checks if any mouse actions are blocked
        /// </summary>
        public bool HasAnyBlocking()
        {
            return BlockLeftButton || BlockRightButton || BlockMiddleButton || 
                   BlockX1Button || BlockX2Button || BlockMouseWheel || 
                   BlockMouseMovement || BlockDoubleClick;
        }

        /// <summary>
        /// Resets all blocking settings to false
        /// </summary>
        public void ClearAll()
        {
            BlockLeftButton = false;
            BlockRightButton = false;
            BlockMiddleButton = false;
            BlockX1Button = false;
            BlockX2Button = false;
            BlockMouseWheel = false;
            BlockMouseMovement = false;
            BlockDoubleClick = false;
        }

        /// <summary>
        /// Sets all blocking settings to true
        /// </summary>
        public void BlockAll()
        {
            BlockLeftButton = true;
            BlockRightButton = true;
            BlockMiddleButton = true;
            BlockX1Button = true;
            BlockX2Button = true;
            BlockMouseWheel = true;
            BlockMouseMovement = true;
            BlockDoubleClick = true;
        }

        /// <summary>
        /// Checks if all mouse actions are blocked
        /// </summary>
        public bool BlockAllActions()
        {
            return BlockLeftButton && BlockRightButton && BlockMiddleButton &&
                   BlockX1Button && BlockX2Button && BlockMouseWheel &&
                   BlockMouseMovement && BlockDoubleClick;
        }
    }
}