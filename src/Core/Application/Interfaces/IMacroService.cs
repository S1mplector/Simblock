using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SimBlock.Core.Domain.Entities;

namespace SimBlock.Core.Application.Interfaces
{
    public interface IMacroService
    {
        bool IsRecording { get; }
        bool IsPlaying { get; }
        Macro? CurrentRecording { get; }

        void StartRecording(string name);
        Macro StopRecording();

        Task SaveAsync(Macro macro);
        Task<Macro?> LoadAsync(string name);
        Task<IReadOnlyList<string>> ListAsync();

        // Enhanced listing with metadata
        Task<IReadOnlyList<MacroInfo>> ListInfoAsync();

        // Management operations
        Task<bool> DeleteAsync(string name);
        Task<bool> RenameAsync(string oldName, string newName);
        Task<bool> ExistsAsync(string name);
        bool ValidateName(string name, out string? errorMessage);

        // Import/Export
        Task<bool> ImportAsync(string filePath, bool overwrite = false);
        Task<bool> ExportAsync(string name, string destinationPath, bool overwrite = false);

        // Playback stub (to be implemented later)
        Task PlayAsync(Macro macro);
        Task PlayAsync(Macro macro, CancellationToken cancellationToken, double speed = 1.0, int loops = 1);
    }
}
