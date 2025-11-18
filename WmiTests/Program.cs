using System;
using System.Diagnostics;
using System.Management;
using System.Security.Principal;

namespace WmiTests;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== WMI Capability Tests ===\n");

        // Check if running as admin
        CheckAdminStatus();
        Console.WriteLine();

        // Test 1: Parent Process Detection
        TestParentProcessDetection();
        Console.WriteLine();

        // Test 2: System Information
        TestSystemInformation();
        Console.WriteLine();

        // Test 3: Process Enumeration
        TestProcessEnumeration();
        Console.WriteLine();

        // Test 4: Hardware Information
        TestHardwareInformation();
        Console.WriteLine();

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static void CheckAdminStatus()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            Console.WriteLine($"Running as Administrator: {isAdmin}");
            Console.WriteLine($"User: {identity.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking admin status: {ex.Message}");
        }
    }

    static void TestParentProcessDetection()
    {
        Console.WriteLine("--- Test 1: Parent Process Detection ---");

        try
        {
            var currentProcess = Process.GetCurrentProcess();
            Console.WriteLine($"Current Process: {currentProcess.ProcessName} (PID: {currentProcess.Id})");

            // Walk up the process tree
            int currentPid = currentProcess.Id;
            int depth = 0;
            const int maxDepth = 5;

            while (depth < maxDepth)
            {
                int parentPid = GetParentProcessId(currentPid);
                if (parentPid <= 0)
                {
                    Console.WriteLine($"  [{depth}] No more parent processes");
                    break;
                }

                try
                {
                    using var parentProcess = Process.GetProcessById(parentPid);
                    string processName = parentProcess.ProcessName;
                    string? mainModule = null;

                    try
                    {
                        mainModule = parentProcess.MainModule?.FileName;
                    }
                    catch
                    {
                        mainModule = "(access denied)";
                    }

                    Console.WriteLine($"  [{depth}] Parent: {processName} (PID: {parentPid})");
                    Console.WriteLine($"      Path: {mainModule}");

                    // Try to get version info
                    if (mainModule != null && mainModule != "(access denied)")
                    {
                        try
                        {
                            var versionInfo = FileVersionInfo.GetVersionInfo(mainModule);
                            Console.WriteLine($"      Version: {versionInfo.FileMajorPart}.{versionInfo.FileMinorPart}.{versionInfo.FileBuildPart}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"      Version: (error: {ex.Message})");
                        }
                    }

                    currentPid = parentPid;
                    depth++;
                }
                catch (ArgumentException)
                {
                    Console.WriteLine($"  [{depth}] Parent process (PID: {parentPid}) no longer exists");
                    break;
                }
            }

            Console.WriteLine($"✓ Parent process detection successful (walked {depth} levels)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Parent process detection failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    static int GetParentProcessId(int processId)
    {
        try
        {
            using var query = new ManagementObjectSearcher(
                $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}");

            using var results = query.Get();
            foreach (ManagementObject result in results)
            {
                return Convert.ToInt32(result["ParentProcessId"]);
            }
        }
        catch (ManagementException ex)
        {
            Console.WriteLine($"    WMI Error: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"    Access Denied: {ex.Message}");
        }

        return -1;
    }

    static void TestSystemInformation()
    {
        Console.WriteLine("--- Test 2: System Information ---");

        // Test OS Information
        try
        {
            using var query = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            using var results = query.Get();

            foreach (ManagementObject os in results)
            {
                Console.WriteLine($"OS: {os["Caption"]}");
                Console.WriteLine($"Version: {os["Version"]}");
                Console.WriteLine($"Build: {os["BuildNumber"]}");
                Console.WriteLine($"Architecture: {os["OSArchitecture"]}");

                // Total Physical Memory
                if (os["TotalVisibleMemorySize"] != null)
                {
                    long totalMemoryKB = Convert.ToInt64(os["TotalVisibleMemorySize"]);
                    double totalMemoryGB = totalMemoryKB / 1024.0 / 1024.0;
                    Console.WriteLine($"Total RAM: {totalMemoryGB:F2} GB");
                }

                break; // Only one OS
            }
            Console.WriteLine("✓ OS information query successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ OS information query failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    static void TestProcessEnumeration()
    {
        Console.WriteLine("--- Test 3: Process Enumeration ---");

        try
        {
            using var query = new ManagementObjectSearcher(
                "SELECT ProcessId, Name, ExecutablePath FROM Win32_Process WHERE Name LIKE '%.exe'");

            using var results = query.Get();
            int count = 0;

            Console.WriteLine("First 10 processes:");
            foreach (ManagementObject process in results)
            {
                if (count >= 10) break;

                Console.WriteLine($"  PID {process["ProcessId"]}: {process["Name"]}");
                if (process["ExecutablePath"] != null)
                {
                    Console.WriteLine($"    Path: {process["ExecutablePath"]}");
                }

                count++;
            }

            Console.WriteLine($"✓ Process enumeration successful (found {count}+ processes)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Process enumeration failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    static void TestHardwareInformation()
    {
        Console.WriteLine("--- Test 4: Hardware Information ---");

        // Test CPU Information
        try
        {
            using var query = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            using var results = query.Get();

            foreach (ManagementObject cpu in results)
            {
                Console.WriteLine($"CPU: {cpu["Name"]}");
                Console.WriteLine($"  Cores: {cpu["NumberOfCores"]}");
                Console.WriteLine($"  Logical Processors: {cpu["NumberOfLogicalProcessors"]}");
                Console.WriteLine($"  Max Clock Speed: {cpu["MaxClockSpeed"]} MHz");
                break; // Show first CPU
            }
            Console.WriteLine("✓ CPU information query successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ CPU information query failed: {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine();

        // Test GPU Information
        try
        {
            using var query = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            using var results = query.Get();

            int gpuCount = 0;
            foreach (ManagementObject gpu in results)
            {
                Console.WriteLine($"GPU {gpuCount + 1}: {gpu["Name"]}");

                if (gpu["AdapterRAM"] != null)
                {
                    long vramBytes = Convert.ToInt64(gpu["AdapterRAM"]);
                    double vramGB = vramBytes / 1024.0 / 1024.0 / 1024.0;
                    Console.WriteLine($"  VRAM: {vramGB:F2} GB");
                }

                Console.WriteLine($"  Driver Version: {gpu["DriverVersion"]}");
                gpuCount++;

                if (gpuCount >= 2) break; // Show first 2 GPUs
            }
            Console.WriteLine($"✓ GPU information query successful (found {gpuCount} GPU(s))");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ GPU information query failed: {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine();

        // Test Physical Memory Information
        try
        {
            using var query = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            using var results = query.Get();

            int moduleCount = 0;
            long totalCapacity = 0;

            foreach (ManagementObject memory in results)
            {
                long capacity = Convert.ToInt64(memory["Capacity"]);
                totalCapacity += capacity;

                double capacityGB = capacity / 1024.0 / 1024.0 / 1024.0;
                Console.WriteLine($"RAM Module {moduleCount + 1}: {capacityGB:F2} GB");
                Console.WriteLine($"  Speed: {memory["Speed"]} MHz");
                Console.WriteLine($"  Manufacturer: {memory["Manufacturer"]}");

                moduleCount++;
            }

            double totalGB = totalCapacity / 1024.0 / 1024.0 / 1024.0;
            Console.WriteLine($"Total Installed RAM: {totalGB:F2} GB ({moduleCount} modules)");
            Console.WriteLine("✓ Physical memory information query successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Physical memory information query failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
