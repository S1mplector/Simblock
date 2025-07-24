using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimBlock.Infrastructure.Services
{
    /// <summary>
    /// JSON-based implementation of macro storage
    /// </summary>
    public class JsonMacroStorage : IMacroStorage
    {
        private readonly ILogger<JsonMacroStorage> _logger;
        private readonly string _storageDirectory;
        private readonly string _macrosFilePath;
        private readonly string _backupDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        private readonly Dictionary<Guid, Macro> _macroCache = new();
        private readonly object _lockObject = new();
        private DateTime _lastCacheUpdate = DateTime.MinValue;

        public JsonMacroStorage(ILogger<JsonMacroStorage> logger, string storageDirectory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageDirectory = storageDirectory ?? throw new ArgumentNullException(nameof(storageDirectory));
            
            _macrosFilePath = Path.Combine(_storageDirectory, "macros.json");
            _backupDirectory = Path.Combine(_storageDirectory, "backups");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            EnsureDirectoriesExist();
        }

        public async Task<IEnumerable<Macro>> GetAllMacrosAsync()
        {
            try
            {
                await EnsureCacheUpdatedAsync();
                
                lock (_lockObject)
                {
                    return _macroCache.Values.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all macros");
                throw;
            }
        }

        public async Task<Macro?> GetMacroByIdAsync(Guid id)
        {
            try
            {
                await EnsureCacheUpdatedAsync();
                
                lock (_lockObject)
                {
                    return _macroCache.TryGetValue(id, out var macro) ? macro : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get macro by ID: {MacroId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Macro>> GetMacrosByCategoryAsync(string category)
        {
            try
            {
                var allMacros = await GetAllMacrosAsync();
                return allMacros.Where(m => string.Equals(m.Category, category, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get macros by category: {Category}", category);
                throw;
            }
        }

        public async Task<IEnumerable<Macro>> GetMacrosByTagsAsync(IEnumerable<string> tags)
        {
            try
            {
                var allMacros = await GetAllMacrosAsync();
                var tagSet = new HashSet<string>(tags.Select(t => t.ToLowerInvariant()));
                
                return allMacros.Where(m => m.Tags.Any(tag => tagSet.Contains(tag)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get macros by tags");
                throw;
            }
        }

        public async Task<IEnumerable<Macro>> SearchMacrosAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllMacrosAsync();

                var allMacros = await GetAllMacrosAsync();
                var lowerSearchTerm = searchTerm.ToLowerInvariant();
                
                return allMacros.Where(m => 
                    m.Name.ToLowerInvariant().Contains(lowerSearchTerm) ||
                    (m.Description?.ToLowerInvariant().Contains(lowerSearchTerm) ?? false) ||
                    (m.Category?.ToLowerInvariant().Contains(lowerSearchTerm) ?? false) ||
                    m.Tags.Any(tag => tag.Contains(lowerSearchTerm)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search macros with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task SaveMacroAsync(Macro macro)
        {
            if (macro == null)
                throw new ArgumentNullException(nameof(macro));

            try
            {
                await EnsureCacheUpdatedAsync();
                
                lock (_lockObject)
                {
                    _macroCache[macro.Id] = macro;
                }

                await SaveCacheToFileAsync();
                
                _logger.LogDebug("Saved macro: {MacroName} ({MacroId})", macro.Name, macro.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save macro: {MacroName} ({MacroId})", macro.Name, macro.Id);
                throw;
            }
        }

        public async Task DeleteMacroAsync(Guid id)
        {
            try
            {
                await EnsureCacheUpdatedAsync();
                
                string? macroName = null;
                lock (_lockObject)
                {
                    if (_macroCache.TryGetValue(id, out var macro))
                    {
                        macroName = macro.Name;
                        _macroCache.Remove(id);
                    }
                }

                if (macroName != null)
                {
                    await SaveCacheToFileAsync();
                    _logger.LogDebug("Deleted macro: {MacroName} ({MacroId})", macroName, id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete macro: {MacroId}", id);
                throw;
            }
        }

        public async Task<bool> MacroExistsAsync(Guid id)
        {
            try
            {
                await EnsureCacheUpdatedAsync();
                
                lock (_lockObject)
                {
                    return _macroCache.ContainsKey(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if macro exists: {MacroId}", id);
                throw;
            }
        }

        public async Task<int> GetMacroCountAsync()
        {
            try
            {
                await EnsureCacheUpdatedAsync();
                
                lock (_lockObject)
                {
                    return _macroCache.Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get macro count");
                throw;
            }
        }

        public async Task<string> ExportMacrosAsync(IEnumerable<Guid> macroIds)
        {
            try
            {
                var macros = new List<Macro>();
                
                foreach (var id in macroIds)
                {
                    var macro = await GetMacroByIdAsync(id);
                    if (macro != null)
                        macros.Add(macro);
                }

                var exportData = new
                {
                    ExportedAt = DateTime.UtcNow,
                    Version = "1.0",
                    MacroCount = macros.Count,
                    Macros = macros
                };

                var json = JsonSerializer.Serialize(exportData, _jsonOptions);
                _logger.LogInformation("Exported {Count} macros", macros.Count);
                
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export macros");
                throw;
            }
        }

        public async Task<IEnumerable<Macro>> ImportMacrosAsync(string data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data))
                    throw new ArgumentException("Import data cannot be empty", nameof(data));

                using var document = JsonDocument.Parse(data);
                var root = document.RootElement;

                // Try to parse as export format first
                if (root.TryGetProperty("Macros", out var macrosElement))
                {
                    var macros = JsonSerializer.Deserialize<List<Macro>>(macrosElement.GetRawText(), _jsonOptions);
                    _logger.LogInformation("Imported {Count} macros from export format", macros?.Count ?? 0);
                    return macros ?? new List<Macro>();
                }
                
                // Try to parse as direct macro array
                var directMacros = JsonSerializer.Deserialize<List<Macro>>(data, _jsonOptions);
                _logger.LogInformation("Imported {Count} macros from direct format", directMacros?.Count ?? 0);
                return directMacros ?? new List<Macro>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import macros");
                throw;
            }
        }

        public async Task<string> CreateBackupAsync()
        {
            try
            {
                var allMacros = await GetAllMacrosAsync();
                var backupData = new
                {
                    BackupCreatedAt = DateTime.UtcNow,
                    Version = "1.0",
                    MacroCount = allMacros.Count(),
                    Macros = allMacros
                };

                var json = JsonSerializer.Serialize(backupData, _jsonOptions);
                
                // Save backup to file
                var backupFileName = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                var backupFilePath = Path.Combine(_backupDirectory, backupFileName);
                
                await File.WriteAllTextAsync(backupFilePath, json);
                
                _logger.LogInformation("Created backup with {Count} macros: {BackupFile}", allMacros.Count(), backupFileName);
                
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create backup");
                throw;
            }
        }

        public async Task RestoreFromBackupAsync(string backupData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(backupData))
                    throw new ArgumentException("Backup data cannot be empty", nameof(backupData));

                using var document = JsonDocument.Parse(backupData);
                var root = document.RootElement;

                if (!root.TryGetProperty("Macros", out var macrosElement))
                    throw new ArgumentException("Invalid backup format: missing Macros property");

                var macros = JsonSerializer.Deserialize<List<Macro>>(macrosElement.GetRawText(), _jsonOptions);
                if (macros == null)
                    throw new ArgumentException("Failed to deserialize macros from backup");

                // Clear current cache and replace with backup data
                lock (_lockObject)
                {
                    _macroCache.Clear();
                    foreach (var macro in macros)
                    {
                        _macroCache[macro.Id] = macro;
                    }
                }

                await SaveCacheToFileAsync();
                
                _logger.LogInformation("Restored {Count} macros from backup", macros.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore from backup");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetStorageStatsAsync()
        {
            try
            {
                await EnsureCacheUpdatedAsync();
                
                var fileInfo = new FileInfo(_macrosFilePath);
                var backupFiles = Directory.Exists(_backupDirectory) 
                    ? Directory.GetFiles(_backupDirectory, "*.json").Length 
                    : 0;

                lock (_lockObject)
                {
                    var macros = _macroCache.Values.ToList();
                    
                    return new Dictionary<string, object>
                    {
                        ["TotalMacros"] = macros.Count,
                        ["EnabledMacros"] = macros.Count(m => m.IsEnabled),
                        ["DisabledMacros"] = macros.Count(m => !m.IsEnabled),
                        ["TotalEvents"] = macros.Sum(m => m.Events.Count),
                        ["AverageEventsPerMacro"] = macros.Count > 0 ? macros.Average(m => m.Events.Count) : 0,
                        ["TotalExecutions"] = macros.Sum(m => m.ExecutionCount),
                        ["StorageFilePath"] = _macrosFilePath,
                        ["StorageFileSize"] = fileInfo.Exists ? fileInfo.Length : 0,
                        ["LastModified"] = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.MinValue,
                        ["BackupCount"] = backupFiles,
                        ["CacheLastUpdated"] = _lastCacheUpdate
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get storage stats");
                throw;
            }
        }

        private async Task EnsureCacheUpdatedAsync()
        {
            var fileInfo = new FileInfo(_macrosFilePath);
            
            // Check if cache needs updating
            if (!fileInfo.Exists || fileInfo.LastWriteTime <= _lastCacheUpdate)
                return;

            try
            {
                await LoadCacheFromFileAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update cache from file");
                throw;
            }
        }

        private async Task LoadCacheFromFileAsync()
        {
            if (!File.Exists(_macrosFilePath))
            {
                lock (_lockObject)
                {
                    _macroCache.Clear();
                    _lastCacheUpdate = DateTime.UtcNow;
                }
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_macrosFilePath);
                var macros = JsonSerializer.Deserialize<List<Macro>>(json, _jsonOptions) ?? new List<Macro>();

                lock (_lockObject)
                {
                    _macroCache.Clear();
                    foreach (var macro in macros)
                    {
                        _macroCache[macro.Id] = macro;
                    }
                    _lastCacheUpdate = DateTime.UtcNow;
                }

                _logger.LogDebug("Loaded {Count} macros from storage", macros.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load macros from file: {FilePath}", _macrosFilePath);
                throw;
            }
        }

        private async Task SaveCacheToFileAsync()
        {
            try
            {
                List<Macro> macrosToSave;
                
                lock (_lockObject)
                {
                    macrosToSave = _macroCache.Values.ToList();
                }

                var json = JsonSerializer.Serialize(macrosToSave, _jsonOptions);
                await File.WriteAllTextAsync(_macrosFilePath, json);
                
                _lastCacheUpdate = DateTime.UtcNow;
                
                _logger.LogDebug("Saved {Count} macros to storage", macrosToSave.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save macros to file: {FilePath}", _macrosFilePath);
                throw;
            }
        }

        private void EnsureDirectoriesExist()
        {
            try
            {
                if (!Directory.Exists(_storageDirectory))
                {
                    Directory.CreateDirectory(_storageDirectory);
                    _logger.LogDebug("Created storage directory: {Directory}", _storageDirectory);
                }

                if (!Directory.Exists(_backupDirectory))
                {
                    Directory.CreateDirectory(_backupDirectory);
                    _logger.LogDebug("Created backup directory: {Directory}", _backupDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create storage directories");
                throw;
            }
        }
    }
}
