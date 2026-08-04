using TeamActivity.Domain;

namespace TeamActivity.RemoteSupport;

[Flags]
public enum SupportCapability
{
    None = 0,
    View = 1,
    Control = 2,
    SystemAudio = 4
}

public sealed class ConsentSession
{
    private readonly IClock clock;

    public ConsentSession(
        Guid requestId,
        string verifiedManager,
        string reason,
        SupportCapability requestedCapabilities,
        TimeSpan maximumDuration,
        IClock clock)
    {
        if (requestId == Guid.Empty) throw new ArgumentException("Request ID is required.", nameof(requestId));
        ArgumentException.ThrowIfNullOrWhiteSpace(verifiedManager);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (requestedCapabilities == SupportCapability.None)
            throw new ArgumentException("At least view permission must be requested.", nameof(requestedCapabilities));
        if (!requestedCapabilities.HasFlag(SupportCapability.View))
            throw new ArgumentException("Control or system audio cannot be requested without view permission.", nameof(requestedCapabilities));
        if (maximumDuration <= TimeSpan.Zero || maximumDuration > TimeSpan.FromHours(2))
            throw new ArgumentOutOfRangeException(nameof(maximumDuration));

        RequestId = requestId;
        VerifiedManager = verifiedManager;
        Reason = reason;
        RequestedCapabilities = requestedCapabilities;
        MaximumDuration = maximumDuration;
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Guid RequestId { get; }
    public string VerifiedManager { get; }
    public string Reason { get; }
    public SupportCapability RequestedCapabilities { get; }
    public SupportCapability GrantedCapabilities { get; private set; }
    public TimeSpan MaximumDuration { get; }
    public RemoteSupportStatus Status { get; private set; } = RemoteSupportStatus.Pending;
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }

    public void Accept(SupportCapability grantedCapabilities)
    {
        EnsurePending();
        if (grantedCapabilities == SupportCapability.None ||
            (grantedCapabilities & ~RequestedCapabilities) != SupportCapability.None)
            throw new InvalidOperationException("Only explicitly requested capabilities may be granted.");
        if (!grantedCapabilities.HasFlag(SupportCapability.View))
            throw new InvalidOperationException("An active support session requires view permission.");

        GrantedCapabilities = grantedCapabilities;
        StartedAtUtc = clock.UtcNow;
        Status = RemoteSupportStatus.Active;
    }

    public void Deny()
    {
        EnsurePending();
        Status = RemoteSupportStatus.Denied;
        EndedAtUtc = clock.UtcNow;
    }

    public void Revoke()
    {
        if (Status != RemoteSupportStatus.Active)
            throw new InvalidOperationException("Only an active session can be revoked.");
        GrantedCapabilities = SupportCapability.None;
        Status = RemoteSupportStatus.Revoked;
        EndedAtUtc = clock.UtcNow;
    }

    public void EndForLockOrSignOut()
    {
        if (Status != RemoteSupportStatus.Active) return;
        GrantedCapabilities = SupportCapability.None;
        Status = RemoteSupportStatus.Ended;
        EndedAtUtc = clock.UtcNow;
    }

    public bool ExpireIfNeeded()
    {
        if (Status != RemoteSupportStatus.Active || StartedAtUtc is null ||
            clock.UtcNow < StartedAtUtc.Value + MaximumDuration)
            return false;

        GrantedCapabilities = SupportCapability.None;
        Status = RemoteSupportStatus.Expired;
        EndedAtUtc = clock.UtcNow;
        return true;
    }

    private void EnsurePending()
    {
        if (Status != RemoteSupportStatus.Pending)
            throw new InvalidOperationException("The request is no longer pending.");
    }
}
