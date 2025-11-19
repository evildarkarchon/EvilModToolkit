using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Threading;

namespace EvilModToolkit.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels in the application.
    /// Provides common functionality including busy state management, error handling, and cancellation support.
    /// </summary>
    public abstract class ViewModelBase : ReactiveObject, IDisposable
    {
        private bool _isBusy;
        private string? _errorMessage;
        private string? _statusMessage;
        private double _progressPercentage;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposed;

        /// <summary>
        /// Gets or sets a value indicating whether the ViewModel is currently performing an operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            protected set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets the current error message, if any.
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            protected set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        /// <summary>
        /// Gets or sets the current status message.
        /// </summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            protected set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets or sets the progress percentage (0-100) for long-running operations.
        /// </summary>
        public double ProgressPercentage
        {
            get => _progressPercentage;
            protected set => this.RaiseAndSetIfChanged(ref _progressPercentage, value);
        }

        /// <summary>
        /// Gets a value indicating whether there is an active error.
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// Gets the cancellation token for the current operation.
        /// </summary>
        protected CancellationToken CancellationToken =>
            _cancellationTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// Clears the current error message.
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = null;
        }

        /// <summary>
        /// Sets an error message.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        protected void SetError(string message)
        {
            ErrorMessage = message;
        }

        /// <summary>
        /// Sets an error message from an exception.
        /// </summary>
        /// <param name="exception">The exception to extract the message from.</param>
        /// <param name="logger">Optional logger to log the exception details.</param>
        protected void SetError(Exception exception, ILogger? logger = null)
        {
            logger?.LogError(exception, "Error in {ViewModelName}", GetType().Name);
            ErrorMessage = exception.Message;
        }

        /// <summary>
        /// Clears the current status message.
        /// </summary>
        protected void ClearStatus()
        {
            StatusMessage = null;
        }

        /// <summary>
        /// Sets a status message.
        /// </summary>
        /// <param name="message">The status message to display.</param>
        protected void SetStatus(string message)
        {
            StatusMessage = message;
        }

        /// <summary>
        /// Creates a new cancellation token source for an operation.
        /// Cancels and disposes any existing token source.
        /// </summary>
        protected void CreateCancellationTokenSource()
        {
            CancelOperation();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Cancels the current operation if one is in progress.
        /// </summary>
        protected void CancelOperation()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Executes an action and handles any exceptions by setting the error message.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="logger">Optional logger to log exceptions.</param>
        /// <returns>True if the action executed successfully, false if an exception occurred.</returns>
        protected bool TryExecute(Action action, ILogger? logger = null)
        {
            try
            {
                ClearError();
                action();
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex, logger);
                return false;
            }
        }

        /// <summary>
        /// Executes an async action and handles any exceptions by setting the error message.
        /// </summary>
        /// <param name="action">The async action to execute.</param>
        /// <param name="logger">Optional logger to log exceptions.</param>
        /// <returns>True if the action executed successfully, false if an exception occurred.</returns>
        protected async System.Threading.Tasks.Task<bool> TryExecuteAsync(
            Func<System.Threading.Tasks.Task> action,
            ILogger? logger = null)
        {
            try
            {
                ClearError();
                await action();
                return true;
            }
            catch (OperationCanceledException)
            {
                SetStatus("Operation cancelled");
                return false;
            }
            catch (Exception ex)
            {
                SetError(ex, logger);
                return false;
            }
        }

        /// <summary>
        /// Disposes resources used by the ViewModel.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources used by the ViewModel.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                CancelOperation();
            }

            _disposed = true;
        }
    }
}
