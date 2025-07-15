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
    }
}