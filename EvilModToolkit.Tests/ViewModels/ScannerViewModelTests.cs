using EvilModToolkit.Models;
using EvilModToolkit.Services.Analysis;
using EvilModToolkit.Services.Game;
using EvilModToolkit.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EvilModToolkit.Tests.ViewModels
{
    public class ScannerViewModelTests
    {
        private readonly IModScannerService _scannerService;
        private readonly IGameDetectionService _gameDetectionService;
        private readonly IModManagerService _modManagerService;
        private readonly ILogger<ScannerViewModel> _logger;

        public ScannerViewModelTests()
        {
            _scannerService = Substitute.For<IModScannerService>();
            _gameDetectionService = Substitute.For<IGameDetectionService>();
            _modManagerService = Substitute.For<IModManagerService>();
            _logger = Substitute.For<ILogger<ScannerViewModel>>();
        }

        [Fact]
        public void Constructor_InitializesProperties()
        {
            var viewModel = new ScannerViewModel(_scannerService, _gameDetectionService, _modManagerService, _logger);

            viewModel.Results.Should().NotBeNull().And.BeEmpty();
            viewModel.ScanOptions.Should().NotBeNull();
            viewModel.ScanCommand.Should().NotBeNull();
            viewModel.CancelScanCommand.Should().NotBeNull();
            viewModel.ResultsInfo.Should().Be("Ready to scan.");
        }

        [Fact]
        public async Task ScanCommand_ExecutesScan()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = true, DataPath = @"C:\Games\Fallout 4\Data" };
            _gameDetectionService.DetectGame().Returns(gameInfo);
            
            var modManagerInfo = new ModManagerInfo { Type = ModManagerType.None };
            _modManagerService.DetectModManagerAsync().Returns(Task.FromResult(modManagerInfo));

            var scanResults = new List<ScanResult>
            {
                new ScanResult(ProblemType.JunkFile, "path", "relpath", "summary")
            };

            _scannerService.ScanAsync(
                Arg.Any<GameInfo>(),
                Arg.Any<ModManagerInfo>(),
                Arg.Any<ScanOptions>(),
                Arg.Any<IProgress<string>>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(scanResults));

            var viewModel = new ScannerViewModel(_scannerService, _gameDetectionService, _modManagerService, _logger);

            // Act
            await viewModel.ScanCommand.Execute();

            // Assert
            viewModel.Results.Should().HaveCount(1);
            viewModel.ResultsInfo.Should().Contain("1 issues found");
            _gameDetectionService.Received().DetectGame();
            await _modManagerService.Received().DetectModManagerAsync();
        }

        [Fact]
        public async Task ScanCommand_HandlesMissingGame()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = false };
            _gameDetectionService.DetectGame().Returns(gameInfo);
            // Second call to DetectGameAsync (simulated via same mock for simplicity if needed, but code uses DetectGame() sync now?) 
            // Wait, code uses DetectGame() then calls DetectGameAsync() if DataPath is null.
            // Let's mock DetectGameAsync too if needed, but I removed it? 
            // No, I checked code. Wait, I replaced DetectGame() usage.
            // Ah, `gameInfo = await _gameDetectionService.DetectGameAsync()` was what I removed/changed.
            // It is now `var gameInfo = _gameDetectionService.DetectGame();`
            
            // If IsInstalled is false, DataPath is likely null/empty.
            
            var viewModel = new ScannerViewModel(_scannerService, _gameDetectionService, _modManagerService, _logger);

            // Act
            await viewModel.ScanCommand.Execute();

            // Assert
            viewModel.ErrorMessage.Should().Contain("Data folder not found");
            viewModel.Results.Should().BeEmpty();
        }
    }
}
