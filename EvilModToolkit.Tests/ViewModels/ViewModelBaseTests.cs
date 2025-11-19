using EvilModToolkit.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EvilModToolkit.Tests.ViewModels
{
    public class ViewModelBaseTests
    {
        // Concrete implementation of ViewModelBase for testing
        private class TestViewModel : ViewModelBase
        {
            public void PublicSetError(string message) => SetError(message);
            public void PublicSetError(Exception ex, ILogger? logger = null) => SetError(ex, logger);
            public void PublicClearError() => ClearError();
            public void PublicSetStatus(string message) => SetStatus(message);
            public void PublicClearStatus() => ClearStatus();
            public void PublicCreateCancellationTokenSource() => CreateCancellationTokenSource();
            public void PublicCancelOperation() => CancelOperation();
            public CancellationToken PublicCancellationToken => CancellationToken;
            public bool PublicTryExecute(Action action, ILogger? logger = null) => TryExecute(action, logger);
            public Task<bool> PublicTryExecuteAsync(Func<Task> action, ILogger? logger = null) => TryExecuteAsync(action, logger);

            public void PublicSetIsBusy(bool value) => IsBusy = value;
            public void PublicSetProgressPercentage(double value) => ProgressPercentage = value;
        }

        [Fact]
        public void Constructor_InitializesPropertiesWithDefaultValues()
        {
            // Arrange & Act
            var viewModel = new TestViewModel();

            // Assert
            viewModel.IsBusy.Should().BeFalse();
            viewModel.ErrorMessage.Should().BeNull();
            viewModel.StatusMessage.Should().BeNull();
            viewModel.ProgressPercentage.Should().Be(0.0);
            viewModel.HasError.Should().BeFalse();
        }

        [Fact]
        public void IsBusy_RaisesPropertyChanged()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var propertyChanged = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModelBase.IsBusy))
                    propertyChanged = true;
            };

            // Act
            viewModel.PublicSetIsBusy(true);

            // Assert
            viewModel.IsBusy.Should().BeTrue();
            propertyChanged.Should().BeTrue();
        }

        [Fact]
        public void ErrorMessage_RaisesPropertyChanged()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var propertyChanged = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModelBase.ErrorMessage))
                    propertyChanged = true;
            };

            // Act
            viewModel.PublicSetError("Test error");

            // Assert
            viewModel.ErrorMessage.Should().Be("Test error");
            propertyChanged.Should().BeTrue();
        }

        [Fact]
        public void StatusMessage_RaisesPropertyChanged()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var propertyChanged = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModelBase.StatusMessage))
                    propertyChanged = true;
            };

            // Act
            viewModel.PublicSetStatus("Test status");

            // Assert
            viewModel.StatusMessage.Should().Be("Test status");
            propertyChanged.Should().BeTrue();
        }

        [Fact]
        public void ProgressPercentage_RaisesPropertyChanged()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var propertyChanged = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModelBase.ProgressPercentage))
                    propertyChanged = true;
            };

            // Act
            viewModel.PublicSetProgressPercentage(50.0);

            // Assert
            viewModel.ProgressPercentage.Should().Be(50.0);
            propertyChanged.Should().BeTrue();
        }

        [Fact]
        public void HasError_ReturnsTrueWhenErrorMessageIsSet()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            viewModel.PublicSetError("Test error");

            // Assert
            viewModel.HasError.Should().BeTrue();
        }

        [Fact]
        public void HasError_ReturnsFalseWhenErrorMessageIsNull()
        {
            // Arrange
            var viewModel = new TestViewModel();
            viewModel.PublicSetError("Test error");

            // Act
            viewModel.PublicClearError();

            // Assert
            viewModel.HasError.Should().BeFalse();
        }

        [Fact]
        public void HasError_ReturnsFalseWhenErrorMessageIsWhitespace()
        {
            // Arrange
            var viewModel = new TestViewModel();
            viewModel.PublicSetError("   ");

            // Assert
            viewModel.HasError.Should().BeFalse();
        }

        [Fact]
        public void SetError_WithString_SetsErrorMessage()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            viewModel.PublicSetError("Test error");

            // Assert
            viewModel.ErrorMessage.Should().Be("Test error");
        }

        [Fact]
        public void SetError_WithException_SetsErrorMessageToExceptionMessage()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var exception = new InvalidOperationException("Test exception");

            // Act
            viewModel.PublicSetError(exception);

            // Assert
            viewModel.ErrorMessage.Should().Be("Test exception");
        }

        [Fact]
        public void SetError_WithExceptionAndLogger_LogsException()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var logger = Substitute.For<ILogger>();
            var exception = new InvalidOperationException("Test exception");

            // Act
            viewModel.PublicSetError(exception, logger);

            // Assert
            logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                exception,
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public void ClearError_SetsErrorMessageToNull()
        {
            // Arrange
            var viewModel = new TestViewModel();
            viewModel.PublicSetError("Test error");

            // Act
            viewModel.PublicClearError();

            // Assert
            viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void SetStatus_SetsStatusMessage()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            viewModel.PublicSetStatus("Test status");

            // Assert
            viewModel.StatusMessage.Should().Be("Test status");
        }

        [Fact]
        public void ClearStatus_SetsStatusMessageToNull()
        {
            // Arrange
            var viewModel = new TestViewModel();
            viewModel.PublicSetStatus("Test status");

            // Act
            viewModel.PublicClearStatus();

            // Assert
            viewModel.StatusMessage.Should().BeNull();
        }

        [Fact]
        public void CreateCancellationTokenSource_CreatesCancellationToken()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            viewModel.PublicCreateCancellationTokenSource();

            // Assert
            viewModel.PublicCancellationToken.Should().NotBe(CancellationToken.None);
            viewModel.PublicCancellationToken.CanBeCanceled.Should().BeTrue();
        }

        [Fact]
        public void CreateCancellationTokenSource_CancelsPreviousToken()
        {
            // Arrange
            var viewModel = new TestViewModel();
            viewModel.PublicCreateCancellationTokenSource();
            var firstToken = viewModel.PublicCancellationToken;

            // Act
            viewModel.PublicCreateCancellationTokenSource();

            // Assert
            firstToken.IsCancellationRequested.Should().BeTrue();
            viewModel.PublicCancellationToken.Should().NotBe(firstToken);
        }

        [Fact]
        public void CancelOperation_CancelsCancellationToken()
        {
            // Arrange
            var viewModel = new TestViewModel();
            viewModel.PublicCreateCancellationTokenSource();
            var token = viewModel.PublicCancellationToken;

            // Act
            viewModel.PublicCancelOperation();

            // Assert
            token.IsCancellationRequested.Should().BeTrue();
            viewModel.PublicCancellationToken.Should().Be(CancellationToken.None);
        }

        [Fact]
        public void CancelOperation_WhenNoTokenExists_DoesNotThrow()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            Action act = () => viewModel.PublicCancelOperation();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void TryExecute_SuccessfulAction_ReturnsTrue()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var executed = false;

            // Act
            var result = viewModel.PublicTryExecute(() => executed = true);

            // Assert
            result.Should().BeTrue();
            executed.Should().BeTrue();
            viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void TryExecute_ThrowingAction_ReturnsFalseAndSetsError()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            var result = viewModel.PublicTryExecute(() => throw new InvalidOperationException("Test exception"));

            // Assert
            result.Should().BeFalse();
            viewModel.ErrorMessage.Should().Be("Test exception");
        }

        [Fact]
        public void TryExecute_ClearsErrorBeforeExecution()
        {
            // Arrange
            var viewModel = new TestViewModel();
            viewModel.PublicSetError("Previous error");

            // Act
            viewModel.PublicTryExecute(() => { });

            // Assert
            viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task TryExecuteAsync_SuccessfulAction_ReturnsTrue()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var executed = false;

            // Act
            var result = await viewModel.PublicTryExecuteAsync(async () =>
            {
                await Task.Delay(10);
                executed = true;
            });

            // Assert
            result.Should().BeTrue();
            executed.Should().BeTrue();
            viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task TryExecuteAsync_ThrowingAction_ReturnsFalseAndSetsError()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            var result = await viewModel.PublicTryExecuteAsync(async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test exception");
            });

            // Assert
            result.Should().BeFalse();
            viewModel.ErrorMessage.Should().Be("Test exception");
        }

        [Fact]
        public async Task TryExecuteAsync_CancelledOperation_ReturnsFalseAndSetsStatus()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            var result = await viewModel.PublicTryExecuteAsync(async () =>
            {
                await Task.Delay(10);
                throw new OperationCanceledException();
            });

            // Assert
            result.Should().BeFalse();
            viewModel.StatusMessage.Should().Be("Operation cancelled");
            viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task TryExecuteAsync_ClearsErrorBeforeExecution()
        {
            // Arrange
            var viewModel = new TestViewModel();
            viewModel.PublicSetError("Previous error");

            // Act
            await viewModel.PublicTryExecuteAsync(async () => await Task.Delay(10));

            // Assert
            viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void Dispose_CancelsOperation()
        {
            // Arrange
            var viewModel = new TestViewModel();
            viewModel.PublicCreateCancellationTokenSource();
            var token = viewModel.PublicCancellationToken;

            // Act
            viewModel.Dispose();

            // Assert
            token.IsCancellationRequested.Should().BeTrue();
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            Action act = () =>
            {
                viewModel.Dispose();
                viewModel.Dispose();
                viewModel.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }
    }
}
