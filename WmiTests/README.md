# WMI Tests for Evil Modding Toolkit

This test project validates WMI (Windows Management Instrumentation) capabilities needed for the C# port of the Evil Modding Toolkit.

## Purpose

The Evil Modding Toolkit needs to:
1. Detect parent processes (to identify if launched from Mod Organizer 2 or Vortex)
2. Collect system information for diagnostics (OS, CPU, GPU, RAM)
3. Enumerate processes for various features

This test program validates that all required WMI queries work **without administrator privileges**.

## What It Tests

- ✅ **Parent Process Detection**: Walking up the process tree via `Win32_Process.ParentProcessId`
- ✅ **System Information**: Querying OS details via `Win32_OperatingSystem`
- ✅ **Process Enumeration**: Listing running processes via `Win32_Process`
- ✅ **Hardware Information**: CPU (`Win32_Processor`), GPU (`Win32_VideoController`), RAM (`Win32_PhysicalMemory`)

## Running the Tests

```bash
dotnet run
```

**Expected Result**: All tests should pass without requiring administrator elevation.

## Test Results

See [TEST_RESULTS.md](TEST_RESULTS.md) for detailed test results from Windows 11.

**Summary**: All WMI queries work perfectly without admin privileges! ✅

## Dependencies

- **.NET**: 10.0+ (or adjust target framework in .csproj)
- **NuGet Package**: System.Management 10.0.0

## Files

- `Program.cs` - Main test program
- `TEST_RESULTS.md` - Detailed test results and findings
- `README.md` - This file

## Integration with Main Project

The confirmed working WMI queries from this test project will be integrated into the Evil Modding Toolkit's service layer:

- `IModManagerService` - Parent process detection
- `ISystemInfoService` - Hardware/OS information collection

## Notes

- These tests were specifically designed to validate the porting requirements from Python's `psutil` library to C#'s `System.Management`
- The parent process detection mimics the original Python implementation's behavior
- All queries are read-only and safe to run
