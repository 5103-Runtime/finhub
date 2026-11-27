namespace FinHub.Api.Services;

/// <summary>
/// Limits guest statement previews per client while counting only successful parses.
/// </summary>
/// <param name="permitLimit">The number of successful previews allowed per client during the window.</param>
/// <param name="window">The time window for successful previews.</param>
/// <param name="timeProvider">The clock used to track preview times.</param>
public sealed class GuestPreviewRateLimiter(int permitLimit, TimeSpan window, TimeProvider timeProvider)
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, PreviewState> _clients = new(StringComparer.Ordinal);
    private DateTimeOffset _nextCleanup = DateTimeOffset.MinValue;

    /// <summary>
    /// Reserves a preview slot before parsing, or rejects the request if its quota is exhausted
    /// or another preview for that client is already being parsed.
    /// </summary>
    /// <param name="clientKey">The key identifying the client, usually its IP address.</param>
    /// <param name="retryAfter">The suggested delay before retrying when no lease is returned.</param>
    /// <returns>A lease to complete after parsing, or <see langword="null"/> when the request is limited.</returns>
    public PreviewLease? TryAcquire(string clientKey, out TimeSpan retryAfter)
    {
        lock (_gate)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            CleanupExpiredClients(now);

            if (_clients.TryGetValue(clientKey, out PreviewState? state))
            {
                ExpireSuccessfulPreviews(state, now);

                if (state.IsParsing)
                {
                    retryAfter = TimeSpan.FromSeconds(1);
                    return null;
                }

                if (state.SuccessfulPreviews.Count >= permitLimit)
                {
                    retryAfter = state.SuccessfulPreviews.Peek() + window - now;
                    return null;
                }
            }
            else
            {
                state = new PreviewState();
                _clients.Add(clientKey, state);
            }

            state.IsParsing = true;
            retryAfter = TimeSpan.Zero;
            return new PreviewLease(
                () => MarkSuccessful(state),
                () => Release(state));
        }
    }

    /// <summary>
    /// Records a successful preview for the client holding the reservation.
    /// </summary>
    /// <param name="state">The client's current preview state.</param>
    private void MarkSuccessful(PreviewState state)
    {
        lock (_gate)
        {
            state.SuccessfulPreviews.Enqueue(timeProvider.GetUtcNow());
        }
    }

    /// <summary>
    /// Releases the client's active parsing reservation.
    /// </summary>
    /// <param name="state">The client's current preview state.</param>
    private void Release(PreviewState state)
    {
        lock (_gate)
        {
            state.IsParsing = false;
        }
    }

    /// <summary>
    /// Removes idle client state that no longer has successful previews in the active window.
    /// </summary>
    /// <param name="now">The current time used to determine expiration.</param>
    private void CleanupExpiredClients(DateTimeOffset now)
    {
        if (now < _nextCleanup)
        {
            return;
        }

        foreach (KeyValuePair<string, PreviewState> client in _clients.ToArray())
        {
            PreviewState state = client.Value;
            ExpireSuccessfulPreviews(state, now);
            if (!state.IsParsing && state.SuccessfulPreviews.Count == 0)
            {
                _clients.Remove(client.Key);
            }
        }

        _nextCleanup = now + window;
    }

    /// <summary>
    /// Removes successful previews that have aged out of the configured window.
    /// </summary>
    /// <param name="state">The client's preview state to update.</param>
    /// <param name="now">The current time used to determine expiration.</param>
    private void ExpireSuccessfulPreviews(PreviewState state, DateTimeOffset now)
    {
        while (state.SuccessfulPreviews.TryPeek(out DateTimeOffset previewTime)
            && now >= previewTime + window)
        {
            state.SuccessfulPreviews.Dequeue();
        }
    }

    /// <summary>
    /// Tracks active parsing and successful previews for one client.
    /// </summary>
    private sealed class PreviewState
    {
        public bool IsParsing { get; set; }
        public Queue<DateTimeOffset> SuccessfulPreviews { get; } = new();
    }

    /// <summary>
    /// Represents a reserved guest preview slot.
    /// </summary>
    /// <remarks>
    /// Call <see cref="MarkSuccessful"/> only after parsing succeeds. Disposing an uncompleted
    /// lease releases the reservation without consuming a successful-preview permit.
    /// </remarks>
    public sealed class PreviewLease : IDisposable
    {
        private readonly Action _markSuccessful;
        private readonly Action _release;
        private readonly Lock _gate = new();
        private bool _disposed;

        /// <summary>
        /// Initializes a lease with actions for recording success and releasing its reservation.
        /// </summary>
        /// <param name="markSuccessful">The action that records a successful parse.</param>
        /// <param name="release">The action that releases the parsing reservation.</param>
        internal PreviewLease(Action markSuccessful, Action release)
        {
            _markSuccessful = markSuccessful;
            _release = release;
        }

        /// <summary>
        /// Records the preview as successful so it counts toward the client's quota.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The lease has already been disposed.</exception>
        public void MarkSuccessful()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(PreviewLease));
                }

                _markSuccessful();
            }
        }

        /// <summary>
        /// Releases the reservation and does not count it unless <see cref="MarkSuccessful"/> was called.
        /// </summary>
        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _release();
            }
        }
    }
}
