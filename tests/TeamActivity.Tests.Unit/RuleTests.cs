using FluentAssertions;
using TeamActivity.Contracts;
using TeamActivity.Domain;
using TeamActivity.Infrastructure;
using TeamActivity.RemoteSupport;

namespace TeamActivity.Tests.Unit;

public sealed class ScreenshotIntervalTests
{
    [Fact]
    public void New_interval_is_always_within_inclusive_policy_boundaries()
    {
        var random = new Random(12345);
        var samples = Enumerable.Range(0, 2_000)
            .Select(_ => ScreenshotIntervalScheduler.Next(5, 10, random))
            .ToArray();

        samples.Should().OnlyContain(x => x >= TimeSpan.FromMinutes(5) && x <= TimeSpan.FromMinutes(10));
        samples.Distinct().Count().Should().BeGreaterThan(100);
    }

    [Fact]
    public void Fixed_interval_is_supported()
    {
        ScreenshotIntervalScheduler.Next(7, 7, new Random(1)).Should().Be(TimeSpan.FromMinutes(7));
    }
}

public sealed class ScreenshotGroupingTests
{
    [Fact]
    public void Multiple_monitors_with_nearby_timestamps_form_one_set()
    {
        var captured = DateTimeOffset.Parse("2026-08-04T12:00:00Z");
        var monitors = new[]
        {
            new MonitorCaptureMetadata(1, 1920, 1080, 1, 0, captured),
            new MonitorCaptureMetadata(2, 2560, 1440, 1.25, 90, captured.AddMilliseconds(180))
        };

        var act = () => ScreenshotSetRules.Validate(monitors, TimeSpan.FromSeconds(1));
        act.Should().NotThrow();
    }

    [Fact]
    public void Monitor_removal_is_valid_in_the_next_independent_set()
    {
        var captured = DateTimeOffset.Parse("2026-08-04T12:00:00Z");
        var beforeDockRemoval = new[]
        {
            new MonitorCaptureMetadata(1, 1920, 1080, 1, 0, captured),
            new MonitorCaptureMetadata(2, 1920, 1080, 1, 0, captured)
        };
        var afterDockRemoval = new[]
        {
            new MonitorCaptureMetadata(1, 1920, 1080, 1, 0, captured.AddMinutes(5))
        };

        ScreenshotSetRules.Validate(beforeDockRemoval, TimeSpan.Zero);
        ScreenshotSetRules.Validate(afterDockRemoval, TimeSpan.Zero);
    }
}

public sealed class ActivityStateTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-08-04T12:00:00Z");

    [Fact]
    public void Idle_threshold_period_is_not_retroactively_counted_as_active()
    {
        var result = ActivityStateCalculator.Classify(
            Start.AddMinutes(1),
            Start.AddMinutes(2),
            Start.AddMinutes(5),
            Start,
            TimeSpan.FromMinutes(5),
            withinSchedule: true,
            isLocked: false,
            isSignedIn: true,
            isApprovedBreak: false);

        result.Should().Be(ActivityKind.Idle);
    }

    [Fact]
    public void Just_before_threshold_remains_active()
    {
        var result = ActivityStateCalculator.Classify(
            Start.AddMinutes(1),
            Start.AddMinutes(2),
            Start.AddMinutes(5).AddTicks(-1),
            Start,
            TimeSpan.FromMinutes(5),
            true, false, true, false);

        result.Should().Be(ActivityKind.Active);
    }

    [Theory]
    [InlineData(true, true, false, ActivityKind.Locked)]
    [InlineData(false, true, false, ActivityKind.SignedOut)]
    [InlineData(true, false, true, ActivityKind.ApprovedBreak)]
    public void Explicit_session_states_take_precedence(
        bool signedIn,
        bool locked,
        bool approvedBreak,
        ActivityKind expected)
    {
        ActivityStateCalculator.Classify(
            Start, Start.AddMinutes(1), Start.AddMinutes(1), Start,
            TimeSpan.FromMinutes(5), true, locked, signedIn, approvedBreak).Should().Be(expected);
    }

    [Fact]
    public void Incomplete_telemetry_is_unknown_not_active()
    {
        ActivityStateCalculator.Classify(
            Start, Start.AddMinutes(1), Start.AddMinutes(1), Start,
            TimeSpan.FromMinutes(5), true, false, true, false, telemetryComplete: false)
            .Should().Be(ActivityKind.Unknown);
    }
}

public sealed class ScheduleTests
{
    [Fact]
    public void End_boundary_is_exclusive()
    {
        var utc = TimeZoneInfo.Utc;
        WorkScheduleEvaluator.IsWithinSchedule(
            DateTimeOffset.Parse("2026-08-04T17:00:00Z"),
            DayOfWeek.Tuesday,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            utc).Should().BeFalse();
    }

