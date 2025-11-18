# CLAUDE.md

This file provides guidance to Claude Code when working with the Evil Modding Toolkit C# port.

## Project Overview

This is a C# port of the [Collective Modding Toolkit](https://github.com/wxMichael/Collective-Modding-Toolkit), rewritten from Python/Tkinter to C#/Avalonia using the MVVM architectural pattern with ReactiveUI. The original source code is temporarily preserved in `Code_to_Port/` for reference during development and will be removed once feature parity is achieved.

## Development Setup and Commands

### Build and Run

```bash
# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run the application (do not use --no-build)
dotnet run --project EvilModToolkit

# Run tests (do not use --no-build)
dotnet test

# Clean build artifacts
dotnet clean
```

### Code Quality

```bash
# Format code
dotnet format

# Analyze code
dotnet build /p:TreatWarningsAsErrors=true

# Run security analysis (if configured)
dotnet list package --vulnerable
```

## Architecture Overview

### Technology Stack

- **Framework**: .NET 8+ (target framework: net8.0-windows)
- **UI Framework**: Avalonia UI 11.x
- **Architecture**: MVVM (Model-View-ViewModel)
- **MVVM Framework**: ReactiveUI
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Logging**: Microsoft.Extensions.Logging

### Project Structure

```
EvilModToolkit/
├── Models/                      # Domain models and business entities
│   ├── GameInfo.cs             # Game version detection and file analysis
│   ├── ModManagerInfo.cs       # Mod manager detection (MO2, Vortex)
│   ├── F4SEPlugin.cs           # F4SE DLL information
│   └── ScanResult.cs           # Scanner results
├── ViewModels/                  # MVVM ViewModels
│   ├── MainWindowViewModel.cs  # Main window VM
│   ├── OverviewViewModel.cs    # Overview tab
│   ├── F4SEViewModel.cs        # F4SE scanner tab
│   ├── ScannerViewModel.cs     # Mod scanner tab
│   ├── ToolsViewModel.cs       # Tools/patcher tab
│   └── SettingsViewModel.cs    # Settings tab
├── Views/                       # Avalonia XAML views
│   ├── MainWindow.axaml        # Main application window
│   ├── OverviewView.axaml      # Overview tab view
│   └── ...                     # Other views
├── Services/                    # Business logic services
│   ├── IGameDetectionService.cs
│   ├── IModManagerService.cs
│   ├── IPatcherService.cs
│   ├── IScannerService.cs
│   └── ISettingsService.cs
├── Converters/                  # Value converters for XAML bindings
├── Assets/                      # Icons, images, styles
└── App.axaml                    # Application resources and styles
```

### MVVM Pattern Guidelines with ReactiveUI

1. **ViewModels** must inherit from `ReactiveObject`
2. Use `RaiseAndSetIfChanged` for property setters with change notifications
3. Use `ReactiveCommand` for user interactions and async operations
4. ViewModels should never reference Views directly
5. Use dependency injection for services
6. Leverage ReactiveUI's `WhenAnyValue` for reactive property bindings
7. Avoid code-behind in views - keep them thin

### ReactiveUI Patterns

```csharp
// ViewModel example
public class MyViewModel : ReactiveObject
{
    private string _gameVersion;
    public string GameVersion
    {
        get => _gameVersion;
        set => this.RaiseAndSetIfChanged(ref _gameVersion, value);
    }

    public ReactiveCommand<Unit, Unit> ScanCommand { get; }

    public MyViewModel(IScannerService scannerService)
    {
        ScanCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await scannerService.ScanModsAsync();
        });
    }
}
```

### Key Implementation Notes

**Original Python Architecture (for reference)**:
- Main entry: `src/main.py` → Tkinter app
- Tab system: `src/tabs/` with base class `CMCTabFrame`
- Controller: `src/cm_checker.py` (CMChecker class)
- Settings: JSON-based with `src/app_settings.py`

**C# Port Approach**:
- Replace Tkinter tabs with Avalonia UserControls and ViewModels
- Convert Python protocols to C# interfaces
- Replace threading with async/await and Task-based operations
- Use Avalonia's data binding with ReactiveUI instead of manual UI updates
- Implement proper MVVM separation of concerns with ReactiveUI patterns

### Service Layer

All business logic should be abstracted into services:

