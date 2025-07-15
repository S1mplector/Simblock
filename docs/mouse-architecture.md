# Mouse Architecture Design

## Overview

The mouse domain interfaces and entities have been designed to mirror the existing keyboard architecture, providing consistent patterns and seamless integration with the SimBlock application. This document outlines the complete mouse architecture design and integration points.

## Architecture Components

### 1. Domain Entity

#### [`MouseBlockState`](../src/Core/Domain/Entities/MouseBlockState.cs)
- **Location**: `src/Core/Domain/Entities/MouseBlockState.cs`
- **Purpose**: Represents the current state of mouse blocking
- **Design**: Exact mirror of [`KeyboardBlockState`](../src/Core/Domain/Entities/KeyboardBlockState.cs)

**Key Properties:**
- `IsBlocked`: Current blocking state
- `LastToggleTime`: When the state was last changed
- `LastToggleReason`: Reason for the last state change

**Key Methods:**
- `SetBlocked(bool, string?)`: Set blocking state with reason
- `Toggle(string?)`: Toggle current state with reason

### 2. Domain Interfaces

#### [`IMouseHookService`](../src/Core/Domain/Interfaces/IMouseHookService.cs)
- **Location**: `src/Core/Domain/Interfaces/IMouseHookService.cs`
- **Purpose**: Low-level mouse hook operations
- **Design**: Mirrors [`IKeyboardHookService`](../src/Core/Domain/Interfaces/IKeyboardHookService.cs)

**Events:**
- `BlockStateChanged`: Fired when mouse block state changes
- `EmergencyUnlockAttempt`: Fired on emergency unlock attempts

**Properties:**
- `IsHookInstalled`: Hook installation status
- `CurrentState`: Current mouse block state

**Methods:**
- `InstallHookAsync()`: Install mouse hook
- `UninstallHookAsync()`: Uninstall mouse hook
- `SetBlockingAsync(bool, string?)`: Set blocking state
- `ToggleBlockingAsync(string?)`: Toggle blocking state

#### [`IMouseInfoService`](../src/Core/Domain/Interfaces/IMouseInfoService.cs)
- **Location**: `src/Core/Domain/Interfaces/IMouseInfoService.cs`
- **Purpose**: Retrieve mouse device information
- **Design**: Adapted from [`IKeyboardInfoService`](../src/Core/Domain/Interfaces/IKeyboardInfoService.cs) for mouse-specific data

**Methods:**
- `GetCurrentMouseNameAsync()`: Get mouse device name
- `GetMouseButtonCountAsync()`: Get number of mouse buttons
- `GetMouseDpiAsync()`: Get mouse DPI setting
- `HasScrollWheelAsync()`: Check for scroll wheel presence
- `GetConnectionTypeAsync()`: Get connection type (USB, Bluetooth, PS/2)

### 3. Application Interface

#### [`IMouseBlockerService`](../src/Core/Application/Interfaces/IMouseBlockerService.cs)
- **Location**: `src/Core/Application/Interfaces/IMouseBlockerService.cs`
- **Purpose**: Main mouse application service
- **Design**: Mirrors [`IKeyboardBlockerService`](../src/Core/Application/Interfaces/IKeyboardBlockerService.cs)

**Events:**
- `StateChanged`: Mouse block state changes
- `EmergencyUnlockAttempt`: Emergency unlock attempts
- `ShowWindowRequested`: Window display requests

**Properties:**
- `CurrentState`: Current mouse block state

**Methods:**
- `InitializeAsync()`: Initialize mouse blocking service
- `ShutdownAsync()`: Shutdown mouse blocking service
- `ToggleBlockingAsync()`: Toggle mouse blocking
- `SetBlockingAsync(bool)`: Set mouse blocking state
- `ShowMainWindowAsync()`: Show main window
- `HideToTrayAsync()`: Hide to system tray

## Design Patterns and Consistency

### 1. Architectural Consistency
- **Layered Architecture**: Follows Domain-Driven Design principles
- **Dependency Injection**: Compatible with existing DI container
- **Async/Await**: All operations are asynchronous
- **Event-Driven**: Uses events for state change notifications

### 2. Naming Conventions
- **Interfaces**: Prefixed with `I` and suffixed with `Service`
- **Events**: Past tense naming (e.g., `BlockStateChanged`)
- **Methods**: Async methods suffixed with `Async`
- **Properties**: Clear, descriptive names

### 3. Error Handling
- **Exceptions**: Will follow existing exception handling patterns
- **Logging**: Compatible with existing logging infrastructure
- **Graceful Degradation**: Fallback mechanisms for missing functionality

