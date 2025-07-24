using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;
using SimBlock.Core.Domain.Events;
using SimBlock.Presentation.Commands;

namespace SimBlock.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel for managing macros
    /// </summary>
    public class MacroManagerViewModel : INotifyPropertyChanged
    {
        private readonly IMacroService _macroService;
        private bool _isRecording;
        private bool _isPlaying;
        private Macro? _selectedMacro;
        private string _statusMessage = string.Empty;
        private string _searchText = string.Empty;
        private string _selectedCategory = string.Empty;
        private bool _isLoading;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets all available macros
        /// </summary>
        public ObservableCollection<Macro> Macros { get; } = new();

        /// <summary>
        /// Gets filtered macros based on search and category
        /// </summary>
        public ObservableCollection<Macro> FilteredMacros { get; } = new();

        /// <summary>
        /// Gets all available categories
        /// </summary>
        public ObservableCollection<string> Categories { get; } = new();

        /// <summary>
        /// Gets or sets the currently selected macro
        /// </summary>
        public Macro? SelectedMacro
        {
            get => _selectedMacro;
            set
            {
                if (_selectedMacro != value)
                {
                    _selectedMacro = value;
                    OnPropertyChanged(nameof(SelectedMacro));
                    OnPropertyChanged(nameof(IsMacroSelected));
                    OnPropertyChanged(nameof(CanEditMacro));
                    OnPropertyChanged(nameof(CanDeleteMacro));
                    OnPropertyChanged(nameof(CanPlayMacro));
                    OnPropertyChanged(nameof(CanDuplicateMacro));
                    OnPropertyChanged(nameof(CanExportMacro));
                }
            }
        }

        /// <summary>
        /// Gets or sets the search text for filtering macros
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    FilterMacros();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected category for filtering
        /// </summary>
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged(nameof(SelectedCategory));
                    FilterMacros();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        /// <summary>
        /// Gets whether a macro is currently selected
        /// </summary>
        public bool IsMacroSelected => SelectedMacro != null;

        /// <summary>
        /// Gets whether a macro can be edited (selected and not currently recording/playing)
        /// </summary>
        public bool CanEditMacro => IsMacroSelected && !IsRecording && !IsPlaying;

        /// <summary>
        /// Gets whether a macro can be deleted (selected and not currently recording/playing)
        /// </summary>
        public bool CanDeleteMacro => IsMacroSelected && !IsRecording && !IsPlaying;

        /// <summary>
        /// Gets whether a macro can be played (selected and not currently recording/playing)
        /// </summary>
        public bool CanPlayMacro => IsMacroSelected && !IsRecording && !IsPlaying && SelectedMacro!.IsEnabled;

        /// <summary>
        /// Gets whether a macro can be duplicated
        /// </summary>
        public bool CanDuplicateMacro => IsMacroSelected && !IsRecording;

        /// <summary>
        /// Gets whether a macro can be exported
        /// </summary>
        public bool CanExportMacro => IsMacroSelected;

        /// <summary>
        /// Gets or sets whether a macro is currently being recorded
        /// </summary>
        public bool IsRecording
        {
            get => _isRecording;
            private set
            {
                if (_isRecording != value)
                {
                    _isRecording = value;
                    OnPropertyChanged(nameof(IsRecording));
                    OnPropertyChanged(nameof(IsIdle));
                    OnPropertyChanged(nameof(CanRecord));
                    OnPropertyChanged(nameof(CanEditMacro));
                    OnPropertyChanged(nameof(CanDeleteMacro));
                    OnPropertyChanged(nameof(CanPlayMacro));
                    OnPropertyChanged(nameof(CanDuplicateMacro));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether a macro is currently playing
        /// </summary>
        public bool IsPlaying
        {
            get => _isPlaying;
            private set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged(nameof(IsPlaying));
                    OnPropertyChanged(nameof(IsIdle));
                    OnPropertyChanged(nameof(CanRecord));
                    OnPropertyChanged(nameof(CanEditMacro));
                    OnPropertyChanged(nameof(CanDeleteMacro));
                    OnPropertyChanged(nameof(CanPlayMacro));
                }
            }
        }

        /// <summary>
        /// Gets whether the system is idle (not recording or playing)
        /// </summary>
        public bool IsIdle => !IsRecording && !IsPlaying;

        /// <summary>
        /// Gets whether recording can be started
        /// </summary>
        public bool CanRecord => IsIdle;

        /// <summary>
        /// Gets or sets whether the view model is loading data
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        #region Commands

        /// <summary>
        /// Command to start recording a new macro
        /// </summary>
        public ICommand StartRecordingCommand { get; }

        /// <summary>
        /// Command to stop recording
        /// </summary>
        public ICommand StopRecordingCommand { get; }

        /// <summary>
        /// Command to play the selected macro
        /// </summary>
        public ICommand PlayMacroCommand { get; }

        /// <summary>
        /// Command to stop macro playback
        /// </summary>
        public ICommand StopPlaybackCommand { get; }

        /// <summary>
        /// Command to edit the selected macro
        /// </summary>
        public ICommand EditMacroCommand { get; }

        /// <summary>
        /// Command to delete the selected macro
        /// </summary>
        public ICommand DeleteMacroCommand { get; }

        /// <summary>
        /// Command to duplicate the selected macro
        /// </summary>
        public ICommand DuplicateMacroCommand { get; }

        /// <summary>
        /// Command to create a new empty macro
        /// </summary>
        public ICommand CreateMacroCommand { get; }

        /// <summary>
        /// Command to refresh the macro list
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Command to export selected macros
        /// </summary>
        public ICommand ExportMacrosCommand { get; }

        /// <summary>
        /// Command to import macros
        /// </summary>
        public ICommand ImportMacrosCommand { get; }

        /// <summary>
        /// Command to clear search filters
        /// </summary>
        public ICommand ClearFiltersCommand { get; }

        #endregion

        public MacroManagerViewModel(IMacroService macroService)
        {
            _macroService = macroService ?? throw new ArgumentNullException(nameof(macroService));

            // Initialize commands
            StartRecordingCommand = new AsyncRelayCommand(StartRecordingAsync, () => CanRecord);
            StopRecordingCommand = new AsyncRelayCommand(StopRecordingAsync, () => IsRecording);
            PlayMacroCommand = new AsyncRelayCommand(PlayMacroAsync, () => CanPlayMacro);
            StopPlaybackCommand = new AsyncRelayCommand(StopPlaybackAsync, () => IsPlaying);
            EditMacroCommand = new RelayCommand(EditMacro, () => CanEditMacro);
            DeleteMacroCommand = new AsyncRelayCommand(DeleteMacroAsync, () => CanDeleteMacro);
            DuplicateMacroCommand = new AsyncRelayCommand(DuplicateMacroAsync, () => CanDuplicateMacro);
            CreateMacroCommand = new AsyncRelayCommand(CreateMacroAsync, () => IsIdle);
            RefreshCommand = new AsyncRelayCommand(RefreshMacrosAsync);
            ExportMacrosCommand = new AsyncRelayCommand(ExportMacrosAsync, () => CanExportMacro);
            ImportMacrosCommand = new AsyncRelayCommand(ImportMacrosAsync);
            ClearFiltersCommand = new RelayCommand(ClearFilters);

            // Subscribe to macro service events
            _macroService.RecordingStarted += OnRecordingStarted;
            _macroService.RecordingStopped += OnRecordingStopped;
            _macroService.PlaybackStarted += OnPlaybackStarted;
            _macroService.PlaybackStopped += OnPlaybackStopped;
            _macroService.MacroCollectionChanged += OnMacroCollectionChanged;

            // Load existing macros
            _ = Task.Run(LoadMacrosAsync);
        }

        #region Command Implementations

        private async Task StartRecordingAsync()
        {
            try
            {
                var macroName = await ShowInputDialogAsync("New Macro", "Enter macro name:", $"Macro {DateTime.Now:yyyyMMdd_HHmmss}");
                if (string.IsNullOrWhiteSpace(macroName))
                    return;

                var description = await ShowInputDialogAsync("Macro Description", "Enter description (optional):", "");
                var category = await ShowInputDialogAsync("Macro Category", "Enter category (optional):", "");

                StatusMessage = "Starting recording...";
                await _macroService.StartRecordingAsync(macroName, description, category);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to start recording: {ex.Message}";
                await ShowErrorDialogAsync("Recording Error", ex.Message);
            }
        }

        private async Task StopRecordingAsync()
        {
            try
            {
                StatusMessage = "Stopping recording...";
                var macro = await _macroService.StopRecordingAsync();
                if (macro != null)
                {
                    StatusMessage = $"Recording completed: {macro.Name} ({macro.Events.Count} events)";
                    await RefreshMacrosAsync();
                    SelectedMacro = Macros.FirstOrDefault(m => m.Id == macro.Id);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to stop recording: {ex.Message}";
                await ShowErrorDialogAsync("Recording Error", ex.Message);
            }
        }

        private async Task PlayMacroAsync()
        {
            if (SelectedMacro == null)
                return;

            try
            {
                StatusMessage = $"Playing macro: {SelectedMacro.Name}";
                await _macroService.PlayMacroAsync(SelectedMacro);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to play macro: {ex.Message}";
                await ShowErrorDialogAsync("Playback Error", ex.Message);
            }
        }

        private async Task StopPlaybackAsync()
        {
            try
            {
                StatusMessage = "Stopping playback...";
                await _macroService.StopPlaybackAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to stop playback: {ex.Message}";
                await ShowErrorDialogAsync("Playback Error", ex.Message);
            }
        }

        private void EditMacro()
        {
            if (SelectedMacro == null)
                return;

            // This would typically open a macro editor dialog
            StatusMessage = $"Editing macro: {SelectedMacro.Name}";
            // TODO: Implement macro editor dialog
        }

        private async Task DeleteMacroAsync()
        {
            if (SelectedMacro == null)
                return;

            try
            {
                var confirmed = await ShowConfirmDialogAsync("Delete Macro", 
                    $"Are you sure you want to delete the macro '{SelectedMacro.Name}'?");
                
                if (!confirmed)
                    return;

                StatusMessage = $"Deleting macro: {SelectedMacro.Name}";
                await _macroService.DeleteMacroAsync(SelectedMacro.Id);
                StatusMessage = $"Macro deleted: {SelectedMacro.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete macro: {ex.Message}";
                await ShowErrorDialogAsync("Delete Error", ex.Message);
            }
        }

        private async Task DuplicateMacroAsync()
        {
            if (SelectedMacro == null)
                return;

            try
            {
                var newName = await ShowInputDialogAsync("Duplicate Macro", "Enter name for the duplicate:", $"{SelectedMacro.Name} (Copy)");
                if (string.IsNullOrWhiteSpace(newName))
                    return;

                StatusMessage = $"Duplicating macro: {SelectedMacro.Name}";
                var duplicatedMacro = await _macroService.DuplicateMacroAsync(SelectedMacro.Id, newName);
                StatusMessage = $"Macro duplicated: {duplicatedMacro.Name}";
                
                await RefreshMacrosAsync();
                SelectedMacro = Macros.FirstOrDefault(m => m.Id == duplicatedMacro.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to duplicate macro: {ex.Message}";
                await ShowErrorDialogAsync("Duplicate Error", ex.Message);
            }
        }

        private async Task CreateMacroAsync()
        {
            try
            {
                var macroName = await ShowInputDialogAsync("New Macro", "Enter macro name:", $"New Macro {DateTime.Now:yyyyMMdd_HHmmss}");
                if (string.IsNullOrWhiteSpace(macroName))
                    return;

                var description = await ShowInputDialogAsync("Macro Description", "Enter description (optional):", "");
                var category = await ShowInputDialogAsync("Macro Category", "Enter category (optional):", "");

                StatusMessage = "Creating macro...";
                var macro = await _macroService.CreateMacroAsync(macroName, description, category);
                StatusMessage = $"Macro created: {macro.Name}";
                
                await RefreshMacrosAsync();
                SelectedMacro = Macros.FirstOrDefault(m => m.Id == macro.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to create macro: {ex.Message}";
                await ShowErrorDialogAsync("Create Error", ex.Message);
            }
        }

        private async Task RefreshMacrosAsync()
        {
            await LoadMacrosAsync();
        }

        private async Task ExportMacrosAsync()
        {
            if (SelectedMacro == null)
                return;

            try
            {
                StatusMessage = "Exporting macro...";
                var exportData = await _macroService.ExportMacrosAsync(new[] { SelectedMacro.Id });
                
                // TODO: Show save file dialog and save the export data
                StatusMessage = $"Macro exported: {SelectedMacro.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to export macro: {ex.Message}";
                await ShowErrorDialogAsync("Export Error", ex.Message);
            }
        }

        private async Task ImportMacrosAsync()
        {
            try
            {
                // TODO: Show open file dialog and read the import data
                StatusMessage = "Import functionality not yet implemented";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to import macros: {ex.Message}";
                await ShowErrorDialogAsync("Import Error", ex.Message);
            }
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategory = string.Empty;
        }

        #endregion

        #region Private Methods

        private async Task LoadMacrosAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading macros...";

                var macros = await _macroService.GetAllMacrosAsync();
                var categories = await _macroService.GetCategoriesAsync();

                // Update collections on UI thread (WinForms)
                if (System.Windows.Forms.Application.OpenForms.Count > 0)
                {
                    var mainForm = System.Windows.Forms.Application.OpenForms[0];
                    if (mainForm.InvokeRequired)
                    {
                        mainForm.Invoke(new Action(() =>
                        {
                            UpdateCollections(macros, categories);
                        }));
                    }
                    else
                    {
                        UpdateCollections(macros, categories);
                    }
                }
                else
                {
                    UpdateCollections(macros, categories);
                }

                StatusMessage = $"Loaded {macros.Count()} macros";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load macros: {ex.Message}";
                await ShowErrorDialogAsync("Load Error", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateCollections(IEnumerable<Macro> macros, IEnumerable<string> categories)
        {
            Macros.Clear();
            foreach (var macro in macros.OrderBy(m => m.Name))
            {
                Macros.Add(macro);
            }

            Categories.Clear();
            Categories.Add("All Categories");
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            FilterMacros();
        }

        private void FilterMacros()
        {
            FilteredMacros.Clear();

            var filtered = Macros.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                filtered = filtered.Where(m => 
                    m.Name.ToLowerInvariant().Contains(searchLower) ||
                    (m.Description?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                    m.Tags.Any(tag => tag.Contains(searchLower)));
            }

            // Apply category filter
            if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All Categories")
            {
                filtered = filtered.Where(m => string.Equals(m.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var macro in filtered)
            {
                FilteredMacros.Add(macro);
            }
        }

        private void OnRecordingStarted(object? sender, MacroRecordingStartedEvent e)
        {
            IsRecording = true;
            StatusMessage = $"Recording started: {e.Macro.Name}";
        }

        private void OnRecordingStopped(object? sender, MacroRecordingStoppedEvent e)
        {
            IsRecording = false;
            StatusMessage = $"Recording stopped: {e.Macro.Name} ({e.EventCount} events, {e.Duration.TotalSeconds:F1}s)";
        }

        private void OnPlaybackStarted(object? sender, MacroPlaybackStartedEvent e)
        {
            IsPlaying = true;
            StatusMessage = $"Playback started: {e.Macro.Name}";
        }

        private void OnPlaybackStopped(object? sender, MacroPlaybackStoppedEvent e)
        {
            IsPlaying = false;
            StatusMessage = e.WasSuccessful 
                ? $"Playback completed: {e.Macro.Name}"
                : $"Playback failed: {e.Macro.Name} - {e.ErrorMessage}";
        }

        private void OnMacroCollectionChanged(object? sender, MacroCollectionChangedEvent e)
        {
            // Refresh the macro list when the collection changes
            _ = Task.Run(RefreshMacrosAsync);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // These would be implemented to show dialogs in the actual application
        private async Task<string?> ShowInputDialogAsync(string title, string prompt, string defaultValue)
        {
            // TODO: Implement input dialog
            await Task.CompletedTask;
            return defaultValue;
        }

        private async Task<bool> ShowConfirmDialogAsync(string title, string message)
        {
            // TODO: Implement confirmation dialog
            await Task.CompletedTask;
            return true;
        }

        private async Task ShowErrorDialogAsync(string title, string message)
        {
            // TODO: Implement error dialog
            await Task.CompletedTask;
        }

        #endregion
    }
}
