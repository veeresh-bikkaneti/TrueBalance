namespace Application.Uploads;

/// <summary>Slows down guessing of a passphrase or a recovery code.</summary>
public interface IWrongTryThrottle
{
    /// <summary>Claims the right to check one secret. Only one claim is held at a time.</summary>
    /// <returns>Null when the caller must wait <paramref name="retryAfter"/> and must not check the secret.</returns>
    ISecretTry? TryBegin(out TimeSpan retryAfter);
}

/// <summary>One claimed check. Dispose it without an answer and the count stays as it was.</summary>
public interface ISecretTry : IDisposable
{
    void Right();

    void Wrong();
}
