using SimBlock.Core.Domain.Entities;

namespace SimBlock.Core.Application.Interfaces
{
    /// <summary>
    /// Interface for macro storage operations
    /// </summary>
    public interface IMacroStorage
    {
        /// <summary>
        /// Gets all macros from storage
        /// </summary>
        Task<IEnumerable<Macro>> GetAllMacrosAsync();

        /// <summary>
        /// Gets a macro by its ID
        /// </summary>
        Task<Macro?> GetMacroByIdAsync(Guid id);

        /// <summary>
        /// Gets macros by category
        /// </summary>
        Task<IEnumerable<Macro>> GetMacrosByCategoryAsync(string category);

        /// <summary>
        /// Gets macros that match the specified tags
        /// </summary>
        Task<IEnumerable<Macro>> GetMacrosByTagsAsync(IEnumerable<string> tags);

        /// <summary>
        /// Searches macros by name or description
        /// </summary>
        Task<IEnumerable<Macro>> SearchMacrosAsync(string searchTerm);

        /// <summary>
        /// Saves a macro to storage
        /// </summary>
        Task SaveMacroAsync(Macro macro);

        /// <summary>
        /// Deletes a macro from storage
        /// </summary>
        Task DeleteMacroAsync(Guid id);

        /// <summary>
        /// Checks if a macro exists
        /// </summary>
        Task<bool> MacroExistsAsync(Guid id);

        /// <summary>
        /// Gets the total number of macros
        /// </summary>
        Task<int> GetMacroCountAsync();

        /// <summary>
        /// Exports macros to a file or stream
        /// </summary>
        Task<string> ExportMacrosAsync(IEnumerable<Guid> macroIds);

        /// <summary>
        /// Imports macros from a file or stream
        /// </summary>
        Task<IEnumerable<Macro>> ImportMacrosAsync(string data);

        /// <summary>
        /// Creates a backup of all macros
        /// </summary>
        Task<string> CreateBackupAsync();

        /// <summary>
        /// Restores macros from a backup
        /// </summary>
        Task RestoreFromBackupAsync(string backupData);

        /// <summary>
        /// Gets storage statistics
        /// </summary>
        Task<Dictionary<string, object>> GetStorageStatsAsync();
    }
}
