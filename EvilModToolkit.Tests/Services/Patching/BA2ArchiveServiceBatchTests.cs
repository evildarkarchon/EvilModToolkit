using System;
using System.IO;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Patching;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EvilModToolkit.Tests.Services.Patching
{
    public class BA2ArchiveServiceBatchTests : IDisposable
    {
        private readonly ILogger<BA2ArchiveService> _mockLogger;
        private readonly BA2ArchiveService _service;
        private readonly string _testDirectory;

        public BA2ArchiveServiceBatchTests()
        {
            _mockLogger = Substitute.For<ILogger<BA2ArchiveService>>();
            _service = new BA2ArchiveService(_mockLogger);
            _testDirectory = Path.Combine(Path.GetTempPath(), "BA2BatchTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        private void CreateDummyBA2(string fileName, BA2Version version)
        {
            var path = Path.Combine(_testDirectory, fileName);
            using var stream = new FileStream(path, FileMode.Create);

            // Write Magic "BTDX"
            stream.Write(new byte[] { 0x42, 0x54, 0x44, 0x58 }, 0, 4);

            // Write Version
            stream.WriteByte((byte)version);
            stream.Write(new byte[3], 0, 3); // Padding

            // Write Type "GNRL"
            stream.Write(new byte[] { 0x47, 0x4E, 0x52, 0x4C }, 0, 4);

            // Write some dummy data
            stream.Write(new byte[100], 0, 100);
        }

        [Fact]
        public void GetBA2FilesInDirectory_ReturnsCorrectFiles()
        {
            // Arrange
            CreateDummyBA2("test1.ba2", BA2Version.V1);
            CreateDummyBA2("test2.ba2", BA2Version.V1);
            File.WriteAllText(Path.Combine(_testDirectory, "not_ba2.txt"), "content");

            // Act
            var files = _service.GetBA2FilesInDirectory(_testDirectory);

            // Assert
            Assert.Equal(2, files.Length);
            Assert.Contains(files, f => f.EndsWith("test1.ba2"));
            Assert.Contains(files, f => f.EndsWith("test2.ba2"));
        }

        [Fact]
        public async Task BatchPatchDirectoryAsync_PatchesFilesCorrectly()
        {
            // Arrange
            CreateDummyBA2("v1_file.ba2", BA2Version.V1);
            CreateDummyBA2("v7_file.ba2", BA2Version.V7); // Already at target

            // Act
            var result = await _service.BatchPatchDirectoryAsync(_testDirectory, BA2Version.V7);

            // Assert
            Assert.Equal(2, result.TotalFiles);
            Assert.Equal(1, result.SuccessCount); // v1 -> v7
            Assert.Equal(1, result.SkippedCount); // v7 -> v7
            Assert.Equal(0, result.FailedCount);

            // Verify file content
            var v1File = Path.Combine(_testDirectory, "v1_file.ba2");
            var info = _service.GetArchiveInfo(v1File);
            Assert.Equal(BA2Version.V7, info?.Version);
        }

        [Fact]
        public async Task BatchPatchDirectoryAsync_HandlesSubdirectories()
        {
            // Arrange
            var subDir = Path.Combine(_testDirectory, "SubDir");
            Directory.CreateDirectory(subDir);

            CreateDummyBA2("root.ba2", BA2Version.V1);

            var subPath = Path.Combine(subDir, "sub.ba2");
            using (var stream = new FileStream(subPath, FileMode.Create))
            {
                // Write Magic "BTDX"
                stream.Write(new byte[] { 0x42, 0x54, 0x44, 0x58 }, 0, 4);
                // Write Version V1
                stream.WriteByte((byte)BA2Version.V1);
                stream.Write(new byte[3], 0, 3);
                // Write Type "GNRL"
                stream.Write(new byte[] { 0x47, 0x4E, 0x52, 0x4C }, 0, 4);
            }

            // Act
            var result = await _service.BatchPatchDirectoryAsync(_testDirectory, BA2Version.V8, includeSubdirectories: true);

            // Assert
            Assert.Equal(2, result.TotalFiles);
            Assert.Equal(2, result.SuccessCount);
        }
    }
}
