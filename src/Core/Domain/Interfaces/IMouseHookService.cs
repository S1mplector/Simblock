using SimBlock.Core.Domain.Entities;

namespace SimBlock.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for low-level mouse hook operations
    /// </summary>
    public interface IMouseHookService
    {
        event EventHandler<MouseBlockState>? BlockStateChanged;
        event EventHandler<int>? EmergencyUnlockAttempt;
        
        bool IsHookInstalled { get; }
        MouseBlockState CurrentState { get; }
        
        Task InstallHookAsync();
        Task UninstallHookAsync();
        Task SetBlockingAsync(bool shouldBlock, string? reason = null);
        Task ToggleBlockingAsync(string? reason = null);
        Task SetSimpleModeAsync(string? reason = null);
        Task SetAdvancedModeAsync(AdvancedMouseConfiguration config, string? reason = null);
    }
}