 # SimBlock Auto-Update System

## Overview

The SimBlock auto-update system provides automatic checking, downloading, and installation of updates from the GitHub releases. It compares version numbers and automatically downloads newer versions when available.

## Features

- **Automatic Version Comparison**: Compares current version with latest GitHub release
- **Semantic Version Support**: Handles semantic versioning (e.g., 1.0.0, 1.0.1, 2.0.0)
- **GitHub Integration**: Fetches releases from https://github.com/S1mplector/Simblock/releases
- **Progress Tracking**: Shows download progress with percentage and file size
- **Safe Updates**: Creates backups and handles rollback on failure
- **User Interface**: Clean dialog for update notifications
- **Automatic Scheduling**: Checks for updates every 4 hours by default

## Architecture

### Core Components

1. **IAutoUpdateService**: Main service for update operations
2. **IVersionComparator**: Handles version string comparison
3. **IGitHubReleaseService**: GitHub API integration
4. **IAutoUpdateManager**: UI integration and scheduling
5. **UpdateDialog**: User interface for update notifications

### Version Comparison

The system uses semantic versioning with the following rules:
- Compares major.minor.patch versions
- Handles 'v' prefixes (e.g., v1.0.1)
- Supports pre-release versions
- Stable releases take precedence over pre-releases

## Usage

### Manual Update Check

```csharp
// Get the auto-update manager from DI container
var autoUpdateManager = serviceProvider.GetRequiredService<IAutoUpdateManager>();

// Check for updates manually (shows "no update" message if none available)
await autoUpdateManager.CheckForUpdatesAsync(showNoUpdateMessage: true);
```

### Automatic Update Checking

```csharp
// Start automatic checking every 4 hours
autoUpdateManager.StartAutomaticUpdateChecking(TimeSpan.FromHours(4));

// Stop automatic checking
autoUpdateManager.StopAutomaticUpdateChecking();
```

### Integration in Main Application

The auto-update system is automatically integrated into the application startup:

```csharp
// In Program.cs, services are registered:
services.AddSingleton<IVersionComparator, VersionComparator>();
services.AddSingleton<IGitHubReleaseService, GitHubReleaseService>();
services.AddSingleton<IAutoUpdateService, AutoUpdateService>();
services.AddSingleton<IAutoUpdateManager, AutoUpdateManager>();
```

## Configuration

### Version Information

The current version is read from the assembly version in `SimBlock.csproj`:

```xml
<Version>1.0.1</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

### GitHub Repository

The system checks releases from:
- Repository: `S1mplector/Simblock`
- API Endpoint: `https://api.github.com/repos/S1mplector/Simblock/releases`

## Update Process

1. **Check for Updates**: Compares current version with latest GitHub release
2. **User Notification**: Shows update dialog if newer version is available
3. **Download**: Downloads the release asset (executable or zip file)
4. **Backup**: Creates backup of current executable
5. **Install**: Replaces current executable with new version
6. **Restart**: Automatically restarts the application
7. **Cleanup**: Removes temporary files and backup

## Error Handling

- **Network Issues**: Gracefully handles connection failures
- **Download Failures**: Cleans up partial downloads
- **Installation Failures**: Restores from backup
- **Logging**: Comprehensive logging for troubleshooting

## Security Considerations

- **HTTPS Only**: All downloads use HTTPS
- **File Verification**: Checks file size after download
- **Backup Strategy**: Always creates backup before update
- **User Consent**: Requires user approval before updating

## Customization

### Update Frequency

Change the automatic update check interval:

```csharp
// Check every 2 hours instead of 4
autoUpdateManager.StartAutomaticUpdateChecking(TimeSpan.FromHours(2));
```

### Asset Selection

The system automatically selects the appropriate asset:
1. First priority: `.exe` files containing "SimBlock"
2. Second priority: `.zip` files containing "SimBlock"

### Version Format

Supported version formats:
- `1.0.0`
- `v1.0.0`
- `1.0.0-beta`
- `1.0.0-alpha.1`

## Troubleshooting

### Common Issues

1. **No Internet Connection**: Update check will fail silently
2. **GitHub API Rate Limiting**: Rare, but may cause temporary failures
3. **Antivirus Interference**: May block executable replacement
4. **Insufficient Permissions**: May fail to replace executable

### Logging

Check the application logs for detailed error information:
- Update check attempts
- Download progress
- Installation success/failure
- Error details

## Example Implementation

Here's how to add a "Check for Updates" menu item:

```csharp
private async void CheckForUpdatesMenuItem_Click(object sender, EventArgs e)
{
    var autoUpdateManager = _serviceProvider.GetRequiredService<IAutoUpdateManager>();
    await autoUpdateManager.CheckForUpdatesAsync(showNoUpdateMessage: true);
}
```

## Future Enhancements

- **Delta Updates**: Download only changed files
- **Signature Verification**: Verify digital signatures
- **Rollback Feature**: Easy rollback to previous version
- **Update Channels**: Support for beta/stable channels
- **Offline Updates**: Support for manual update files