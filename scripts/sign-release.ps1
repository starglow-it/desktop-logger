param(
    [Parameter(Mandatory = $true)][string]$PublishRoot,
    [Parameter(Mandatory = $true)][string]$CertificatePath,
    [Parameter(Mandatory = $true)][string]$CertificatePassword,
    [Parameter(Mandatory = $false)][string]$TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'
$signTool = (Get-Command signtool.exe -ErrorAction Stop).Source
$files = Get-ChildItem $PublishRoot -Recurse -File | Where-Object { $_.Extension -in '.exe', '.dll', '.msi' }
if ($files.Count -eq 0) { throw 'No signable files were found.' }

foreach ($file in $files) {
    & $signTool sign /fd SHA256 /td SHA256 /tr $TimestampUrl /f $CertificatePath /p $CertificatePassword $file.FullName
    if ($LASTEXITCODE -ne 0) { throw "Signing failed for $($file.FullName)." }
    & $signTool verify /pa /all $file.FullName
    if ($LASTEXITCODE -ne 0) { throw "Signature verification failed for $($file.FullName)." }
}
