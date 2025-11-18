using System;
using System.IO;
using System.Linq;
using PeNet;

namespace F4SETests;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== F4SE Plugin Detector Test ===\n");

        // Test with Buffout4.dll
        string testDll = Path.Combine("..", "test_data", "Buffout4.dll");

        if (!File.Exists(testDll))
        {
            Console.WriteLine($"Error: Test file not found: {testDll}");
            Console.WriteLine("Please ensure Buffout4.dll is in the test_data directory.");
            return;
        }

        Console.WriteLine($"Testing: {Path.GetFileName(testDll)}");
        Console.WriteLine($"Full path: {Path.GetFullPath(testDll)}\n");

        var info = ParseF4SEPlugin(testDll);

        if (info == null)
        {
            Console.WriteLine("✗ Failed to parse DLL");
            return;
        }

        Console.WriteLine("=== F4SE Plugin Analysis Results ===\n");
        Console.WriteLine($"Is F4SE Plugin:     {FormatBool(info.IsF4SE)}");
        Console.WriteLine($"Supports OG (1.10): {FormatBool(info.SupportsOG)}");
        Console.WriteLine($"Supports NG (1.18): {FormatBool(info.SupportsNG)}");
        Console.WriteLine();

        // Interpretation
        if (!info.IsF4SE)
        {
            Console.WriteLine("⚠ This is NOT an F4SE plugin (no F4SEPlugin_Load export)");
        }
        else if (info.SupportsOG && info.SupportsNG)
        {
            Console.WriteLine("✓ Universal plugin - supports both OG and NG versions!");
        }
        else if (info.SupportsOG)
        {
            Console.WriteLine("✓ OG-only plugin (Fallout 4 v1.10.163 and earlier)");
        }
        else if (info.SupportsNG)
        {
            Console.WriteLine("✓ NG-only plugin (Fallout 4 v1.10.980+ Next-Gen update)");
        }
        else
        {
            Console.WriteLine("⚠ F4SE plugin but version support unclear");
        }

        // Also show all exports for debugging
        Console.WriteLine("\n=== All Exported Functions ===");
        var allExports = GetExportedFunctions(testDll);
        if (allExports.Count > 0)
        {
            foreach (var export in allExports.OrderBy(e => e))
            {
                bool isF4SE = export.StartsWith("F4SE", StringComparison.Ordinal);
                Console.WriteLine($"  {(isF4SE ? "→" : " ")} {export}");
            }
            Console.WriteLine($"\nTotal exports: {allExports.Count}");
        }
        else
        {
            Console.WriteLine("  (No exports found)");
        }
    }

    static string FormatBool(bool value) => value ? "✓ Yes" : "✗ No";

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
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing DLL: {ex.Message}");
            return null;
        }
    }

    private static HashSet<string> GetExportedFunctions(string dllPath)
    {
        var exports = new HashSet<string>();

        try
        {
            var peFile = new PeFile(dllPath);

            if (peFile.ExportedFunctions != null)
            {
                foreach (var export in peFile.ExportedFunctions)
                {
                    if (!string.IsNullOrEmpty(export.Name))
                    {
                        exports.Add(export.Name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading PE exports: {ex.Message}");
        }

        return exports;
    }
}
