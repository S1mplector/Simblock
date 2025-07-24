namespace SimBlock.Core.Domain.Enums
{
    /// <summary>
    /// Represents how a macro can be triggered
    /// </summary>
    public enum MacroTriggerType
    {
        /// <summary>
        /// Manual trigger - macro must be started manually
        /// </summary>
        Manual,

        /// <summary>
        /// Hotkey trigger - macro is triggered by a key combination
        /// </summary>
        Hotkey,

        /// <summary>
        /// Time-based trigger - macro runs at scheduled times
        /// </summary>
        Scheduled,

        /// <summary>
        /// Event-based trigger - macro runs when specific events occur
        /// </summary>
        Event,

        /// <summary>
        /// Window-based trigger - macro runs when specific windows are active
        /// </summary>
        WindowFocus,

        /// <summary>
        /// Application startup trigger
        /// </summary>
        Startup,

        /// <summary>
        /// Chain trigger - macro runs after another macro completes
        /// </summary>
        Chain
    }
}