    [Fact]
    public void Both_repeated_fall_back_hours_are_evaluated_using_their_real_offsets()
    {
        var eastern = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");
        var firstOccurrence = DateTimeOffset.Parse("2026-11-01T05:30:00Z");
        var secondOccurrence = DateTimeOffset.Parse("2026-11-01T06:30:00Z");

        WorkScheduleEvaluator.IsWithinSchedule(firstOccurrence, DayOfWeek.Sunday,
            new TimeOnly(1, 0), new TimeOnly(2, 0), eastern).Should().BeTrue();
        WorkScheduleEvaluator.IsWithinSchedule(secondOccurrence, DayOfWeek.Sunday,
            new TimeOnly(1, 0), new TimeOnly(2, 0), eastern).Should().BeTrue();
    }
}

public sealed class HeartbeatTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-04T12:00:00Z");

    [Theory]
    [InlineData(59, DeviceState.Online)]
    [InlineData(60, DeviceState.Degraded)]
    [InlineData(89, DeviceState.Degraded)]
    [InlineData(90, DeviceState.Offline)]
    public void Two_missed_heartbeats_degrade_and_three_mark_offline(int elapsedSeconds, DeviceState expected)
    {
        HeartbeatStatusEvaluator.Evaluate(Now, Now.AddSeconds(-elapsedSeconds), TimeSpan.FromSeconds(30))
            .Should().Be(expected);
    }

    [Fact]
    public void A_new_heartbeat_recovers_the_device()
    {
        HeartbeatStatusEvaluator.Evaluate(Now, Now, TimeSpan.FromSeconds(30)).Should().Be(DeviceState.Online);
    }
}

public sealed class PrivacyContractTests
{
    [Fact]
    public void Activity_contract_has_no_raw_key_or_mouse_button_field()
    {
        var prohibited = new[] { "Key", "KeyCode", "KeyValue", "MouseButton", "Button" };
        var propertyNames = typeof(ActivityBucketUpload).GetProperties().Select(x => x.Name);

        propertyNames.Should().NotIntersectWith(prohibited);
    }
}

public sealed class RemoteSupportConsentTests
{
    [Fact]
    public void Control_cannot_be_added_when_only_view_was_requested()
    {
        var session = CreateSession(SupportCapability.View);
        var act = () => session.Accept(SupportCapability.View | SupportCapability.Control);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Denial_never_starts_a_session()
    {
        var session = CreateSession(SupportCapability.View);
        session.Deny();
        session.Status.Should().Be(RemoteSupportStatus.Denied);
        session.StartedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Revocation_removes_all_capabilities_immediately()
    {
        var session = CreateSession(SupportCapability.View | SupportCapability.Control);
        session.Accept(SupportCapability.View | SupportCapability.Control);
        session.Revoke();
        session.Status.Should().Be(RemoteSupportStatus.Revoked);
        session.GrantedCapabilities.Should().Be(SupportCapability.None);
    }

    [Fact]
    public void Session_expires_using_a_controllable_clock()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-04T12:00:00Z"));
        var session = new ConsentSession(Guid.NewGuid(), "Verified Manager", "Support request",
            SupportCapability.View, TimeSpan.FromMinutes(30), clock);
        session.Accept(SupportCapability.View);
        clock.UtcNow = clock.UtcNow.AddMinutes(30);

        session.ExpireIfNeeded().Should().BeTrue();
        session.Status.Should().Be(RemoteSupportStatus.Expired);
    }

    private static ConsentSession CreateSession(SupportCapability capabilities) =>
        new(Guid.NewGuid(), "Verified Manager", "Troubleshoot the company application", capabilities,
            TimeSpan.FromMinutes(30), new FakeClock(DateTimeOffset.Parse("2026-08-04T12:00:00Z")));
}

public sealed class AuditTests
{
    [Fact]
    public void Changing_an_audited_value_breaks_the_hash()
    {
        var item = new AuditEvent
        {
            Id = Guid.Parse("3c89a0f1-2c08-4a15-902a-cab9f1e89e4b"),
            CreatedAtUtc = DateTimeOffset.Parse("2026-08-04T12:00:00Z"),
            Actor = "manager",
            Action = "ScreenshotViewed",
            SubjectType = "ScreenshotSet",
            SubjectId = "set-1",
            DetailsJson = "{}",
            PreviousHash = "previous"
        };
        item.Hash = AuditChain.CalculateHash(item);
        AuditChain.IsValid(item).Should().BeTrue();

        item.SubjectId = "set-2";
        AuditChain.IsValid(item).Should().BeFalse();
    }
}

public sealed class EncryptedFileStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"teamactivity-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Stored_bytes_are_encrypted_and_round_trip()
    {
        var store = new EncryptedFileStore(root, Enumerable.Range(0, 32).Select(x => (byte)x).ToArray());
        var plaintext = "screenshot bytes"u8.ToArray();
        var stored = await store.StoreAsync(plaintext, new DateOnly(2026, 8, 4));

        var diskBytes = await File.ReadAllBytesAsync(Path.Combine(root, stored.RelativePath));
        diskBytes.Should().NotContainInOrder(plaintext);
        (await store.ReadAsync(stored.RelativePath)).Should().Equal(plaintext);
    }

    [Fact]
    public async Task Traversal_outside_data_root_is_rejected()
    {
        var store = new EncryptedFileStore(root, new byte[32]);
        var act = async () => await store.ReadAsync(Path.Combine("..", "outside.tas"));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}

internal sealed class FakeClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}
