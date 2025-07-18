<div align="center">
    <img src="src/Presentation/Resources/Images/logo.png" width="128">
    <br><br>
</div>

# SimBlock - Simple Input Device Blocker Utility

A very lightweight Windows application that allows you to temporarily block all keyboard 
and mouse input globally with a single toggle button, designed to safely clean your peripherals.

## Features

-  **One-Click Toggle** - Simple button to block/unblock keyboard / mouse input
-  **Global Blocking** - Blocks all input system-wide
-  **Emergency Unlock** - Key combination of choice to revert the block state in case of issues
-  **System Tray Integration** - Minimize to tray and toggle from tray icon
-  **Windows Native** - Uses Win32 API for reliable keyboard and mouse hooking

## Screenshots

<div align="center">
  <img src="docs/images/main_interface.png" alt="Main Interface" width="600">
  <p><i>Main interface</i></p>
  
   <img src="docs/images/settings.png" alt="Settings" width="400">
   <p><i>User configurable settings</i></p>

   <img src="docs/images/tray.png" alt="Tray" width="300">
  <p><i>Minimized to tray</i></p>
</div>

## Emergency Unlock

If the application becomes unresponsive or you need to quickly unlock:
- Press **Ctrl+Alt+U** (by default) **three times in quick succession** (within 2 seconds) to immediately unlock the keyboard. The shortcut can be changed in settings.
- The triple-press requirement prevents accidental unlocks

## Architecture

The application follows Clean Architecture principles:

```
src/
├── Core/
│   ├── Domain/          # Business entities and interfaces
│   └── Application/     # Application services and use cases
├── Infrastructure/      # Platform-specific implementations
│   └── Windows/         # Windows API integration
└── Presentation/        # UI layer (WinForms)
    ├── Forms/           # Windows Forms
    └── ViewModels/      # View models
```

## Technologies Used

- **.NET 8** - Target framework
- **Windows Forms** - GUI framework
- **Microsoft.Extensions.Hosting** - Dependency injection and configuration
- **Win32 API** - Low-level keyboard hooking


## Building and Running

1. **Prerequisites:**
   - Windows 10/11
   - .NET 8 SDK

2. **Build:**
   ```powershell
   dotnet build
   ```

3. **Run:**
   ```powershell
   dotnet run
   ```

## Usage

1. Launch the application
2. Click "Block Keyboard" or "Block Mouse" toggle buttons to prevent all peripheral input
3. Clean your devices safely
4. Click the toggle button again or use the emergency combination
5. Minimize to system tray for easy access, click on the icon to toggle state

## Security Note

This application requires administrative privileges to install global keyboard hooks. It only blocks keyboard input locally and does not transmit any data. You are welcome to inspect the source code. 

