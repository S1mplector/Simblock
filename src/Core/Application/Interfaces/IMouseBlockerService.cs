using SimBlock.Core.Domain.Entities;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Core.Application.Interfaces
{
    /// <summary>
    /// Main mouse application service interface
    /// </summary>
    public interface IMouseBlockerService
    {
        event EventHandler<MouseBlockState>? StateChanged;
        event EventHandler<int>? EmergencyUnlockAttempt;
        event EventHandler? ShowWindowRequested;
        
        MouseBlockState CurrentState { get; }
        
        Task InitializeAsync();
        Task InitializeAsync(IInitializationProgress? progress);
        Task ShutdownAsync();
        Task ToggleBlockingAsync();
        Task SetBlockingAsync(bool shouldBlock);
        Task SetSimpleModeAsync();
        Task SetAdvancedModeAsync(AdvancedMouseConfiguration config);
        Task SetSelectModeAsync(AdvancedMouseConfiguration config);
        Task ApplySelectionAsync();
        Task ShowMainWindowAsync();
        Task HideToTrayAsync();
    }
}