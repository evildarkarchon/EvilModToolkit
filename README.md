# Evil Modding Toolkit (C# Port)

A C# port and fork of the [Collective Modding Toolkit](https://github.com/wxMichael/Collective-Modding-Toolkit), rewritten using Avalonia UI and the MVVM pattern for modern cross-platform capabilities.

> **Note:** This is a work-in-progress port from the original Python/Tkinter version to C# with Avalonia.

## Overview

Evil Modding Toolkit is a comprehensive toolkit for troubleshooting and optimizing your mod setup for Fallout 4. Originally designed for the Collective Modding Discord Community, this C# port aims to bring improved performance, maintainability, and a modern UI framework.

## Planned Features

The goal is to port all features from the original toolkit:

- **Game Version Management**: Downgrade and upgrade Fallout 4 and Creation Kit between Old-Gen and Next-Gen with delta patches
- **Mod Statistics**: Provides counts of data files by type; plugins (Full/Light) and BA2 archives (General and Textures)
- **Archive Patching**: Patches all BA2 files to either v1 (OG) or v8 (NG) format
- **F4SE DLL Scanner**: Scans F4SE DLLs for game version support and compatibility
- **Mod Setup Scanner**: Analyzes your mod setup for potential issues and conflicts
- **Mod Manager Integration**: Detects and works with MO2 and Vortex

## Architecture

### Technology Stack

- **Language**: C# (.NET 8+)
- **UI Framework**: Avalonia UI 11.x
- **Architecture**: MVVM (Model-View-ViewModel)
- **MVVM Framework**: ReactiveUI
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Microsoft.Extensions.Logging
- **Testing**: xUnit, FluentAssertions, NSubstitute
- **Platform**: Windows (primary), with potential for Linux/macOS support

### Architecture Principles

This project follows strict MVVM architecture with ReactiveUI:

- **Models**: Plain C# classes representing domain entities
- **ViewModels**: ReactiveUI `ReactiveObject` classes managing UI state and commands
- **Views**: Avalonia XAML files with minimal code-behind
- **Services**: Dependency-injected services for business logic
- **Trim-Friendly**: Built for trimmed publishing (self-contained executables)
- **Compiled Bindings**: All XAML bindings use compiled mode for performance
- **Async/Await**: All long-running operations are asynchronous

See [CLAUDE.md](CLAUDE.md) for detailed development guidelines and architecture decisions.

### Project Structure

```
EvilModToolkit/
├── EvilModToolkit/              # Main application project
│   ├── Models/                  # Data models and business logic
│   ├── ViewModels/              # View models (MVVM pattern)
│   ├── Views/                   # Avalonia XAML views
│   ├── Services/                # Service layer (game detection, patching, etc.)
│   ├── Converters/              # Value converters for data binding
│   ├── Assets/                  # Images, icons, styles
│   └── Program.cs               # Application entry point
└── Code_to_Port/                # Original Python source for reference
```

## Development Status

🚧 **Currently in Phase 0 - Project Foundation** 🚧

This project is actively being ported from Python to C#. Reference the original source in the `Code_to_Port/` directory for implementation details.

**Current Progress:**
- ✅ Project structure and Avalonia setup
- ✅ Testing infrastructure (xUnit, FluentAssertions, NSubstitute)
- ✅ WMI functionality tested
- ✅ PeNet library tested for F4SE DLL parsing
- ⏳ Core services layer (next phase)

See [ROADMAP.md](ROADMAP.md) for detailed development plans and progress tracking.

## Building and Running

### Prerequisites

- .NET 8 SDK or later
- Visual Studio 2022 or JetBrains Rider (recommended for Avalonia development)
- Windows 10/11 (primary development platform)

### Build Instructions

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project EvilModToolkit

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Testing

The project uses xUnit for testing with the following tools:

- **xUnit**: Test framework
- **FluentAssertions**: Readable assertion syntax
- **NSubstitute**: Mocking framework
- **coverlet.collector**: Code coverage

Tests are organized in the `EvilModToolkit.Tests` project, mirroring the main project structure:

```
EvilModToolkit.Tests/
├── Services/        # Service layer tests
├── ViewModels/      # ViewModel tests
└── Models/          # Model tests
```

## Installation (Future)

Once stable:

1. Extract the release zip to any location
2. Add `EvilModToolkit.exe` to your mod manager's executables
3. Launch via your mod manager to access the VFS (Virtual File System)

## Contributing

This is a personal port project. If you'd like to contribute, please open an issue first to discuss the changes.

## License

GPL-2.0-or-later (matching the original Collective Modding Toolkit license)

## Credits

- **Original Project**: [Collective Modding Toolkit](https://github.com/wxMichael/Collective-Modding-Toolkit) by wxMichael
- **C# Port**: Evil Modding Toolkit (this project)

## Related Links

- Original Python Version: https://github.com/wxMichael/Collective-Modding-Toolkit
- Collective Modding Discord: https://discord.gg/tktyEyYHZH
- Nexus Mods Page: https://www.nexusmods.com/fallout4/mods/87907
