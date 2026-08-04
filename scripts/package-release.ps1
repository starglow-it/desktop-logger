param(
    [Parameter(Mandatory = $false)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '0.1.0'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $repositoryRoot 'artifacts'
$publishRoot = Join-Path $artifacts 'publish'
$releaseRoot = Join-Path $artifacts 'release'

if (Test-Path $publishRoot) { Remove-Item $publishRoot -Recurse -Force }
if (Test-Path $releaseRoot) { Remove-Item $releaseRoot -Recurse -Force }
New-Item $publishRoot -ItemType Directory -Force | Out-Null
New-Item $releaseRoot -ItemType Directory -Force | Out-Null

$projects = @{
    'ManagerServer'  = 'src/TeamActivity.Manager.Server/TeamActivity.Manager.Server.csproj'
    'ManagerDesktop' = 'src/TeamActivity.Manager.Desktop/TeamActivity.Manager.Desktop.csproj'
    'AgentService'   = 'src/TeamActivity.Agent.Service/TeamActivity.Agent.Service.csproj'
    'AgentDesktop'   = 'src/TeamActivity.Agent.Desktop/TeamActivity.Agent.Desktop.csproj'
}

foreach ($entry in $projects.GetEnumerator()) {
    $output = Join-Path $publishRoot $entry.Key
    dotnet publish (Join-Path $repositoryRoot $entry.Value) `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --output $output `
        -p:Version=$Version `
        -p:PublishReadyToRun=false
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for $($entry.Key)." }
}

Copy-Item (Join-Path $repositoryRoot 'docs') (Join-Path $publishRoot 'docs') -Recurse
Copy-Item (Join-Path $repositoryRoot 'README.md') (Join-Path $publishRoot 'README.md')

$zipPath = Join-Path $releaseRoot "TeamActivity-$Version-win-x64.zip"
Compress-Archive -Path (Join-Path $publishRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal
$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $zipPath -Leaf)" | Set-Content "$zipPath.sha256" -Encoding ascii
Write-Host "Created $zipPath"
Write-Host "SHA-256: $hash"
