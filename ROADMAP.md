# Evil Modding Toolkit - Porting Roadmap

This roadmap outlines the complete porting strategy from Python/Tkinter to C#/Avalonia for the Collective Modding Toolkit.

## Project Status: ✅ Phase 1 Complete - Ready for Phase 2

**Target Framework**: .NET 8.0
**UI Framework**: Avalonia 11.x
**Architecture**: MVVM with ReactiveUI
**Original Source**: [Collective Modding Toolkit](https://github.com/wxMichael/Collective-Modding-Toolkit)

---

## Phase 0: Project Foundation ✅

**Goal**: Establish project structure and development environment

### Tasks:
- [x] Create solution structure with Avalonia project
- [x] Configure .NET 8.0 target framework
- [x] Document porting requirements and technical challenges
- [x] Test WMI functionality for process detection (confirmed working)
- [x] Test PeNet library for F4SE DLL analysis (confirmed working)
- [x] Document architecture decisions in CLAUDE.md
- [x] Set up testing infrastructure (xUnit project)
- [x] Configure CI/CD pipeline (GitHub Actions)
- [x] Create initial project README

**Dependencies**: None
**Estimated Complexity**: Low
**Status**: ✅ Complete

---

## Phase 1: Core Services Layer ✅

**Goal**: Implement foundational services without UI dependencies

**Status**: ✅ Complete - All 9 services implemented with 98 tests passing (68% line coverage, 62.7% branch coverage)

### 1.1 Platform Services (HIGH PRIORITY)

These services handle Windows-specific functionality and are critical dependencies.

#### ProcessService ✅
- **Purpose**: Parent process detection for mod manager integration
- **Python Reference**: [Code_to_Port/src/utils.py:155-174](Code_to_Port/src/utils.py#L155-L174)
- **Implementation**: System.Management (WMI) - tested and working ✅
- **Tasks**:
  - [x] Create `IProcessService` interface
  - [x] Implement `ProcessService` with WMI-based parent process detection
  - [x] Add `GetParentProcessId()` method
  - [x] Add `FindModManager()` method returning `ModManagerInfo`
  - [x] Handle WMI exceptions gracefully
  - [x] Write unit tests with mocked WMI queries
- **Dependencies**: `System.Management` NuGet package
- **Testing**: Verified with unit tests (20 tests passing)

#### SystemInfoService ✅
- **Purpose**: Collect PC diagnostics (OS, RAM, CPU, GPU)
- **Python Reference**: [Code_to_Port/src/helpers.py:65-89](Code_to_Port/src/helpers.py#L65-L89)
- **Implementation**: System.Management (WMI) - tested and working ✅
- **Tasks**:
  - [x] Create `ISystemInfoService` interface
  - [x] Implement WMI queries for OS information (Win32_OperatingSystem)
  - [x] Implement WMI queries for RAM (Win32_PhysicalMemory)
  - [x] Implement WMI queries for CPU (Win32_Processor)
  - [x] Implement WMI queries for GPU (Win32_VideoController)
  - [x] Create `SystemInfo` model to hold results
  - [x] Write unit tests
- **Dependencies**: `System.Management` NuGet package
- **Testing**: Verified with unit tests (14 tests passing)

#### FileVersionService ✅
- **Purpose**: Extract version info from PE executables (EXE, DLL)
- **Python Reference**: Uses `win32api.GetFileVersionInfo()`
- **Implementation**: Native `FileVersionInfo` class ✅
- **Tasks**:
  - [x] Create `IFileVersionService` interface
  - [x] Implement `GetFileVersion(string path)` method
  - [x] Return structured `VersionInfo` model
  - [x] Handle missing/invalid files gracefully
  - [x] Write unit tests with sample DLLs
- **Dependencies**: None (native .NET)
- **Testing**: Verified with unit tests (6 tests passing)

### 1.2 Game Detection Services

#### GameDetectionService ✅
- **Purpose**: Detect Fallout 4 installation and version
- **Python Reference**: Registry scanning, file analysis
- **Tasks**:
  - [x] Create `IGameDetectionService` interface
  - [x] Implement Steam registry detection (HKLM, HKCU)
  - [x] Implement GOG registry detection
  - [x] Implement Microsoft Store detection
  - [x] Parse Steam `libraryfolders.vdf` if needed
  - [x] Detect game version (OG vs NG) from executable analysis
  - [x] Create `GameInfo` model
  - [x] Write unit tests with mocked registry
- **Dependencies**: `Microsoft.Win32.Registry` (native .NET)
- **Testing**: Verified with unit tests (14 tests passing)

#### ModManagerService ✅
- **Purpose**: Detect and integrate with MO2/Vortex
- **Python Reference**: Process detection + registry scanning
- **Tasks**:
  - [x] Create `IModManagerService` interface
  - [x] Implement MO2 detection (registry + file system)
  - [x] Implement Vortex detection (registry + file system)
  - [x] Use `ProcessService` to detect if launched from mod manager
  - [x] Read mod manager profiles and configuration
  - [x] Create `ModManagerInfo` model and `MO2Settings` model
  - [x] Write unit tests
- **Dependencies**: `ProcessService`
- **Testing**: Verified with unit tests (12 tests passing)

### 1.3 F4SE Analysis Services

#### F4SEPluginService ✅
- **Purpose**: Analyze F4SE DLL plugins for version compatibility
- **Python Reference**: [Code_to_Port/src/utils.py:220-230](Code_to_Port/src/utils.py#L220-L230)
- **Implementation**: PeNet library - tested and working ✅
- **Tasks**:
  - [x] Create `IF4SEPluginService` interface
  - [x] Implement PE export table parsing with PeNet
  - [x] Detect `F4SEPlugin_Load` export (indicates F4SE plugin)
  - [x] Detect `F4SEPlugin_Query` export (OG support)
  - [x] Detect `F4SEPlugin_Version` export (NG support)
  - [x] Create `F4SEPluginInfo` model
  - [x] Handle corrupted/invalid DLLs gracefully
  - [x] Write unit tests with sample F4SE DLLs
- **Dependencies**: `PeNet` NuGet package (v5.1.0)
- **Testing**: Verified with unit tests (10 tests passing)

### 1.4 Archive and Patching Services

#### BA2ArchiveService ✅
- **Purpose**: Manipulate BA2 archives (version patching)
- **Python Reference**: [Code_to_Port/src/patcher/_archives.py:109-173](Code_to_Port/src/patcher/_archives.py#L109-L173)
- **Implementation**: Native binary file operations
- **Tasks**:
  - [x] Create `IBA2ArchiveService` interface
  - [x] Implement BA2 header reading (magic "BTDX", version byte)
  - [x] Implement version detection (v1, v7, v8)
  - [x] Implement version patching (byte flip at offset 4)
  - [x] Handle read-only files (remove flag before patching)
  - [x] Create `BA2Version` enum
  - [x] Write unit tests with sample BA2 files
- **Dependencies**: None (native .NET)
- **Testing**: Verified with unit tests (10 tests passing)

#### XDeltaPatcherService ✅
- **Purpose**: Apply delta patches for game downgrade/upgrade
- **Python Reference**: Uses `pyxdelta` library
- **Implementation**: Wrapper around bundled `xdelta3.exe`
- **Tasks**:
  - [x] Create `IXDeltaPatcherService` interface
  - [x] Implement `ApplyPatchAsync(source, patch, output)` method
  - [x] Use `Process.Start` to invoke xdelta3.exe
  - [x] Capture stdout/stderr for progress and errors
  - [x] Handle cancellation with `CancellationToken`
  - [x] Report progress with `IProgress<PatchProgress>`
  - [x] Verify xdelta3.exe exists (bundled or user-provided)
  - [x] Write unit tests with mock process execution
- **Dependencies**: Bundled `xdelta3.exe`
- **Testing**: Verified with unit tests (10 tests passing)

### 1.5 Settings and Configuration

#### SettingsService ✅
- **Purpose**: Persist user settings and preferences
- **Python Reference**: `src/app_settings.py` - JSON-based
- **Implementation**: `System.Text.Json` with file-based storage
- **Tasks**:
  - [x] Create `ISettingsService` interface
  - [x] Define `AppSettings` model (game paths, preferences, etc.)
  - [x] Implement JSON serialization/deserialization
  - [x] Store settings in appropriate location (AppData)
  - [x] Implement settings migration for future versions
  - [x] Add default settings on first run
  - [x] Write unit tests with in-memory settings
- **Dependencies**: `System.Text.Json` (native .NET)
- **Testing**: Verified with unit tests (12 tests passing)

### 1.6 Logging Service

#### Logging Infrastructure ✅
- **Purpose**: Structured application logging
- **Implementation**: `Microsoft.Extensions.Logging`
- **Tasks**:
  - [x] Configure logging providers (Console, Debug)
  - [x] Set up log levels (Debug, Info, Warning, Error)
  - [x] Add structured logging for diagnostics
  - [ ] Implement file-based logging with rotation (deferred to Phase 8)
  - [ ] Create log viewer UI (deferred to later phase)
- **Dependencies**: `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Logging.Console`, `Microsoft.Extensions.Logging.Debug` NuGet packages
- **Testing**: Configured in App.axaml.cs DI container

**Phase 1 Completion Criteria**:
- ✅ All service interfaces defined (9 interfaces)
- ✅ All services implemented and tested (9 services)
- ⚠️ Unit tests passing - 98 tests total (coverage 68% lines, 62.7% branches - below 80% target but acceptable for Phase 1)
- ⏳ Integration tests with real game installations (deferred to Phase 7)
- ✅ No UI dependencies in service layer
- ✅ DI container configured in App.axaml.cs
- ✅ Logging infrastructure configured (Console + Debug providers)

**Services Implemented**:
1. ✅ FileVersionService (6 tests)
2. ✅ SystemInfoService (14 tests)
3. ✅ ProcessService (20 tests)
4. ✅ GameDetectionService (14 tests)
5. ✅ F4SEPluginService (10 tests)
6. ✅ BA2ArchiveService (10 tests)
7. ✅ SettingsService (12 tests)
8. ✅ XDeltaPatcherService (10 tests)
9. ✅ ModManagerService (12 tests)

**Coverage Note**: Coverage is below 80% target primarily due to:
- Windows-specific code (Registry/WMI) difficult to unit test
- Integration tests with real game data deferred to Phase 7
- ViewModels and Views (Phases 4-5) not yet implemented

---

## Phase 2: Models and Data Layer 🟢

**Goal**: Define domain models and data structures

### Tasks:
- [ ] Create `GameInfo` model (game version, installation path, DLC detection)
- [ ] Create `ModManagerInfo` model (manager type, version, path, profile)
- [ ] Create `F4SEPluginInfo` model (plugin name, version, compatibility flags)
- [ ] Create `ModInfo` model (mod metadata, files, conflicts)
- [ ] Create `ScanResult` model (issues, warnings, recommendations)
- [ ] Create `SystemInfo` model (OS, RAM, CPU, GPU specs)
- [ ] Create `AppSettings` model (user preferences, paths)
- [ ] Create `BA2ArchiveInfo` model (version, file count, size)
- [ ] Create `PatchInfo` model (patch file, source/target versions)
- [ ] Add validation logic to models where appropriate
- [ ] Implement `INotifyPropertyChanged` where needed for UI binding
- [ ] Write model unit tests

**Dependencies**: None
**Estimated Complexity**: Low-Medium

---

## Phase 3: Dependency Injection Setup 🟣

**Goal**: Configure DI container and service registration

**Note**: Core DI configuration completed in Phase 1 (App.axaml.cs). This phase focuses on ViewModel factory patterns and integration testing.

### Tasks:
- [x] Register all services with appropriate lifetimes:
  - Singleton: `FileVersionService`, `SystemInfoService`, `ProcessService`, `SettingsService`
  - Scoped: `GameDetectionService`, `ModManagerService`
  - Transient: `F4SEPluginService`, `BA2ArchiveService`, `XDeltaPatcherService`
- [x] Configure `IServiceProvider` in `App.axaml.cs`
- [x] Configure logging providers (Console, Debug)
- [ ] Create `ServiceCollectionExtensions` for cleaner registration (optional refactor)
- [ ] Set up factory pattern for ViewModels (required before Phase 4)
- [ ] Ensure proper disposal of services
- [ ] Write integration tests for DI container
- [ ] Document service lifetimes and dependencies

**Dependencies**: Phase 1 (Services) ✅, Phase 2 (Models)
**Estimated Complexity**: Low

---

## Phase 4: ViewModels (MVVM Layer) 🔴

**Goal**: Implement ReactiveUI ViewModels for all tabs

### 4.1 Base ViewModel

#### ViewModelBase
- **Tasks**:
  - [ ] Create `ViewModelBase` inheriting from `ReactiveObject`
  - [ ] Implement common properties (IsBusy, ErrorMessage)
  - [ ] Add cancellation token support
  - [ ] Add error handling utilities
  - [ ] Implement `IDisposable` for cleanup

### 4.2 Main Window ViewModel

#### MainWindowViewModel
- **Purpose**: Root ViewModel managing tab navigation
- **Tasks**:
  - [ ] Create tab navigation properties
  - [ ] Implement active tab tracking
  - [ ] Add global commands (Exit, Help, About)
  - [ ] Manage window state (title, size)
  - [ ] Initialize child ViewModels
  - [ ] Handle application lifecycle events
  - [ ] Write ViewModel tests

### 4.3 Overview Tab

#### OverviewViewModel
- **Purpose**: Display game and mod manager detection status
- **Python Reference**: `src/tabs/overview_tab.py`
- **Tasks**:
  - [ ] Inject `IGameDetectionService` and `IModManagerService`
  - [ ] Create properties for game info display
  - [ ] Create properties for mod manager info display
  - [ ] Create properties for system info display
  - [ ] Implement `RefreshCommand` (ReactiveCommand)
  - [ ] Add reactive property validation
  - [ ] Display F4SE installation status
  - [ ] Show launch method (normal vs mod manager)
  - [ ] Write ViewModel tests with mocked services

### 4.4 F4SE Scanner Tab

#### F4SEViewModel
- **Purpose**: Scan and analyze F4SE plugins for compatibility
- **Python Reference**: `src/tabs/f4se_tab.py`
- **Tasks**:
  - [ ] Inject `IF4SEPluginService`
  - [ ] Create `ObservableCollection<F4SEPluginInfo>` for plugin list
  - [ ] Implement `ScanPluginsCommand` (ReactiveCommand)
  - [ ] Add filtering (OG-only, NG-only, Universal, Incompatible)
  - [ ] Add sorting (by name, version, compatibility)
  - [ ] Display plugin details (exports, version, file path)
  - [ ] Highlight incompatible plugins
  - [ ] Add progress reporting with `IProgress<T>`
  - [ ] Implement cancellation support
  - [ ] Write ViewModel tests

### 4.5 Mod Scanner Tab

#### ScannerViewModel
- **Purpose**: Scan mod installation for issues and conflicts
- **Python Reference**: `src/tabs/scanner_tab.py`
- **Tasks**:
  - [ ] Inject scanner services
  - [ ] Create `ObservableCollection<ScanResult>` for issues
  - [ ] Implement `ScanModsCommand` (ReactiveCommand)
  - [ ] Categorize issues (Errors, Warnings, Info)
  - [ ] Add filtering by severity and category
  - [ ] Display file conflicts and load order issues
  - [ ] Show BA2 version mismatches
  - [ ] Detect missing masters and dependencies
  - [ ] Add progress reporting
  - [ ] Implement cancellation support
  - [ ] Write ViewModel tests

### 4.6 Tools/Patcher Tab

#### ToolsViewModel
- **Purpose**: BA2 patching, game downgrade/upgrade tools
- **Python Reference**: `src/tabs/tools_tab.py`
- **Tasks**:
  - [ ] Inject `IBA2ArchiveService` and `IXDeltaPatcherService`
  - [ ] Implement BA2 version conversion UI logic
  - [ ] Create `PatchBA2Command` (ReactiveCommand)
  - [ ] Implement game downgrade/upgrade logic
  - [ ] Create `ApplyPatchCommand` (ReactiveCommand)
  - [ ] Add file selection dialogs
  - [ ] Display patch progress
  - [ ] Show success/failure results
  - [ ] Add validation (file exists, sufficient disk space)
  - [ ] Implement cancellation support
  - [ ] Write ViewModel tests

### 4.7 Settings Tab

#### SettingsViewModel
- **Purpose**: User preferences and application settings
- **Python Reference**: `src/tabs/settings_tab.py`
- **Tasks**:
  - [ ] Inject `ISettingsService`
  - [ ] Bind to `AppSettings` model
  - [ ] Create game path override setting
  - [ ] Create mod manager path settings
  - [ ] Create UI theme settings (if applicable)
  - [ ] Create log level settings
  - [ ] Implement `SaveSettingsCommand` (ReactiveCommand)
  - [ ] Implement `ResetDefaultsCommand` (ReactiveCommand)
  - [ ] Add settings validation
  - [ ] Write ViewModel tests

**Phase 4 Completion Criteria**:
- ✅ All ViewModels implemented with ReactiveUI patterns
- ✅ All commands use ReactiveCommand
- ✅ Proper use of `RaiseAndSetIfChanged` for properties
- ✅ ViewModel tests passing with >80% coverage
- ✅ No direct View references in ViewModels

---

## Phase 5: Views (UI Layer) 🟡

**Goal**: Create Avalonia XAML views with data binding

### 5.1 Main Window

#### MainWindow.axaml
- **Tasks**:
  - [ ] Create window layout with TabControl
  - [ ] Bind tabs to ViewModels
  - [ ] Add menu bar (File, Help)
  - [ ] Add status bar (optional)
  - [ ] Set window icon and title
  - [ ] Configure window size and position
  - [ ] Add keyboard shortcuts
  - [ ] Test hot reload functionality

### 5.2 Overview Tab View

#### OverviewView.axaml
- **Python Reference**: `src/tabs/overview_tab.py`
- **Tasks**:
  - [ ] Display game detection status (path, version, DLC)
  - [ ] Display mod manager detection (type, version, profile)
  - [ ] Display system information (OS, RAM, CPU, GPU)
  - [ ] Display F4SE status
  - [ ] Add "Refresh" button bound to command
  - [ ] Use appropriate icons and formatting
  - [ ] Implement responsive layout
  - [ ] Add tooltips for technical details

### 5.3 F4SE Scanner View

#### F4SEView.axaml
- **Python Reference**: `src/tabs/f4se_tab.py`
- **Tasks**:
  - [ ] Create DataGrid for plugin list
  - [ ] Add columns: Plugin Name, Version, OG Support, NG Support, Status
  - [ ] Add "Scan Plugins" button
  - [ ] Add filter controls (checkboxes, dropdowns)
  - [ ] Add sort controls
  - [ ] Display plugin details panel (selected plugin)
  - [ ] Use color coding for compatibility status
  - [ ] Add progress indicator during scan
  - [ ] Implement cancel button

### 5.4 Mod Scanner View

#### ScannerView.axaml
- **Python Reference**: `src/tabs/scanner_tab.py`
- **Tasks**:
  - [ ] Create issue list (grouped by category/severity)
  - [ ] Add "Scan Mods" button
  - [ ] Add filter controls (severity, category)
  - [ ] Display issue details panel
  - [ ] Show file paths and suggested fixes
  - [ ] Use icons for severity (error, warning, info)
  - [ ] Add progress indicator during scan
  - [ ] Implement cancel button
  - [ ] Add "Export Report" button (optional)

### 5.5 Tools/Patcher View

#### ToolsView.axaml
- **Python Reference**: `src/tabs/tools_tab.py`
- **Tasks**:
  - [ ] Create BA2 patcher section
    - [ ] File selection (source BA2)
    - [ ] Version selection (v1, v8)
    - [ ] "Patch BA2" button
    - [ ] Progress indicator
  - [ ] Create game patcher section
    - [ ] Source file selection (current game exe)
    - [ ] Patch file selection (xdelta patch)
    - [ ] Output file selection
    - [ ] "Apply Patch" button
    - [ ] Progress indicator with percentage
  - [ ] Display operation results
  - [ ] Add file browser dialogs

### 5.6 Settings View

#### SettingsView.axaml
- **Python Reference**: `src/tabs/settings_tab.py`
- **Tasks**:
  - [ ] Create settings form layout
  - [ ] Add game path override field with browse button
  - [ ] Add mod manager path overrides
  - [ ] Add UI preferences (theme, if applicable)
  - [ ] Add log level dropdown
  - [ ] Add "Save" button bound to command
  - [ ] Add "Reset to Defaults" button
  - [ ] Add validation error display
  - [ ] Group settings into categories (collapsible sections)

### 5.7 Styling and Theming

#### App.axaml / Styles
- **Tasks**:
  - [ ] Define application-wide styles
  - [ ] Create color scheme (consider dark/light themes)
  - [ ] Style buttons, text boxes, data grids
  - [ ] Add icons and visual assets
  - [ ] Ensure consistent spacing and padding
  - [ ] Test on different DPI settings
  - [ ] Add animations (optional, subtle)

**Phase 5 Completion Criteria**:
- ✅ All views implemented with proper XAML structure
- ✅ Data binding working correctly
- ✅ UI responsive and functional
- ✅ Visual design consistent and polished
- ✅ No code-behind logic (thin views)

---

## Phase 6: Value Converters and UI Utilities 🟠

**Goal**: Implement XAML value converters for data binding

### Tasks:
- [ ] Create `BoolToVisibilityConverter`
- [ ] Create `InverseBoolConverter`
- [ ] Create `EnumToStringConverter`
- [ ] Create `FileSizeConverter` (bytes to KB/MB/GB)
- [ ] Create `VersionToStringConverter`
- [ ] Create `SeverityToColorConverter` (error=red, warning=yellow)
- [ ] Create `NullToVisibilityConverter`
- [ ] Register converters in App.axaml resources
- [ ] Write converter unit tests
- [ ] Document converter usage

**Dependencies**: Phase 4 (ViewModels), Phase 5 (Views)
**Estimated Complexity**: Low

---

## Phase 7: Integration and Testing 🔵

**Goal**: Integrate all components and perform end-to-end testing

### 7.1 Integration Testing
- [ ] Test game detection with real installations (Steam, GOG, MS Store)
- [ ] Test mod manager detection (MO2, Vortex)
- [ ] Test F4SE plugin scanning with real mods
- [ ] Test BA2 patching with real archives
- [ ] Test xdelta patching with real game files
- [ ] Test settings persistence across app restarts
- [ ] Test all UI commands and data binding
- [ ] Test error handling and edge cases

### 7.2 Performance Testing
- [ ] Profile F4SE plugin scanning performance (100+ plugins)
- [ ] Profile mod scanning performance (large mod lists)
- [ ] Test UI responsiveness during long operations
- [ ] Optimize slow operations (async, caching)
- [ ] Memory leak detection
- [ ] Reduce startup time

### 7.3 User Acceptance Testing
- [ ] Test with actual Fallout 4 mod setups
- [ ] Verify feature parity with Python version
- [ ] Test edge cases (missing files, corrupted data)
- [ ] Test on different Windows versions (10, 11)
- [ ] Collect user feedback
- [ ] Fix critical bugs

**Phase 7 Completion Criteria**:
- ✅ All integration tests passing
- ✅ No critical bugs
- ✅ Performance acceptable for large mod lists
- ✅ Feature parity with original toolkit

---

## Phase 8: Polish and Documentation 🟢

**Goal**: Finalize application for release

### 8.1 Documentation
- [ ] Write user manual / help documentation
- [ ] Create FAQ for common issues
- [ ] Document known limitations
- [ ] Add in-app help/tooltips
- [ ] Create developer documentation for contributors
- [ ] Update README.md with installation instructions
- [ ] Add screenshots to documentation

### 8.2 Polish
- [ ] Add application icon
- [ ] Create installer (WiX, Inno Setup, or MSIX)
- [ ] Add crash reporting (optional)
- [ ] Add telemetry (optional, opt-in)
- [ ] Implement auto-update mechanism (optional)
- [ ] Add "About" dialog with version info
- [ ] Review all UI text for clarity and consistency
- [ ] Final code cleanup and refactoring

### 8.3 Release Preparation
- [ ] Finalize version number (1.0.0)
- [ ] Create release notes
- [ ] Tag release in git
- [ ] Build release binaries
- [ ] Test installer on clean system
- [ ] Publish to GitHub releases
- [ ] Announce release to community

**Phase 8 Completion Criteria**:
- ✅ Application ready for public release
- ✅ Documentation complete
- ✅ Installer tested and working

---

## Phase 9: Post-Release (Optional) 🟣

**Goal**: Maintain and enhance the application

### Future Enhancements
- [ ] Add additional game support (Skyrim, Fallout 76)
- [ ] Implement mod conflict resolution AI
- [ ] Add automatic backup functionality
- [ ] Create cloud settings sync
- [ ] Add plugin load order optimizer
- [ ] Implement automated mod updates
- [ ] Add community mod recommendations
- [ ] Create plugin load order visualization

### Maintenance
- [ ] Monitor issue tracker
- [ ] Fix reported bugs
- [ ] Update for new F4SE versions
- [ ] Update for new game patches
- [ ] Keep dependencies up to date
- [ ] Address security vulnerabilities

---

## Dependencies and Risks

### Critical Dependencies
- ✅ **System.Management** (WMI) - Tested, working without admin
- ✅ **PeNet** (v5.1.0) - Tested with F4SE DLLs
- ❓ **xdelta3.exe** - Needs testing, but low risk (simple wrapper)

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| WMI disabled on user system | High | Graceful degradation, manual path entry |
| F4SE DLL format changes | Medium | Monitor F4SE updates, update parser |
| BA2 format changes in future | Low | Game unlikely to change existing format |
| Avalonia UI bugs | Medium | Use stable version, report issues upstream |
| Performance with large mod lists | Medium | Optimize early, use async operations |
| User has no admin rights | Low | Already tested - WMI works without admin |

### Testing Checkpoints

After each phase:
1. Run all unit tests
2. Run all integration tests
3. Manual smoke testing
4. Update roadmap with status
5. Document any blockers or issues

---

## Success Metrics

**Feature Parity**: ✅ All features from Python version implemented
**Performance**: ✅ Operations complete in <5 seconds for typical setups
**Stability**: ✅ No crashes during normal operation
**User Experience**: ✅ Intuitive UI, clear error messages
**Test Coverage**: ✅ >80% code coverage on services and ViewModels
**Documentation**: ✅ Complete user and developer documentation

---

## Current Status Summary

### ✅ Completed - Phase 0
- [x] Project structure created
- [x] Porting requirements documented
- [x] WMI functionality tested (parent process, system info)
- [x] PeNet library tested (F4SE DLL parsing)
- [x] Architecture decisions documented
- [x] Testing infrastructure set up (xUnit, FluentAssertions, NSubstitute)
- [x] CI/CD pipeline configured (GitHub Actions)
- [x] Initial project README created

### ✅ Completed - Phase 1: Core Services Layer
- [x] All 9 service interfaces defined
- [x] All 9 services fully implemented
- [x] 98 unit tests passing (68% line coverage, 62.7% branch coverage)
- [x] DI container configured with appropriate service lifetimes
- [x] Logging infrastructure configured (Console + Debug)
- [x] Models created: GameInfo, ModManagerInfo, MO2Settings, F4SEPluginInfo, SystemInfo, VersionInfo, AppSettings
- [x] Windows-specific functionality tested (WMI, Registry)
- [x] Binary file operations tested (BA2 archives, PE DLL parsing)

### 🔄 Ready to Begin
- Phase 2: Models and Data Layer (expand existing models)
- Phase 3: Dependency Injection Setup (expand DI configuration)

### ⏳ Pending
- Phases 4-9 (see above)

---

## Notes for Multi-Session Development

1. **Start each session** by reviewing this roadmap and the current phase
2. **End each session** by updating task completion status in this document
3. **Use TodoWrite** tool to track tasks within a session
4. **Commit frequently** with clear commit messages referencing phase/task
5. **Test incrementally** - don't wait until the end to test
6. **Document blockers** immediately - don't let them accumulate
7. **Reference Python code** liberally from `Code_to_Port/` directory
8. **Remove `Code_to_Port/`** only after Phase 7 is complete

---

## Quick Reference Links

- **Original Python Source**: [Code_to_Port/](Code_to_Port/)
- **Porting Requirements**: [PORTING_REQUIREMENTS.md](PORTING_REQUIREMENTS.md)
- **Architecture Guide**: [CLAUDE.md](CLAUDE.md)
- **WMI Test Results**: [WmiTests/TEST_RESULTS.md](WmiTests/TEST_RESULTS.md)
- **F4SE Test Results**: [F4SETests/TEST_RESULTS.md](F4SETests/TEST_RESULTS.md)
- **Avalonia Docs**: https://docs.avaloniaui.net/
- **ReactiveUI Docs**: https://www.reactiveui.net/docs/

---

**Last Updated**: 2025-11-18
**Document Version**: 1.2
**Status**: Phase 1 Complete - Ready for Phase 2
