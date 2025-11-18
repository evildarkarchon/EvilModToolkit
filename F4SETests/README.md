# F4SE Plugin Detection Tests

This test project validates F4SE plugin detection using the PeNet library for parsing PE file export tables.

## Purpose

The Evil Modding Toolkit needs to scan F4SE plugin DLLs to determine:
1. If a DLL is an F4SE plugin
2. If it supports the Original Game (pre-Next-Gen, v1.10.163)
3. If it supports the Next-Gen update (v1.10.980+)

This is done by checking for specific exported function names in the DLL.

## F4SE Plugin Detection

F4SE plugins export specific functions to indicate their capabilities:

- **`F4SEPlugin_Load`** - Required for all F4SE plugins
- **`F4SEPlugin_Query`** - Indicates support for **Original Game**
- **`F4SEPlugin_Version`** - Indicates support for **Next-Gen update**

A plugin can export:
- Only `F4SEPlugin_Query` → OG-only plugin
- Only `F4SEPlugin_Version` → NG-only plugin
- Both → Universal plugin (supports both versions)

## Test DLL

**Buffout4.dll** - A real F4SE plugin that is universal (supports both OG and NG)

Located in: `../test_data/Buffout4.dll`

## Running the Tests

```bash
dotnet run
```

**Expected Output**:
```
=== F4SE Plugin Analysis Results ===

Is F4SE Plugin:     ✓ Yes
Supports OG (1.10): ✓ Yes
Supports NG (1.18): ✓ Yes

✓ Universal plugin - supports both OG and NG versions!
```

## Test Results

See [TEST_RESULTS.md](TEST_RESULTS.md) for detailed test results.

**Summary**: ✅ PeNet successfully parses F4SE plugin exports!

## Dependencies

- **.NET**: 10.0+ (or adjust target framework in .csproj)
- **NuGet Package**: PeNet 5.1.0

## Implementation Code

The test includes production-ready code for F4SE plugin detection:

```csharp
public class F4SEPluginInfo
{
    public bool IsF4SE { get; set; }
    public bool SupportsOG { get; set; }
    public bool SupportsNG { get; set; }
}

public static F4SEPluginInfo? ParseF4SEPlugin(string dllPath)
{
    var exports = GetExportedFunctions(dllPath);

    return new F4SEPluginInfo
    {
        IsF4SE = exports.Contains("F4SEPlugin_Load"),
        SupportsOG = exports.Contains("F4SEPlugin_Query"),
        SupportsNG = exports.Contains("F4SEPlugin_Version")
    };
}
```

## Integration with Main Project

This tested code will be integrated into the Evil Modding Toolkit's service layer:

- `IF4SEPluginService` - Service interface for F4SE plugin scanning
- `F4SEPluginService` - Implementation using PeNet
- `F4SEPluginInfo` model - Plugin capability information

## Files

- `Program.cs` - Test program with F4SE detection logic
- `TEST_RESULTS.md` - Detailed test results and analysis
- `README.md` - This file

## Why PeNet?

1. ✅ **Simple API** - Easy to use, minimal code
2. ✅ **Reliable** - Successfully parses real F4SE plugins
3. ✅ **Well-maintained** - Active NuGet package with regular updates
4. ✅ **No P/Invoke needed** - Pure managed code, no unsafe code
5. ✅ **Cross-platform capable** - Works on Windows, Linux (via Wine), macOS

## Notes

- PeNet only reads the PE file structure, it does not load or execute the DLL
- This is safe to run on any DLL file without risk of code execution
- The detection logic exactly matches the original Python implementation's behavior
