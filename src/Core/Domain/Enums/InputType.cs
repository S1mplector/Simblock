namespace SimBlock.Core.Domain.Enums
{
    /// <summary>
    /// Represents the type of input event in a macro
    /// </summary>
    public enum InputType
    {
        /// <summary>
        /// Keyboard key press event
        /// </summary>
        KeyDown,

        /// <summary>
        /// Keyboard key release event
        /// </summary>
        KeyUp,

        /// <summary>
        /// Mouse button press event
        /// </summary>
        MouseDown,

        /// <summary>
        /// Mouse button release event
        /// </summary>
        MouseUp,

        /// <summary>
        /// Mouse movement event
        /// </summary>
        MouseMove,

        /// <summary>
        /// Mouse wheel scroll event
        /// </summary>
        MouseWheel,

        /// <summary>
        /// Delay/wait event
        /// </summary>
        Delay,

        /// <summary>
        /// Text input event (for typing strings)
        /// </summary>
        TextInput,

        /// <summary>
        /// Custom script execution event
        /// </summary>
        Script,

        /// <summary>
        /// Variable assignment event
        /// </summary>
        VariableSet,

        /// <summary>
        /// Conditional logic event
        /// </summary>
        Condition,

        /// <summary>
        /// Loop start event
        /// </summary>
        LoopStart,

        /// <summary>
        /// Loop end event
        /// </summary>
        LoopEnd
    }
}