- **GameDetectionService**: Fallout 4 installation detection, version checking
- **ModManagerService**: MO2/Vortex detection and integration
- **PatcherService**: BA2 archive patching (v1/v8 conversion)
- **ScannerService**: Mod setup analysis and issue detection
- **SettingsService**: Application settings persistence
- **LoggingService**: Application logging (structured logging)

### Data Binding

Use Avalonia's data binding extensively with ReactiveUI support:

```xml
<!-- Example binding in XAML -->
<TextBlock Text="{Binding GameVersion}" />
<Button Command="{Binding ScanCommand}" Content="Scan" />
```

### Async Operations

All long-running operations must be async with ReactiveCommand:

```csharp
// Good - using ReactiveCommand
public ReactiveCommand<Unit, Unit> ScanCommand { get; }

public MyViewModel(IScannerService scannerService)
{
    ScanCommand = ReactiveCommand.CreateFromTask(async () =>
    {
        await scannerService.ScanModsAsync();
    });
}
```

## Porting Guidelines

### Python to C# Translation Notes

1. **File Operations**:
   - Python `pathlib.Path` → C# `System.IO.Path` and `FileInfo`/`DirectoryInfo`
   - Use async I/O where possible: `File.ReadAllTextAsync`, `File.WriteAllTextAsync`

2. **Registry Access**:
   - Python `winreg` → C# `Microsoft.Win32.Registry`

3. **JSON Settings**:
   - Python `json` → C# `System.Text.Json.JsonSerializer`

4. **Threading**:
   - Python `threading.Thread` → C# `async/await` with `Task`
   - Use `ReactiveCommand.CreateFromTask` for async operations in ViewModels
   - Avoid blocking the UI thread

5. **Archive Patching**:
   - Python `pyxdelta` → Need to find C# equivalent or P/Invoke
   - Consider using existing C# libraries for BA2 manipulation

6. **F4SE DLL Scanning**:
   - Preserve the binary scanning logic
   - Use `BinaryReader` for reading PE headers

### Code Style

- Follow standard C# conventions (PascalCase for public members, camelCase for private)
- Nullable reference types are enabled at the project level
- Prefer `var` for local variables when type is obvious
- Use expression-bodied members when appropriate
- Add XML documentation comments to public APIs
- Use ReactiveUI patterns consistently throughout the codebase

### Testing

- Write unit tests for services and ViewModels
- Use xUnit or NUnit as test framework
- Mock services using Moq or NSubstitute
- Test ReactiveCommand execution and property change notifications
- Aim for high coverage on business logic

## Dependencies to Consider

Suggested NuGet packages for porting:

- **Avalonia** (11.x) - UI framework
- **Avalonia.ReactiveUI** - MVVM support with ReactiveUI
- **ReactiveUI** - Core ReactiveUI functionality
- **Microsoft.Extensions.DependencyInjection** - DI container
- **Microsoft.Extensions.Logging** - Logging
- **System.Text.Json** - JSON serialization

For Fallout 4 specific operations, may need:
- BA2 archive manipulation library (TBD)
- Delta patching library (xdelta port or equivalent)

## Platform-Specific Considerations

- **Windows-only APIs**: Registry access, certain file operations
- **Avalonia Cross-platform**: While Avalonia supports Linux/macOS, Fallout 4 is Windows-only, so focus on Windows first
- **Mod Manager Integration**: MO2 and Vortex are Windows applications

## Reference Materials

- Original Python source: `Code_to_Port/` (temporary, will be removed after port completion)
- Avalonia Documentation: https://docs.avaloniaui.net/
- ReactiveUI Documentation: https://www.reactiveui.net/docs/
- MVVM Pattern: https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm

## Development Workflow

1. Reference the original Python implementation in `Code_to_Port/` (while available)
2. Design the C# equivalent using MVVM principles with ReactiveUI
3. Implement services first (business logic), then ViewModels, then Views
4. Write tests for services and ViewModels
5. Use Avalonia's hot reload for UI development
6. Test with actual Fallout 4 installation and mod managers
7. Once feature parity is achieved, remove the `Code_to_Port/` directory

## Notes

- The `Code_to_Port/` directory is temporary and should not be modified
- The port should maintain feature parity with the original toolkit
- Prioritize correctness over speed during initial development
- Consider adding features that benefit from C#'s type safety and tooling
- Use ReactiveUI patterns consistently for a reactive, maintainable codebase
