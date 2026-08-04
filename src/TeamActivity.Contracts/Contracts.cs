namespace TeamActivity.Contracts;

public enum ActivityState
{
    Active,
    Idle,
    ApprovedBreak,
    Locked,
    SignedOut,
    Offline,
    Unknown
}

public enum DeviceConnectionState
{
    Pending,
    Online,
    Degraded,
    Offline,
    Revoked
}

public sealed record CreateEnrollmentCodeRequest(int LifetimeMinutes = 10);
public sealed record EnrollmentCodeResponse(string Code, DateTimeOffset ExpiresAtUtc);

public sealed record EnrollDeviceRequest(
    string Code,
    string InstallationId,
    string PolicyVersion,
    DateTimeOffset AcceptedAtUtc,
    string ApplicationVersion);

public sealed record EnrollDeviceResponse(
    Guid DeviceId,
    DeviceConnectionState State,
    string DeviceToken,
    string PolicyVersion);

public sealed record ApproveDeviceRequest(Guid EmployeeId, string ApprovedBy);
public sealed record CreateEmployeeRequest(string DisplayName, string? Email);
public sealed record EmployeeResponse(Guid EmployeeId, string DisplayName, string? Email);

public sealed record HeartbeatRequest(
    Guid DeviceId,
    string ApplicationVersion,
    DateTimeOffset SentAtUtc,
    string Health,
    int PendingUploadCount);

public sealed record HeartbeatResponse(
    DateTimeOffset ServerTimeUtc,
    DeviceConnectionState State,
    string PolicyVersion,
    int NextHeartbeatSeconds = 30);

public sealed record ActivityBucketUpload(
    Guid DeviceId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    ActivityState State,
    DateTimeOffset? LastKeyboardInputAtUtc,
    DateTimeOffset? LastMouseInputAtUtc,
    int KeyboardEventCount,
    int MouseEventCount,
    double MouseDistancePixels);

public sealed record DeviceSummary(
    Guid DeviceId,
    string EmployeeName,
    DeviceConnectionState State,
    DateTimeOffset? LastHeartbeatUtc,
    string ApplicationVersion,
    int OpenAlertCount,
    ActivityState CurrentActivityState);

public sealed record PolicySummary(
    string Version,
    int ScreenshotMinimumMinutes,
    int ScreenshotMaximumMinutes,
    int IdleThresholdMinutes,
    int ScreenshotRetentionDays,
    bool RemoteSupportEnabled);

public sealed record ApiError(string Code, string Message);
