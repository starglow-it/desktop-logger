namespace TeamActivity.Tests.EndToEnd;

public sealed class WindowsLifecycleTests
{
    [Fact(Skip = "Requires the signed MSI, an isolated Windows 11 VM, and elevation; scheduled for the installer phase.")]
    public void Installer_upgrade_repair_and_uninstall_preserve_audit_data_by_policy()
    {
    }

    [Fact(Skip = "Live capture remains disabled until Windows capture-consent implementation and security review are complete.")]
    public void Secure_desktop_is_never_captured()
    {
    }
}
