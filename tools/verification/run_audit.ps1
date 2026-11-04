param([string]$Branch = 'stable', [switch]$VisualPreview, [switch]$CardUiPreview, [switch]$SoulCostDesignPreview, [switch]$CombatHudPreview, [switch]$SwarmPreview, [switch]$HornetStudyPreview, [switch]$CleanupPreview, [string]$ModSource)
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$auditDir = Join-Path $repo ".build/runtime-$Branch"
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq "$auditDir\SlayTheSpire2.exe" }) {
    throw 'The isolated audit game is still running; wait for it to finish before staging files.'
}
$data = Join-Path $auditDir 'data_sts2_windows_x86_64'
$output = Join-Path $PSScriptRoot '.godot/mono/temp/bin/Debug/HKAudit.dll'
Copy-Item -LiteralPath $output -Destination "$data/HKAudit.dll" -Force
Copy-Item -LiteralPath "$PSScriptRoot/HKAuditRunner.cs" -Destination "$auditDir/HKAuditRunner.cs" -Force
foreach ($name in @('HollowKnight', 'BaseLib')) {
    if ($name -eq 'HollowKnight') {
        $source = if ($ModSource) { $ModSource } else { "$repo/.build/mods/$name" }
        Copy-Item "$source/*" "$auditDir/mods/$name" -Force
    }
}
Copy-Item -LiteralPath "$data/sts2.runtimeconfig.json" -Destination "$data/HKAudit.runtimeconfig.json" -Force
$deps = Get-Content "$data/sts2.deps.json" -Raw | ConvertFrom-Json
$target = $deps.targets.'.NETCoreApp,Version=v9.0/win-x64'
$target | Add-Member -NotePropertyName 'HKAudit/1.0.0' -NotePropertyValue @{dependencies=@{'sts2'='0.1.0';'runtimepack.Microsoft.NETCore.App.Runtime.win-x64'='9.0.7'};runtime=@{'HKAudit.dll'=@{}}}
$deps.libraries | Add-Member -NotePropertyName 'HKAudit/1.0.0' -NotePropertyValue @{type='project';serviceable=$false;sha512=''}
$deps | ConvertTo-Json -Depth 50 | Set-Content "$data/HKAudit.deps.json" -Encoding UTF8
$stamp = Get-Date -Format 'yyyyMMdd-HHmmssfff'
foreach ($old in @('audit-results.json','bootstrap-error.txt')) {
    if (Test-Path "$auditDir/$old") {
        New-Item -ItemType Directory "$auditDir/history" -Force | Out-Null
        Move-Item -LiteralPath "$auditDir/$old" -Destination "$auditDir/history/$stamp-$old"
    }
}
$args = @('--max-fps','60','--path',$auditDir,'--log-file',"$auditDir/test.log",'--quit-after','2400')
if ($CleanupPreview) {
    $args += @('--windowed','--resolution','2010x900','--position','31000,31000','--rendering-method','gl_compatibility','--rendering-driver','opengl3','--','--knight-cleanup-preview')
} elseif ($HornetStudyPreview) {
    $args += @('--windowed','--resolution','1360x900','--position','31000,31000','--rendering-method','gl_compatibility','--rendering-driver','opengl3','--','--knight-hornet-study-preview')
} elseif ($SwarmPreview) {
    $args += @('--windowed','--resolution','1600x650','--position','31000,31000','--rendering-method','gl_compatibility','--rendering-driver','opengl3','--','--knight-swarm-preview')
} elseif ($CombatHudPreview) {
    $args += @('--windowed','--resolution','800x600','--position','31000,31000','--rendering-method','gl_compatibility','--rendering-driver','opengl3','--','--knight-combat-hud-preview')
} elseif ($SoulCostDesignPreview) {
    $args += @('--windowed','--resolution','480x620','--position','31000,31000','--rendering-method','gl_compatibility','--rendering-driver','opengl3','--','--knight-soul-cost-design-preview')
} elseif ($CardUiPreview) {
    $args += @('--windowed','--resolution','1440x940','--position','31000,31000','--rendering-method','gl_compatibility','--rendering-driver','opengl3','--','--knight-card-ui-preview')
} elseif ($VisualPreview) {
    $args += @('--windowed','--resolution','720x420','--position','31000,31000','--rendering-method','gl_compatibility','--rendering-driver','opengl3','--','--knight-visual-preview')
} else { $args = @('--headless') + $args }
$proc = Start-Process -FilePath "$auditDir/SlayTheSpire2.exe" -WorkingDirectory $auditDir -ArgumentList $args -WindowStyle Hidden -RedirectStandardOutput "$auditDir/stdout.log" -RedirectStandardError "$auditDir/stderr.log" -PassThru
Write-Output "Audit PID: $($proc.Id)"
