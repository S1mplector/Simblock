namespace SimBlock.Core.Domain.Enums
{
    /// <summary>
    /// Represents how a macro should be executed
    /// </summary>
    public enum MacroExecutionMode
    {
        /// <summary>
        /// Execute once and stop
        /// </summary>
        Once,

        /// <summary>
        /// Execute a specific number of times
        /// </summary>
        Repeat,

        /// <summary>
        /// Execute continuously until stopped
        /// </summary>
        Loop,

        /// <summary>
        /// Execute until a specific condition is met
        /// </summary>
        UntilCondition,

        /// <summary>
        /// Execute at specific intervals
        /// </summary>
        Interval,

        /// <summary>
        /// Execute with random delays between repetitions
        /// </summary>
        RandomInterval
    }
}
