using Application.Uploads;

namespace Infrastructure.Vault;

/// <summary>
/// Counts wrong answers in memory. From the third one in a row, the next check waits:
/// 1 second, then double, at most 5 minutes. A restart forgets the count.
/// </summary>
public sealed class WrongTryThrottle(TimeProvider time) : IWrongTryThrottle
{
    private const int TriesBeforeWaiting = 3;
    private const int LongestWaitSeconds = 300;

    private readonly object _gate = new();
    private int _wrongInARow;
    private long _lastWrong;
    private bool _checking;

    public ISecretTry? TryBegin(out TimeSpan retryAfter)
    {
        lock (_gate)
        {
            retryAfter = Remaining();
            if (retryAfter <= TimeSpan.Zero && _checking)
            {
                // Another secret is being checked. Letting this one in would dodge the count.
                retryAfter = TimeSpan.FromSeconds(1);
            }

            if (retryAfter > TimeSpan.Zero)
            {
                return null;
            }

            _checking = true;
            return new Attempt(this);
        }
    }

    private TimeSpan Remaining()
    {
        if (_wrongInARow < TriesBeforeWaiting)
        {
            return TimeSpan.Zero;
        }

        // 2 to the 9th is already past the cap, so the shift stops there and cannot overflow.
        var doublings = Math.Min(_wrongInARow - TriesBeforeWaiting, 9);
        var wait = TimeSpan.FromSeconds(Math.Min(1 << doublings, LongestWaitSeconds));
        // A timestamp, not the wall clock: changing the computer's date does not skip the wait.
        return wait - time.GetElapsedTime(_lastWrong);
    }

    private void Finish(bool? right)
    {
        lock (_gate)
        {
            if (right == true)
            {
                _wrongInARow = 0;
            }
            else if (right == false)
            {
                _wrongInARow = Math.Min(_wrongInARow + 1, int.MaxValue - 1);
                _lastWrong = time.GetTimestamp();
            }

            _checking = false;
        }
    }

    /// <summary>Used by one request only. The first answer, or the dispose, ends it.</summary>
    private sealed class Attempt(WrongTryThrottle owner) : ISecretTry
    {
        private bool _done;

        public void Right() => End(true);

        public void Wrong() => End(false);

        public void Dispose() => End(null);

        private void End(bool? right)
        {
            if (_done)
            {
                return;
            }

            _done = true;
            owner.Finish(right);
        }
    }
}
