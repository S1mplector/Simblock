using SimBlock.Core.Domain.Entities;

namespace SimBlock.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for low-level keyboard hook operations
    /// </summary>
    public interface IKeyboardHookService
    {
        event EventHandler<KeyboardBlockState>? BlockStateChanged;
        event EventHandler<int>? EmergencyUnlockAttempt;
        
        bool IsHookInstalled { get; }
        KeyboardBlockState CurrentState { get; }
        
        Task InstallHookAsync();
        Task UninstallHookAsync();
        Task SetBlockingAsync(bool shouldBlock, string? reason = null);
        Task ToggleBlockingAsync(string? reason = null);
        Task SetSimpleModeAsync(string? reason = null);
        Task SetAdvancedModeAsync(AdvancedKeyboardConfiguration config, string? reason = null);
    }
}
