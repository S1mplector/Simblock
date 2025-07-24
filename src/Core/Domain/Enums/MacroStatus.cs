namespace SimBlock.Core.Domain.Enums
{
    /// <summary>
    /// Represents the current status of a macro
    /// </summary>
    public enum MacroStatus
    {
        /// <summary>
        /// Macro is idle and ready to be executed
        /// </summary>
        Idle,

        /// <summary>
        /// Macro is currently being recorded
        /// </summary>
        Recording,

        /// <summary>
        /// Macro is currently being played back
        /// </summary>
        Playing,

        /// <summary>
        /// Macro playback is paused
        /// </summary>
        Paused,

        /// <summary>
        /// Macro execution was stopped by user
        /// </summary>
        Stopped,

        /// <summary>
        /// Macro execution completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Macro execution failed with an error
        /// </summary>
        Failed
    }
}