## Integration Points

### 1. Dependency Injection
Mouse services will be registered in the DI container alongside keyboard services:

```csharp
// In Program.cs or ServiceConfiguration
services.AddScoped<IMouseHookService, WindowsMouseHookService>();
services.AddScoped<IMouseBlockerService, MouseBlockerService>();
services.AddScoped<IMouseInfoService, WindowsMouseInfoService>();
```

### 2. System Tray Integration
Mouse services will integrate with the existing [`ISystemTrayService`](../src/Core/Domain/Interfaces/ISystemTrayService.cs):
- Separate tray icon or combined with keyboard
- Context menu integration
- State notifications

### 3. Main Application Service
A combined service or separate mouse service will orchestrate:
- Mouse hook management
- State synchronization
- UI updates
- Tray notifications

### 4. Windows Platform Integration
Mouse services will use Windows APIs similar to keyboard implementation:
- **Low-level mouse hooks**: `SetWindowsHookEx` with `WH_MOUSE_LL`
- **WMI queries**: For mouse device information
- **Registry access**: For mouse settings and configuration

## File Structure

```
src/
├── Core/
│   ├── Application/
│   │   ├── Interfaces/
│   │   │   ├── IKeyboardBlockerService.cs
│   │   │   └── IMouseBlockerService.cs      ← New
│   │   └── Services/
│   │       ├── KeyboardBlockerService.cs
│   │       └── MouseBlockerService.cs       ← To be implemented
│   └── Domain/
│       ├── Entities/
│       │   ├── KeyboardBlockState.cs
│       │   └── MouseBlockState.cs           ← New
│       └── Interfaces/
│           ├── IKeyboardHookService.cs
│           ├── IKeyboardInfoService.cs
│           ├── IMouseHookService.cs         ← New
│           └── IMouseInfoService.cs         ← New
└── Infrastructure/
    └── Windows/
        ├── WindowsKeyboardHookService.cs
        ├── WindowsKeyboardInfoService.cs
        ├── WindowsMouseHookService.cs       ← To be implemented
        └── WindowsMouseInfoService.cs       ← To be implemented
```

## Next Steps for Implementation

### 1. Infrastructure Layer
- **WindowsMouseHookService**: Implement low-level mouse hook using Windows API
- **WindowsMouseInfoService**: Implement mouse device information retrieval using WMI

### 2. Application Layer
- **MouseBlockerService**: Implement main mouse blocking orchestration service
- **Combined Service**: Consider unified keyboard/mouse blocking service

### 3. Integration Points
- **System Tray**: Extend to support mouse state indicators
- **Main UI**: Update to show mouse blocking status
- **Configuration**: Add mouse-specific settings

### 4. Testing
- **Unit Tests**: Test all mouse interfaces and services
- **Integration Tests**: Test mouse-keyboard interaction
- **Platform Tests**: Test on different Windows versions

## Key Design Decisions

### 1. Separate vs Combined Services
- **Decision**: Separate mouse services mirror keyboard services
- **Rationale**: Maintains separation of concerns, easier testing, cleaner architecture

### 2. Event-Driven Architecture
- **Decision**: Use events for state changes and notifications
- **Rationale**: Decouples services, enables reactive UI updates, follows existing patterns

### 3. Async-First Design
- **Decision**: All service methods are asynchronous
- **Rationale**: Maintains UI responsiveness, follows modern .NET patterns, consistent with keyboard services

### 4. Mouse-Specific Information
- **Decision**: Provide mouse-specific information (DPI, buttons, scroll wheel)
- **Rationale**: Enables advanced features, provides useful debugging information, follows information service pattern

## Compatibility

### .NET Framework
- **Target**: .NET 6.0+ (same as existing codebase)
- **Compatibility**: Windows-specific implementation using P/Invoke

### Windows Versions
- **Minimum**: Windows 10 (same as keyboard implementation)
- **Recommended**: Windows 11 for best compatibility

### Hardware Support
- **Standard Mice**: Full support for basic mouse operations
- **Gaming Mice**: Enhanced support for multi-button mice
- **Touchpads**: Basic support (treated as standard mouse)

## Security Considerations

### 1. Low-Level Hooks
- **Privilege Requirements**: Same as keyboard hooks
- **User Permissions**: Administrative rights may be required

### 2. System Integration
- **Hook Management**: Proper installation/uninstallation
- **Memory Management**: Prevent memory leaks in hook procedures
- **Exception Handling**: Graceful handling of hook failures

This architecture provides a solid foundation for mouse blocking functionality while maintaining consistency with the existing keyboard implementation and following established design patterns.