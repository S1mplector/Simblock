namespace SimBlock.Core.Domain.Enums
{
    /// <summary>
    /// Defines the blocking mode for keyboard and mouse input
    /// </summary>
    public enum BlockingMode
    {
        /// <summary>
        /// Simple mode - blocks all input when enabled (original behavior)
        /// </summary>
        Simple,
        
        /// <summary>
        /// Advanced mode - blocks only selected keys/mouse actions
        /// </summary>
        Advanced
    }
}