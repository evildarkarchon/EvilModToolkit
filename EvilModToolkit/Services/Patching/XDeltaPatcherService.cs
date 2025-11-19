using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit.Services.Patching;

/// <summary>
/// Service for applying xdelta patches using xdelta3.exe.
/// </summary>
public class XDeltaPatcherService : IXDeltaPatcherService
{
    private readonly ILogger<XDeltaPatcherService> _logger;
    private const string XDelta3FileName = "xdelta3.exe";

    public XDeltaPatcherService(ILogger<XDeltaPatcherService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PatchResult> ApplyPatchAsync(
        string sourceFilePath,
        string patchFilePath,
        string outputFilePath,
        IProgress<PatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        if (!File.Exists(sourceFilePath))
        {
            var error = $"Source file not found: {sourceFilePath}";
            _logger.LogError(error);
            return new PatchResult
            {
                Success = false,
                ErrorMessage = error,
                ExitCode = -1
            };
        }

        if (!File.Exists(patchFilePath))
        {
            var error = $"Patch file not found: {patchFilePath}";
            _logger.LogError(error);
            return new PatchResult
            {
                Success = false,
                ErrorMessage = error,
                ExitCode = -1
            };
        }

        var xdeltaPath = GetXDelta3Path();
        if (string.IsNullOrEmpty(xdeltaPath))
        {
            var error = "xdelta3.exe not found";
            _logger.LogError(error);
            return new PatchResult
            {
                Success = false,
                ErrorMessage = error,
                ExitCode = -1
            };
        }

        progress?.Report(new PatchProgress
        {
            Stage = PatchStage.Starting,
            Percentage = 0,
            Message = "Starting patch operation..."
        });

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = xdeltaPath,
                Arguments = $"-d -s \"{sourceFilePath}\" \"{patchFilePath}\" \"{outputFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            using var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    stdout.AppendLine(e.Data);
                    _logger.LogDebug("xdelta3 stdout: {Output}", e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    stderr.AppendLine(e.Data);
                    _logger.LogWarning("xdelta3 stderr: {Error}", e.Data);
                }
            };

            progress?.Report(new PatchProgress
            {
                Stage = PatchStage.Patching,
                Percentage = 50,
                Message = "Applying patch..."
            });

            _logger.LogInformation("Starting xdelta3: {Arguments}", startInfo.Arguments);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var exitCode = process.ExitCode;
            var stdoutStr = stdout.ToString();
            var stderrStr = stderr.ToString();

            if (exitCode == 0 && File.Exists(outputFilePath))
            {
                progress?.Report(new PatchProgress
                {
                    Stage = PatchStage.Completed,
                    Percentage = 100,
                    Message = "Patch applied successfully"
                });

                _logger.LogInformation("Patch applied successfully: {OutputFile}", outputFilePath);
                return new PatchResult
                {
                    Success = true,
                    OutputFilePath = outputFilePath,
                    ExitCode = exitCode,
                    StandardOutput = stdoutStr,
                    StandardError = stderrStr
                };
            }
            else
            {
                var error = $"xdelta3 failed with exit code {exitCode}";
                progress?.Report(new PatchProgress
                {
                    Stage = PatchStage.Failed,
                    Percentage = 0,
                    Message = error
                });

                _logger.LogError("Patch failed: {Error}. stderr: {Stderr}", error, stderrStr);
                return new PatchResult
                {
                    Success = false,
                    ErrorMessage = error,
                    ExitCode = exitCode,
                    StandardOutput = stdoutStr,
                    StandardError = stderrStr
                };
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Patch operation cancelled");
            progress?.Report(new PatchProgress
            {
                Stage = PatchStage.Failed,
                Percentage = 0,
                Message = "Operation cancelled"
            });

            return new PatchResult
            {
                Success = false,
                ErrorMessage = "Operation cancelled",
                ExitCode = -1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during patch operation");
            progress?.Report(new PatchProgress
            {
                Stage = PatchStage.Failed,
                Percentage = 0,
                Message = $"Error: {ex.Message}"
            });

            return new PatchResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExitCode = -1
            };
        }
    }

