using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimBlock.Core.Domain.Entities;

namespace SimBlock.Core.Application.Interfaces
{
    public interface IMacroMappingService
    {
        event EventHandler? BindingsChanged;

        bool Enabled { get; }
        void SetEnabled(bool enabled);

        Task<IReadOnlyList<MacroBinding>> ListBindingsAsync();
        Task<bool> AddOrUpdateBindingAsync(MacroBinding binding);
        Task<bool> RemoveBindingAsync(string bindingId);
        Task<bool> EnableBindingAsync(string bindingId, bool enabled);
        Task<bool> RemoveBindingsForMacroAsync(string macroName);
        Task<bool> ClearAllBindingsAsync();
    }
}
