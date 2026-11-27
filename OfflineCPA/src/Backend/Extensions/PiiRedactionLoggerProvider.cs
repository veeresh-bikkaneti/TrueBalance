using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backend.Extensions;

/// <summary>
/// Wraps a logger provider so that every message passes the PII masker
/// before the inner (console) logger sees it. Console formatters print the
/// whole exception chain, so every message on the chain is checked; if any
/// changes under masking, the chain is rebuilt from type names and masked
/// messages so raw values cannot reach logs.
/// Limitation: log scopes are forwarded unmasked; scopes stay off by
/// default in this app.
/// </summary>
public sealed class PiiRedactionLoggerProvider : ILoggerProvider
{
    private readonly ILoggerProvider _inner;
    private readonly IPiiMasker _masker;

    public PiiRedactionLoggerProvider(ILoggerProvider inner, IPiiMasker masker)
    {
        _inner = inner;
        _masker = masker;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new PiiRedactionLogger(_inner.CreateLogger(categoryName), _masker);
    }

    public void Dispose()
    {
        _inner.Dispose();
    }

    private sealed class PiiRedactionLogger : ILogger
    {
        private readonly ILogger _inner;
        private readonly IPiiMasker _masker;

        public PiiRedactionLogger(ILogger inner, IPiiMasker masker)
        {
            _inner = inner;
            _masker = masker;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _inner.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _inner.IsEnabled(logLevel);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            string masked;
            try
            {
                masked = _masker.Mask(formatter(state, exception));
                if (exception is not null)
                {
                    exception = Sanitize(exception);
                }
            }
            catch (Exception)
            {
                // Fail closed: the raw line and exception never go further.
                masked = "[log line withheld: masking failed]";
                exception = null;
            }

            _inner.Log(logLevel, eventId, new MaskedState(masked), exception, static (s, _) => s.Message);
        }

        /// <summary>
        /// Console formatters print the whole exception chain, so every
        /// message on the chain is masked. When nothing changed, the
        /// original exception (with its stack trace) is kept; when any
        /// message changed, the chain is rebuilt from type names and masked
        /// messages, and the raw stacks are dropped.
        /// </summary>
        private Exception Sanitize(Exception exception)
        {
            return ChainChanges(exception) ? RebuildMasked(exception) : exception;
        }

        private bool ChainChanges(Exception? exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (_masker.Mask(current.Message) != current.Message)
                {
                    return true;
                }

                if (current is AggregateException aggregate)
                {
                    if (aggregate.InnerExceptions.Any(ChainChanges))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Exception RebuildMasked(Exception exception)
        {
            if (exception is AggregateException aggregate)
            {
                return new AggregateException(
                    _masker.Mask(aggregate.Message),
                    aggregate.InnerExceptions.Select(RebuildMasked));
            }

            var inner = exception.InnerException is null
                ? null
                : RebuildMasked(exception.InnerException);
            var maskedMessage = _masker.Mask(exception.Message);
            return new Exception($"{exception.GetType().Name}: {maskedMessage}", inner);
        }

        private readonly struct MaskedState
        {
            public readonly string Message;

            public MaskedState(string message)
            {
                Message = message;
            }
        }
    }
}
