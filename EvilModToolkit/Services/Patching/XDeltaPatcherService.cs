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
}
