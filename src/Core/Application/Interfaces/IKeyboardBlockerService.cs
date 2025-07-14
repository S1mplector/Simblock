using SimBlock.Core.Domain.Entities;

namespace SimBlock.Core.Application.Interfaces
{
    /// <summary>
    /// Main application service interface
    /// </summary>
    public interface IKeyboardBlockerService
    {
        event EventHandler<KeyboardBlockState>? StateChanged;
        event EventHandler<int>? EmergencyUnlockAttempt;
        event EventHandler? ShowWindowRequested;
        
        KeyboardBlockState CurrentState { get; }
        
        Task InitializeAsync();
        Task ShutdownAsync();
        Task ToggleBlockingAsync();
        Task SetBlockingAsync(bool shouldBlock);
        Task ShowMainWindowAsync();
        Task HideToTrayAsync();
    }
}
