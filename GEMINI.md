# EvilModToolkit - Developer Context

## Project Overview
**EvilModToolkit** is a C# port of the [Collective Modding Toolkit](https://github.com/wxMichael/Collective-Modding-Toolkit), originally written in Python.
**Goal:** Re-implement the tool using modern .NET 8, Avalonia UI, and the MVVM pattern (ReactiveUI), while maintaining full feature parity.

## Project Structure
- **`Code_to_Port/`**: Original Python source code (Reference only).
    - `src/`: Main Python logic.
    - `src/patcher/`: BA2 and binary patching logic.
- **`EvilModToolkit/`**: Main C# Application (Avalonia UI).
    - `Models/`: Domain objects (`GameInfo`, `ModManagerInfo`).
    - `ViewModels/`: ReactiveUI ViewModels (`MainWindowViewModel`, etc.).
    - `Views/`: Avalonia XAML views.
    - `Services/`: Business logic implementation (`GameDetectionService`, `PatcherService`).
- **`EvilModToolkit.Tests/`**: Unit tests (xUnit, FluentAssertions, NSubstitute).

## Development Environment
- **SDK:** .NET 8.0
- **Framework:** `net8.0-windows` (due to WPF/Registry/WMI dependencies, though Avalonia is cross-platform).
- **IDE:** Visual Studio 2022 or JetBrains Rider.
- **Platform:** Windows 10/11 (Target OS).

## Build & Run Commands
*   **Restore:** `dotnet restore`
*   **Build:** `dotnet build`
*   **Run App:** `dotnet run --project EvilModToolkit`
*   **Run Tests:** `dotnet test`
*   **Publish (Trimmed):** `dotnet publish -c Release -r win-x64 --self-contained /p:PublishTrimmed=true`

## Key Architectural Decisions
1.  **MVVM Pattern:** Strict separation of concerns using **ReactiveUI**.
    -   ViewModels inherit from `ReactiveObject`.
    -   Use `ReactiveCommand` for actions.
    -   **ViewLocator** uses a `switch` expression (not reflection) to remain trim-friendly.
2.  **Dependency Injection:** Uses `Microsoft.Extensions.DependencyInjection`.
3.  **Trimming/AOT:** The app is designed to be published as a standalone single-file executable. Avoid reflection-based APIs that break trimming.
4.  **Platform Specifics:**
    -   **WMI (`System.Management`)**: Used for parent process detection (MO2/Vortex) and system info.
    -   **PeNet**: Used for parsing F4SE DLL exports.
    -   **Registry**: Used for game installation detection.
    -   **xdelta3**: External binary (bundled) used for game version patching.

## Porting Requirements (Python -> C#)
| Feature | Python Implementation | C# Implementation |
| :--- | :--- | :--- |
| **Parent Process** | `psutil` walking process tree | `System.Management` (WMI) |
| **F4SE Scan** | `ctypes` / `pefile` | `PeNet` (NuGet) |
| **BA2 Patching** | Binary file I/O | `FileStream` / Binary manipulation |
| **Game Path** | `winreg` | `Microsoft.Win32.Registry` |
| **Patching** | `pyxdelta` | `Process.Start("xdelta3.exe")` |

## Coding Conventions
*   **Nullable Reference Types:** Enabled (`<Nullable>enable</Nullable>`).
*   **Async/Await:** Use throughout for I/O and long-running tasks.
*   **Platform Attributes:** Use `[SupportedOSPlatform("windows")]` for Windows-specific APIs.
*   **Logging:** Use `ILogger<T>` injected into services/ViewModels.

## Reference Files
- **`PORTING_REQUIREMENTS.md`**: Detailed technical analysis for specific features (WMI, PeNet, etc.).
- **`CLAUDE.md`**: Existing developer guidelines and architecture notes.
- **`ROADMAP.md`**: Current progress and future tasks.
