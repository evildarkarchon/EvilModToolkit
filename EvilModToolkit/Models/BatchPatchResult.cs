using System.Collections.Generic;

namespace EvilModToolkit.Models
{
    /// <summary>
    /// Represents the result of patching a single BA2 file in a batch operation.
    /// </summary>
    public class BatchPatchResult
    {
        /// <summary>
        /// Gets or sets the full path to the file.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the patch was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the patch failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the original version of the archive before patching.
        /// </summary>
        public BA2Version? OriginalVersion { get; set; }

        /// <summary>
        /// Gets or sets the target version attempted.
        /// </summary>
        public BA2Version TargetVersion { get; set; }
    }

    /// <summary>
    /// Represents the summary of a batch patching operation.
    /// </summary>
    public class BatchPatchSummary
    {
        /// <summary>
        /// Gets or sets the total number of files processed.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of files successfully patched.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the number of files that failed to patch.
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of files that were skipped (already at target version).
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// Gets or sets the list of individual file results.
        /// </summary>
        public List<BatchPatchResult> Results { get; set; } = new();
    }
}
