using SimBlock.Core.Domain.Entities;

namespace SimBlock.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for low-level keyboard hook operations
    /// </summary>
    public interface IKeyboardHookService
    {
        event EventHandler<KeyboardBlockState>? BlockStateChanged;
        
        bool IsHookInstalled { get; }
        KeyboardBlockState CurrentState { get; }
        
        Task InstallHookAsync();
        Task UninstallHookAsync();
        Task SetBlockingAsync(bool shouldBlock, string? reason = null);
        Task ToggleBlockingAsync(string? reason = null);
    }
}
