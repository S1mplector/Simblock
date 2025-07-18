using System.Windows.Forms;

namespace SimBlock.Core.Domain.Entities
{
    /// <summary>
    /// Configuration for advanced keyboard blocking - specifies which individual keys to block
    /// </summary>
    public class AdvancedKeyboardConfiguration
    {
        /// <summary>
        /// Set of individual keys to block
        /// </summary>
        public HashSet<Keys> BlockedKeys { get; set; } = new();
        
        /// <summary>
        /// Block all modifier keys (Ctrl, Alt, Shift, Windows key)
        /// </summary>
        public bool BlockModifierKeys { get; set; } = false;
        
        /// <summary>
        /// Block all function keys (F1-F12)
        /// </summary>
        public bool BlockFunctionKeys { get; set; } = false;
        
        /// <summary>
        /// Block all number keys (0-9)
        /// </summary>
        public bool BlockNumberKeys { get; set; } = false;
        
        /// <summary>
        /// Block all letter keys (A-Z)
        /// </summary>
        public bool BlockLetterKeys { get; set; } = false;
        
        /// <summary>
        /// Block all arrow keys
        /// </summary>
        public bool BlockArrowKeys { get; set; } = false;
        
        /// <summary>
        /// Block special keys (Space, Enter, Tab, Backspace, Delete, etc.)
        /// </summary>
        public bool BlockSpecialKeys { get; set; } = false;

        /// <summary>
        /// Creates a deep copy of this configuration
        /// </summary>
        public AdvancedKeyboardConfiguration Clone()
        {
            return new AdvancedKeyboardConfiguration
            {
                BlockedKeys = new HashSet<Keys>(BlockedKeys),
                BlockModifierKeys = BlockModifierKeys,
                BlockFunctionKeys = BlockFunctionKeys,
                BlockNumberKeys = BlockNumberKeys,
                BlockLetterKeys = BlockLetterKeys,
                BlockArrowKeys = BlockArrowKeys,
                BlockSpecialKeys = BlockSpecialKeys
            };
        }

        /// <summary>
        /// Checks if a specific key should be blocked based on the configuration
        /// </summary>
        public bool IsKeyBlocked(Keys key)
        {
            // Check if the key is explicitly in the blocked keys set
            if (BlockedKeys.Contains(key))
                return true;

            // Check category-based blocking
            if (BlockModifierKeys && IsModifierKey(key))
                return true;

            if (BlockFunctionKeys && IsFunctionKey(key))
                return true;

            if (BlockNumberKeys && IsNumberKey(key))
                return true;

            if (BlockLetterKeys && IsLetterKey(key))
                return true;

            if (BlockArrowKeys && IsArrowKey(key))
                return true;

            if (BlockSpecialKeys && IsSpecialKey(key))
                return true;

            return false;
        }

        /// <summary>
        /// Applies category settings to the blocked keys set
        /// </summary>
        public void ApplyCategorySettings()
        {
            if (BlockModifierKeys)
                AddModifierKeys();

            if (BlockFunctionKeys)
                AddFunctionKeys();

            if (BlockNumberKeys)
                AddNumberKeys();

            if (BlockLetterKeys)
                AddLetterKeys();

            if (BlockArrowKeys)
                AddArrowKeys();

            if (BlockSpecialKeys)
                AddSpecialKeys();
        }

        private bool IsModifierKey(Keys key)
        {
            return key == Keys.Control || key == Keys.ControlKey || key == Keys.LControlKey || key == Keys.RControlKey ||
                   key == Keys.Alt || key == Keys.Menu || key == Keys.LMenu || key == Keys.RMenu ||
                   key == Keys.Shift || key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey ||
                   key == Keys.LWin || key == Keys.RWin;
        }

        private bool IsFunctionKey(Keys key)
        {
            return key >= Keys.F1 && key <= Keys.F24;
        }

        private bool IsNumberKey(Keys key)
        {
            return (key >= Keys.D0 && key <= Keys.D9) || (key >= Keys.NumPad0 && key <= Keys.NumPad9);
        }

        private bool IsLetterKey(Keys key)
        {
            return key >= Keys.A && key <= Keys.Z;
        }

        private bool IsArrowKey(Keys key)
        {
            return key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right;
        }

        private bool IsSpecialKey(Keys key)
        {
            return key == Keys.Space || key == Keys.Enter || key == Keys.Tab || key == Keys.Back ||
                   key == Keys.Delete || key == Keys.Insert || key == Keys.Home || key == Keys.End ||
                   key == Keys.PageUp || key == Keys.PageDown || key == Keys.Escape || key == Keys.PrintScreen ||
                   key == Keys.Pause || key == Keys.CapsLock || key == Keys.NumLock || key == Keys.Scroll;
        }

        private void AddModifierKeys()
        {
            var modifierKeys = new[]
            {
                Keys.Control, Keys.ControlKey, Keys.LControlKey, Keys.RControlKey,
                Keys.Alt, Keys.Menu, Keys.LMenu, Keys.RMenu,
                Keys.Shift, Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey,
                Keys.LWin, Keys.RWin
            };
            
            foreach (var key in modifierKeys)
                BlockedKeys.Add(key);
        }

        private void AddFunctionKeys()
        {
            for (Keys key = Keys.F1; key <= Keys.F24; key++)
                BlockedKeys.Add(key);
        }

        private void AddNumberKeys()
        {
            for (Keys key = Keys.D0; key <= Keys.D9; key++)
                BlockedKeys.Add(key);
            
            for (Keys key = Keys.NumPad0; key <= Keys.NumPad9; key++)
                BlockedKeys.Add(key);
        }

        private void AddLetterKeys()
        {
            for (Keys key = Keys.A; key <= Keys.Z; key++)
                BlockedKeys.Add(key);
        }

        private void AddArrowKeys()
        {
            BlockedKeys.Add(Keys.Up);
            BlockedKeys.Add(Keys.Down);
            BlockedKeys.Add(Keys.Left);
            BlockedKeys.Add(Keys.Right);
        }

        private void AddSpecialKeys()
        {
            var specialKeys = new[]
            {
                Keys.Space, Keys.Enter, Keys.Tab, Keys.Back, Keys.Delete, Keys.Insert,
                Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown, Keys.Escape,
                Keys.PrintScreen, Keys.Pause, Keys.CapsLock, Keys.NumLock, Keys.Scroll
            };
            
            foreach (var key in specialKeys)
                BlockedKeys.Add(key);
        }

        /// <summary>
        /// Checks if all keyboard categories are blocked
        /// </summary>
        /// <returns>True if all categories are blocked, false otherwise</returns>
        public bool BlockAllCategories()
        {
            return BlockModifierKeys &&
                   BlockFunctionKeys &&
                   BlockNumberKeys &&
                   BlockLetterKeys &&
                   BlockArrowKeys &&
                   BlockSpecialKeys;
        }
    }
}