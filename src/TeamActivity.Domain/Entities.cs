using System.ComponentModel.DataAnnotations;

namespace TeamActivity.Domain;

public enum RoleKind { Administrator, Manager, Reviewer, Auditor, Employee }
public enum DeviceState { Pending, Online, Degraded, Offline, Revoked }
public enum ActivityKind { Active, Idle, ApprovedBreak, Locked, SignedOut, Offline, Unknown }
public enum AlertConfidence { Confirmed, Inferred }
public enum ReviewStatus { Draft, Submitted, Acknowledged }
public enum CorrectionStatus { Pending, Approved, Rejected }
public enum RemoteSupportStatus { Pending, Denied, Active, Revoked, Ended, Expired }

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; }
    [Timestamp] public byte[]? RowVersion { get; set; }
}

public sealed class Employee : Entity
{
    [MaxLength(120)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(160)] public string? Email { get; set; }
    public ICollection<Device> Devices { get; set; } = [];
}

public sealed class Manager : Entity
{
    [MaxLength(120)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(160)] public string WindowsIdentity { get; set; } = string.Empty;
}

public sealed class Role : Entity
{
    public RoleKind Kind { get; set; }
    [MaxLength(120)] public string PrincipalId { get; set; } = string.Empty;
}

public sealed class Device : Entity
{
    [MaxLength(128)] public string InstallationIdHash { get; set; } = string.Empty;
    [MaxLength(128)] public string TokenHash { get; set; } = string.Empty;
    [MaxLength(40)] public string ApplicationVersion { get; set; } = string.Empty;
    public DeviceState State { get; set; } = DeviceState.Pending;
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTimeOffset? LastHeartbeatUtc { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    [MaxLength(120)] public string? ApprovedBy { get; set; }
}

public sealed class DeviceEnrollment : Entity
{
    [MaxLength(128)] public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public Guid? DeviceId { get; set; }
}

public sealed class DeviceCertificate : Entity
{
    public Guid DeviceId { get; set; }
    [MaxLength(128)] public string Thumbprint { get; set; } = string.Empty;
    public DateTimeOffset NotBeforeUtc { get; set; }
    public DateTimeOffset NotAfterUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

public sealed class Policy : Entity
{
    [MaxLength(40)] public string Version { get; set; } = "1";
    public int ScreenshotMinimumMinutes { get; set; } = 5;
    public int ScreenshotMaximumMinutes { get; set; } = 10;
    public int IdleThresholdMinutes { get; set; } = 5;
    public int ScreenshotRetentionDays { get; set; } = 30;
    public bool RemoteSupportEnabled { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PolicyAcceptance : Entity
{
    public Guid DeviceId { get; set; }
    [MaxLength(40)] public string PolicyVersion { get; set; } = string.Empty;
    public DateTimeOffset AcceptedAtUtc { get; set; }
}

public sealed class WorkSchedule : Entity
{
    public Guid EmployeeId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartsAtLocal { get; set; }
    public TimeOnly EndsAtLocal { get; set; }
    [MaxLength(80)] public string TimeZoneId { get; set; } = "UTC";
}

public sealed class WorkSession : Entity
{
    public Guid EmployeeId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
}

public sealed class ActivityBucket : Entity
{
    public Guid DeviceId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset EndedAtUtc { get; set; }
    public ActivityKind State { get; set; }
    public DateTimeOffset? LastKeyboardInputAtUtc { get; set; }
    public DateTimeOffset? LastMouseInputAtUtc { get; set; }
    public int KeyboardEventCount { get; set; }
    public int MouseEventCount { get; set; }
    public double MouseDistancePixels { get; set; }
}

public sealed class TimeSegment : Entity
{
    public Guid EmployeeId { get; set; }
    public ActivityKind State { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset EndedAtUtc { get; set; }
}

public sealed class TimeCorrection : Entity
{
    public Guid EmployeeId { get; set; }
    public Guid TimeSegmentId { get; set; }
    public DateTimeOffset OriginalStartUtc { get; set; }
    public DateTimeOffset OriginalEndUtc { get; set; }
    public DateTimeOffset RequestedStartUtc { get; set; }
    public DateTimeOffset RequestedEndUtc { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    public CorrectionStatus Status { get; set; } = CorrectionStatus.Pending;
    [MaxLength(120)] public string? DecidedBy { get; set; }
}

public sealed class ScreenshotSet : Entity
{
    public Guid DeviceId { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; }
    [MaxLength(128)] public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset RetainUntilUtc { get; set; }
    public ICollection<ScreenshotAsset> Assets { get; set; } = [];
}

public sealed class ScreenshotAsset : Entity
{
    public Guid ScreenshotSetId { get; set; }
    public ScreenshotSet? ScreenshotSet { get; set; }
    public int MonitorNumber { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double Scale { get; set; }
    public int OrientationDegrees { get; set; }
    [MaxLength(128)] public string ContentHash { get; set; } = string.Empty;
    [MaxLength(500)] public string EncryptedPath { get; set; } = string.Empty;
}

public sealed class Heartbeat : Entity
{
    public Guid DeviceId { get; set; }
    public DateTimeOffset SentAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    [MaxLength(40)] public string ApplicationVersion { get; set; } = string.Empty;
    [MaxLength(40)] public string Health { get; set; } = string.Empty;
    public int PendingUploadCount { get; set; }
}

public sealed class Alert : Entity
{
    public Guid? DeviceId { get; set; }
    [MaxLength(80)] public string Type { get; set; } = string.Empty;
    [MaxLength(1000)] public string Message { get; set; } = string.Empty;
    public AlertConfidence Confidence { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
}

public sealed class WorkTask : Entity
{
    public Guid EmployeeId { get; set; }
    [MaxLength(240)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string Outcome { get; set; } = string.Empty;
    [MaxLength(2000)] public string AcceptanceCriteria { get; set; } = string.Empty;
    public DateTimeOffset? DueAtUtc { get; set; }
}

public sealed class QualityReview : Entity
{
    public Guid WorkTaskId { get; set; }
    public ReviewStatus Status { get; set; }
    public int Accuracy { get; set; }
    public int Completeness { get; set; }
    public int Timeliness { get; set; }
    public int Communication { get; set; }
    [MaxLength(4000)] public string Evidence { get; set; } = string.Empty;
}

public sealed class ReviewComment : Entity
{
    public Guid QualityReviewId { get; set; }
    [MaxLength(120)] public string Author { get; set; } = string.Empty;
    [MaxLength(4000)] public string Body { get; set; } = string.Empty;
}

public sealed class RemoteSupportRequest : Entity
{
    public Guid DeviceId { get; set; }
    [MaxLength(120)] public string RequestedBy { get; set; } = string.Empty;
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    public bool RequestView { get; set; }
    public bool RequestControl { get; set; }
    public bool RequestSystemAudio { get; set; }
    public RemoteSupportStatus Status { get; set; } = RemoteSupportStatus.Pending;
}

public sealed class RemoteSupportSession : Entity
{
    public Guid RemoteSupportRequestId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public RemoteSupportStatus Status { get; set; }
    public bool ViewEnabled { get; set; }
    public bool ControlEnabled { get; set; }
    public bool SystemAudioEnabled { get; set; }
}

public sealed class AuditEvent : Entity
{
    [MaxLength(120)] public string Actor { get; set; } = string.Empty;
    [MaxLength(120)] public string Action { get; set; } = string.Empty;
    [MaxLength(120)] public string SubjectType { get; set; } = string.Empty;
    [MaxLength(120)] public string SubjectId { get; set; } = string.Empty;
    [MaxLength(4000)] public string DetailsJson { get; set; } = "{}";
    [MaxLength(128)] public string PreviousHash { get; set; } = string.Empty;
    [MaxLength(128)] public string Hash { get; set; } = string.Empty;
}

public sealed class RetentionPolicy : Entity
{
    [MaxLength(80)] public string DataType { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public bool LegalHold { get; set; }
}

public sealed class ExportJob : Entity
{
    [MaxLength(120)] public string RequestedBy { get; set; } = string.Empty;
    [MaxLength(40)] public string Format { get; set; } = string.Empty;
    [MaxLength(80)] public string ReportType { get; set; } = string.Empty;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    [MaxLength(500)] public string? OutputPath { get; set; }
}
