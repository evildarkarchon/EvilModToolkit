# Porting Requirements

This document tracks key technical requirements and challenges for porting the Collective Modding Toolkit from Python to C#.

## Critical Platform-Specific Functionality

### 1. Parent Process Detection

**Requirement**: Detect if the application was launched from a mod manager (Mod Organizer 2 or Vortex) by walking up the process tree.

**Python Implementation** ([Code_to_Port/src/utils.py:155-174](Code_to_Port/src/utils.py#L155-L174)):
```python
def find_mod_manager() -> ModManagerInfo | None:
    pid = os.getppid()  # Get parent process ID
    proc: Process | None = Process(pid)  # psutil.Process

    managers = {"ModOrganizer.exe", "Vortex.exe"}

    # Walk up to 8 levels in process tree
    for _ in range(8):
        if proc is None:
            break

        with proc.oneshot():
            if proc.name() in managers:
                manager_path = Path(proc.exe())
                manager = "Mod Organizer" if proc.name() == "ModOrganizer.exe" else "Vortex"
                ver = get_file_version(manager_path)
                manager_version = Version(".".join(str(n) for n in ver[:3])) if ver else Version("0.0.0")
                return ModManagerInfo(manager, manager_path, manager_version)
            proc = proc.parent()  # Move to parent process

    return None
```

**C# Solution Options**:

1. **Using WMI (System.Management)** - Most reliable cross-version approach:
   ```csharp
   // NuGet: System.Management
   using System.Management;
   using System.Diagnostics;

   public static ModManagerInfo? FindModManager()
   {
       var currentProcess = Process.GetCurrentProcess();
       var managers = new HashSet<string> { "ModOrganizer.exe", "Vortex.exe" };

       int currentPid = currentProcess.Id;

       for (int i = 0; i < 8; i++)
       {
           int parentPid = GetParentProcessId(currentPid);
           if (parentPid <= 0) break;

           try
           {
               using var parentProcess = Process.GetProcessById(parentPid);
               string processName = parentProcess.ProcessName + ".exe";

               if (managers.Contains(processName))
               {
                   // Found mod manager - get details
                   string managerPath = parentProcess.MainModule?.FileName ?? string.Empty;
                   string manager = processName == "ModOrganizer.exe" ? "Mod Organizer" : "Vortex";

                   // Use native .NET API to get file version
                   var versionInfo = FileVersionInfo.GetVersionInfo(managerPath);
                   var version = new Version(
                       versionInfo.FileMajorPart,
                       versionInfo.FileMinorPart,
                       versionInfo.FileBuildPart
                   );

                   return new ModManagerInfo(manager, managerPath, version);
               }

               currentPid = parentPid;
           }
           catch (ArgumentException)
           {
               // Process no longer exists
               break;
           }
       }

       return null;
   }

   private static int GetParentProcessId(int processId)
   {
       try
       {
           using var query = new ManagementObjectSearcher(
               $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}");

           using var results = query.Get();
           foreach (var result in results)
           {
               return Convert.ToInt32(result["ParentProcessId"]);
           }
       }
       catch (ManagementException)
       {
           // WMI access failed - may need admin rights or WMI is disabled
       }
       catch (UnauthorizedAccessException)
       {
           // No permission to query WMI
       }

       return -1;
   }
   ```

2. **Using Windows API (P/Invoke)** - No WMI dependency, works without admin:
   ```csharp
   [DllImport("ntdll.dll")]
   private static extern int NtQueryInformationProcess(
       IntPtr processHandle,
       int processInformationClass,
       ref PROCESS_BASIC_INFORMATION processInformation,
       int processInformationLength,
       out int returnLength);

   [StructLayout(LayoutKind.Sequential)]
   private struct PROCESS_BASIC_INFORMATION
   {
       public IntPtr Reserved1;
       public IntPtr PebBaseAddress;
       public IntPtr Reserved2_0;
       public IntPtr Reserved2_1;
       public IntPtr UniqueProcessId;
       public IntPtr InheritedFromUniqueProcessId;
   }

   private static int GetParentProcessId(Process process)
   {
       var pbi = new PROCESS_BASIC_INFORMATION();
       int returnLength;
       int status = NtQueryInformationProcess(
           process.Handle,
           0,
           ref pbi,
           Marshal.SizeOf(pbi),
           out returnLength);

       if (status != 0)
           return -1;

       return pbi.InheritedFromUniqueProcessId.ToInt32();
   }
   ```

**Recommendation**:
- ✅ **Use System.Management (WMI)** approach - confirmed working without admin
- Handle rare WMI failures gracefully by returning `null`
- No fallback mechanism needed - WMI is reliable on modern Windows

**Test Results**: ✅ Confirmed working on Windows 11 without admin privileges (see [WmiTests/TEST_RESULTS.md](WmiTests/TEST_RESULTS.md))

**Dependencies Required**:
- **NuGet Package**: `System.Management` (for WMI access)
- **Framework**: .NET 8.0 (already targeted)

### 2. System Information Collection

**Requirement**: Collect PC information (OS version, RAM, CPU, GPU) for diagnostic purposes.

**Python Implementation** ([Code_to_Port/src/helpers.py:65-89](Code_to_Port/src/helpers.py#L65-L89)):
- Uses `psutil.virtual_memory()` for RAM
- Uses `platform` module for OS info
- Uses Windows Registry and WMI for hardware info

**C# Solutions**:
- **OS Information**: `System.Management` (Win32_OperatingSystem) ✅ Tested
- **RAM**: `System.Management` (Win32_PhysicalMemory) ✅ Tested
- **CPU**: `System.Management` (Win32_Processor) ✅ Tested
- **GPU**: `System.Management` (Win32_VideoController) ✅ Tested

**Test Results**: ✅ All queries confirmed working without admin privileges on Windows 11

### 3. File Version Information from PE Executables

**Requirement**: Extract version information from executables (mod managers, F4SE DLLs, game files).

**Python Implementation**: Uses `win32api.GetFileVersionInfo()`

**C# Solution** - Native .NET API (no libraries needed):
```csharp
using System.Diagnostics;

// Works for any PE executable (EXE, DLL) on Windows
var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
var version = new Version(
    versionInfo.FileMajorPart,
    versionInfo.FileMinorPart,
    versionInfo.FileBuildPart,
    versionInfo.FilePrivatePart
);

// Also provides:
// - versionInfo.ProductVersion (string)
// - versionInfo.FileVersion (string)
// - versionInfo.ProductName
// - versionInfo.CompanyName
// etc.
```

**Notes**:
- No special permissions required
- Works reliably on all Windows versions
- Native .NET API, no NuGet packages needed
- Handles both EXE and DLL files automatically

## Binary File Operations

### 4. F4SE Plugin DLL Analysis

**Requirement**: Detect F4SE plugin capabilities by checking for exported function names in DLL files.

**Python Implementation** ([Code_to_Port/src/utils.py:220-230](Code_to_Port/src/utils.py#L220-L230)):
```python
from ctypes import WinDLL, DONT_RESOLVE_DLL_REFERENCES

def parse_dll(file_path: Path) -> DLLInfo | None:
    try:
        dll = WinDLL(str(file_path), winmode=DONT_RESOLVE_DLL_REFERENCES)
        dll_info: DLLInfo = {
            "IsF4SE": hasattr(dll, "F4SEPlugin_Load"),
            "SupportsOG": hasattr(dll, "F4SEPlugin_Query"),
            "SupportsNG": hasattr(dll, "F4SEPlugin_Version"),
        }
    except OSError:
        return None
    return dll_info
```

**What it detects**:
- `F4SEPlugin_Load` - exported function indicates this is an F4SE plugin
- `F4SEPlugin_Query` - exported function indicates support for **Original Game (pre-NG)**
- `F4SEPlugin_Version` - exported function indicates support for **Next-Gen update**

**C# Solution**: Parse PE export table to check for exported function names

```csharp
using System.IO;
using System.Runtime.InteropServices;

public class F4SEPluginInfo
{
    public bool IsF4SE { get; set; }
    public bool SupportsOG { get; set; }
    public bool SupportsNG { get; set; }
}

public static F4SEPluginInfo? ParseF4SEPlugin(string dllPath)
{
    try
    {
        var exports = GetExportedFunctions(dllPath);

        return new F4SEPluginInfo
        {
            IsF4SE = exports.Contains("F4SEPlugin_Load"),
            SupportsOG = exports.Contains("F4SEPlugin_Query"),
            SupportsNG = exports.Contains("F4SEPlugin_Version")
        };
    }
    catch (Exception)
    {
        return null;
    }
}

private static HashSet<string> GetExportedFunctions(string dllPath)
{
    // Parse PE file export directory
    // Options:
    // 1. Use PeNet library (NuGet)
    // 2. Manual parsing with BinaryReader
    // 3. P/Invoke to ImageNtHeader and related APIs
}
```

**C# Implementation Options**:

1. **PeNet Library** (Recommended - easiest):
   ```csharp
   // NuGet: PeNet
   using PeNet;

   var peFile = new PeFile(dllPath);
   var exports = peFile.ExportedFunctions
       .Select(f => f.Name)
       .ToHashSet();

   bool isF4SE = exports.Contains("F4SEPlugin_Load");
   ```

2. **Manual PE Parsing** (No dependencies):
   - Parse DOS header → PE header → Optional header → Export directory
   - Read export table and function names
   - More complex but no external dependencies

3. **P/Invoke** (Windows API):
   ```csharp
   [DllImport("dbghelp.dll", SetLastError = true)]
   static extern IntPtr ImageDirectoryEntryToData(
       IntPtr Base,
       bool MappedAsImage,
       ushort DirectoryEntry,
       out uint Size);
   ```

**Recommendation**: ✅ **Use PeNet library** - tested and confirmed working with real F4SE plugins.

**Test Results**: ✅ Successfully tested with Buffout4.dll - correctly detected as universal plugin supporting both OG and NG (see [F4SETests/TEST_RESULTS.md](F4SETests/TEST_RESULTS.md))

**Dependencies**:
- ✅ **Confirmed**: PeNet 5.1.0 NuGet package (recommended and tested)
- **Option 2**: None (manual parsing - more complex)
- **Option 3**: None (P/Invoke to Windows APIs - more complex)

### 5. BA2 Archive Manipulation

**Requirement**: Patch Bethesda Archive v2 (BA2) files for version conversion (v1 ↔ v8).

**Python Implementation**: Simple hex digit flip to change version byte in the archive header.

**C# Solution**:

BA2 files have a simple header structure:
- Offset 0-3: Magic number "BTDX" (4 bytes)
- Offset 4: Version byte (1 byte)
  - `0x01` = v1 (Original Game)
  - `0x07` = v7 (Next-Gen, rare)
  - `0x08` = v8 (Next-Gen, standard)

```csharp
using System.IO;

public enum BA2Version : byte
{
    OG = 0x01,   // Original Game
    NG7 = 0x07,  // Next-Gen (rare)
    NG = 0x08    // Next-Gen (standard)
}

public static bool PatchBA2Version(string ba2FilePath, BA2Version targetVersion)
{
    try
    {
        // Check if file is read-only and remove flag if needed
        var fileInfo = new FileInfo(ba2FilePath);
        if (fileInfo.IsReadOnly)
        {
            fileInfo.IsReadOnly = false;
        }

        using var stream = new FileStream(ba2FilePath, FileMode.Open, FileAccess.ReadWrite);

        // Verify magic number "BTDX"
        byte[] magic = new byte[4];
        if (stream.Read(magic, 0, 4) != 4)
            return false;

        if (magic[0] != 0x42 || magic[1] != 0x54 ||
            magic[2] != 0x44 || magic[3] != 0x58) // "BTDX"
            return false;

        // Read current version byte at offset 4
        int currentVersion = stream.ReadByte();
        if (currentVersion == -1)
            return false;

        // Check if already target version
        if (currentVersion == (byte)targetVersion)
            return false; // Already patched

        // Validate version is recognized (0x01, 0x07, or 0x08)
        if (currentVersion != 0x01 && currentVersion != 0x07 && currentVersion != 0x08)
            return false; // Unrecognized version

        // Seek back to offset 4 and write new version
        stream.Seek(4, SeekOrigin.Begin);
        stream.WriteByte((byte)targetVersion);

        return true;
    }
    catch (Exception)
    {
        return false;
    }
}
```

**Notes**:
- Simple binary file modification at offset 4
- Python implementation: [Code_to_Port/src/patcher/_archives.py:109-173](Code_to_Port/src/patcher/_archives.py#L109-L173)
- Requires handling read-only files (remove flag before patching)

### 6. Game Downgrade/Upgrade Feature

**Requirement**: Apply delta patches to downgrade or upgrade Fallout 4 executables using xdelta3.

**Python Implementation**: Uses `pyxdelta` library

**C# Solution**: Use `Process.Start` to invoke `xdelta3.exe` directly (simplest approach for maintainability)

```csharp
using System.Diagnostics;

public async Task<bool> ApplyPatchAsync(string sourceFile, string patchFile, string outputFile)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "xdelta3.exe",  // Bundled with application or in PATH
        Arguments = $"-d -s \"{sourceFile}\" \"{patchFile}\" \"{outputFile}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    using var process = Process.Start(startInfo);
    if (process == null)
        return false;

    await process.WaitForExitAsync();
    return process.ExitCode == 0;
}
```

**Distribution Strategy**:
- Bundle `xdelta3.exe` in application directory
- Alternative: Allow user to specify path in settings if not bundled

**Notes**: This is a separate feature from BA2 manipulation. Using `Process.Start` is simple, maintainable, and avoids P/Invoke complexity.

## Missing Python Libraries to Replace

### 7. Python Standard Library Replacements

| Python Module | C# Equivalent |
|--------------|---------------|
| `pathlib.Path` | `System.IO.Path`, `FileInfo`, `DirectoryInfo` |
| `json` | `System.Text.Json.JsonSerializer` |
| `threading.Thread` | `async/await`, `Task`, `Thread` |
| `winreg` | `Microsoft.Win32.Registry` |
| `platform` | `Environment`, `System.Runtime.InteropServices.RuntimeInformation` |
| `struct` (binary) | `BinaryReader`, `BinaryWriter`, `Marshal` |
| `win32api.GetFileVersionInfo()` | `FileVersionInfo.GetVersionInfo()` (native .NET) |

### 8. Third-Party Python Library Replacements

| Python Package | C# Solution | Notes |
|---------------|-------------|-------|
| `psutil` | `System.Management` (WMI), `System.Diagnostics.Process` | For process/system info |
| `pyxdelta` | `Process.Start("xdelta3.exe")` | For downgrade/upgrade feature |
| `tkinter` | Avalonia UI | Already planned |

## Windows Registry Access

### 9. Game and Mod Manager Detection via Registry

**Requirement**: Detect Fallout 4 installation path and mod manager installations from Windows Registry.

**Python Implementation**: Uses `winreg` module

**C# Solution**:
```csharp
using Microsoft.Win32;

// Read Steam installation path for Fallout 4 (App ID 377160)
using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 377160");
string? installPath = key?.GetValue("InstallLocation") as string;

// Alternative: Read from Steam registry
using var steamKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
// Parse libraryfolders.vdf or registry values
```

**Notes**: Registry access typically doesn't require admin rights for reading HKLM and HKCU keys.

## Async Operations and Threading

### 10. Long-Running Operations

**Requirement**: Perform file scanning, patching, and analysis without blocking UI.

**Python Implementation**: Uses `threading.Thread` with Tkinter

**C# Solution with Avalonia + ReactiveUI**:
```csharp
public ReactiveCommand<Unit, Unit> ScanCommand { get; }

public MyViewModel(IScannerService scannerService)
{
    ScanCommand = ReactiveCommand.CreateFromTask(async () =>
    {
        // Long-running operation on background thread
        await scannerService.ScanModsAsync();
    });
}
```

**Best Practices**:
- Use `async/await` throughout
- Use `ConfigureAwait(false)` for library code
- Use ReactiveCommand for all UI-triggered async operations
- Use `IProgress<T>` for progress reporting
- Use `CancellationToken` for cancellable operations

## Required NuGet Packages

### 11. Dependencies Summary

**Core Framework**:
- ✅ Avalonia (11.x) - UI framework
- ✅ Avalonia.ReactiveUI - MVVM support
- ✅ Microsoft.Extensions.DependencyInjection - DI container
- ✅ Microsoft.Extensions.Logging - Logging

**Platform-Specific**:
- ✅ **System.Management** - WMI access for parent process detection and system info (tested, working)
- ✅ **PeNet 5.1.0** - Parse DLL export tables for F4SE plugin detection (tested with Buffout4.dll)

**External Tools**:
- 📦 **xdelta3.exe** - Bundled with application for downgrade/upgrade feature

**Native .NET (No Packages Needed)**:
- ✅ File version reading (native `FileVersionInfo`)
- ✅ Binary file manipulation (native `BinaryReader`/`BinaryWriter`)
- ✅ Registry access (native `Microsoft.Win32.Registry`)
- ✅ Process management (native `System.Diagnostics.Process`)

## Testing Considerations

### 12. Platform Testing Requirements

- **Windows 10/11**: Primary target
- **Windows 7/8.1**: May need testing if supporting older OS
- **Admin vs Non-Admin**: Test WMI queries under both privilege levels
- **Mod Manager Integration**: Test parent process detection when launched from MO2 and Vortex
- **Fallout 4 Versions**: Test with various game versions (pre-NG, post-NG)
- **WMI Disabled**: Test graceful degradation when WMI service is disabled

## WMI Test Results ✅

**All WMI queries tested and confirmed working WITHOUT admin privileges!**

See [WmiTests/TEST_RESULTS.md](WmiTests/TEST_RESULTS.md) for full test results.

**Key Findings**:
- ✅ Parent process detection works without admin (tested on Windows 11)
- ✅ System information queries (OS, RAM, CPU, GPU) work without admin
- ✅ Process enumeration works without admin
- ✅ FileVersionInfo works for version extraction
- ✅ No fallback mechanism needed - WMI is reliable on modern Windows

**Test Environment**: Windows 11 Pro (Build 26200), .NET 10.0, Standard User (Non-Admin)

## Open Questions / TODO

- [x] ~~Test WMI access permissions on non-admin accounts for parent process detection~~ ✅ CONFIRMED WORKING
- [x] ~~Test WMI access permissions for system information queries (RAM, CPU, GPU)~~ ✅ CONFIRMED WORKING
- [x] ~~Determine fallback strategy if WMI is unavailable~~ ✅ NOT NEEDED - WMI works reliably
- [x] ~~Test parent process detection with MO2 and Vortex in actual launch scenarios~~ ❌ DEFERRED, will test during integration phase.
- [x] ~~Document exact BA2 version byte offset and manipulation logic from Python code~~ ✅ DOCUMENTED
- [x] ~~Determine xdelta3.exe bundling strategy (include in build or user-provided)~~ ✅ DECIDED - Bundle with application (open-source)
- [x] ~~Test xdelta3.exe invocation on various Windows versions~~ ❌ DEFERRED - Will test during integration phase
- [x] ~~Verify if F4SE plugins need custom metadata parsing beyond FileVersionInfo~~ ✅ YES - Need to parse PE export table for F4SE function names
- [x] ~~Choose PE export table parsing approach (PeNet library vs. manual parsing)~~ ✅ CHOSEN - PeNet library (tested successfully)
- [x] ~~Implement and test F4SE plugin detection with sample DLLs~~ ✅ TESTED - Buffout4.dll correctly identified as universal plugin

## References

- [System.Management Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.management)
- [WMI Win32_Process Class](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-process)
- [FileVersionInfo Class](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.fileversioninfo)
- [ProcessDiagnosticInfo.Parent (UWP)](https://learn.microsoft.com/en-us/uwp/api/windows.system.diagnostics.processdiagnosticinfo.parent)
- [xdelta3 Homepage](http://xdelta.org/)
