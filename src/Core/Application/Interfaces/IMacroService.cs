using System.Collections.Generic;
using System.Threading.Tasks;
using SimBlock.Core.Domain.Entities;

namespace SimBlock.Core.Application.Interfaces
{
    public interface IMacroService
    {
        bool IsRecording { get; }
        Macro? CurrentRecording { get; }

        void StartRecording(string name);
        Macro StopRecording();

        Task SaveAsync(Macro macro);
        Task<Macro?> LoadAsync(string name);
        Task<IReadOnlyList<string>> ListAsync();

        // Playback stub (to be implemented later)
        Task PlayAsync(Macro macro);
    }
}