    /// <inheritdoc />
    public virtual string? GetXDelta3Path()
    {
        // Check in application directory first
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var xdeltaInAppDir = Path.Combine(appDirectory, XDelta3FileName);
        if (File.Exists(xdeltaInAppDir))
        {
            return xdeltaInAppDir;
        }

        // Check in current working directory
        var currentDirectory = Directory.GetCurrentDirectory();
        var xdeltaInCurrentDir = Path.Combine(currentDirectory, XDelta3FileName);
        if (File.Exists(xdeltaInCurrentDir))
        {
            return xdeltaInCurrentDir;
        }

        // Check in PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (var path in pathEnv.Split(Path.PathSeparator))
            {
                var xdeltaInPath = Path.Combine(path, XDelta3FileName);
                if (File.Exists(xdeltaInPath))
                {
                    return xdeltaInPath;
                }
            }
        }

        _logger.LogWarning("xdelta3.exe not found in app directory, working directory, or PATH");
        return null;
    }

    /// <inheritdoc />
    public bool IsXDelta3Available()
    {
        var path = GetXDelta3Path();
        return !string.IsNullOrEmpty(path);
    }

    /// <inheritdoc />
    public async Task<(bool IsValid, string? ErrorMessage)> ValidatePatchAsync(string sourceFilePath, string patchFilePath)
    {
        // Check if source file exists
        if (!File.Exists(sourceFilePath))
        {
            return (false, $"Source file not found: {sourceFilePath}");
        }

        // Check if patch file exists
        if (!File.Exists(patchFilePath))
        {
            return (false, $"Patch file not found: {patchFilePath}");
        }

        // Check if xdelta3 is available
        if (!IsXDelta3Available())
        {
            return (false, "xdelta3.exe not found. Please ensure xdelta3.exe is in the application directory or PATH.");
        }

        // Check if source file is readable
        try
        {
            await using var sourceStream = File.OpenRead(sourceFilePath);
            // File is readable
        }
        catch (UnauthorizedAccessException)
        {
            return (false, $"Access denied to source file: {sourceFilePath}");
        }
        catch (IOException ex)
        {
            return (false, $"Cannot read source file: {ex.Message}");
        }

        // Check if patch file is readable and appears to be a valid xdelta file
        try
        {
            await using var patchStream = File.OpenRead(patchFilePath);

            // Check for xdelta3 magic bytes (VCDIFF format)
            // VCDIFF files start with magic bytes: 0xD6 0xC3 0xC4 (VCD in ASCII-ish)
            var buffer = new byte[4];
            var bytesRead = await patchStream.ReadAsync(buffer, 0, 4);

            if (bytesRead < 4)
            {
                return (false, "Patch file is too small to be a valid xdelta patch.");
            }

            // Check for VCDIFF magic number
            if (buffer[0] != 0xD6 || buffer[1] != 0xC3 || buffer[2] != 0xC4)
            {
                _logger.LogWarning("Patch file does not have VCDIFF magic bytes. First bytes: {Bytes}",
                    BitConverter.ToString(buffer));
                // Don't fail validation on this - xdelta3 might still be able to handle it
                // but log a warning
            }
        }
        catch (UnauthorizedAccessException)
        {
            return (false, $"Access denied to patch file: {patchFilePath}");
        }
        catch (IOException ex)
        {
            return (false, $"Cannot read patch file: {ex.Message}");
        }

        // Check if there's enough disk space for the operation
        try
        {
            var sourceFileInfo = new FileInfo(sourceFilePath);
            var driveInfo = new DriveInfo(Path.GetPathRoot(sourceFilePath) ?? "C:\\");

            // We need space for: backup file + temp patched file
            // Estimate: 2x source file size (conservative estimate)
            var requiredSpace = sourceFileInfo.Length * 2;

            if (driveInfo.AvailableFreeSpace < requiredSpace)
            {
                return (false, $"Insufficient disk space. Required: {requiredSpace / (1024 * 1024)} MB, Available: {driveInfo.AvailableFreeSpace / (1024 * 1024)} MB");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check disk space");
            // Don't fail validation on disk space check failure
        }

        // All validation checks passed
        return (true, null);
    }
}
