param([ValidateSet('stable','beta')][string]$Branch = 'stable')
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source = 'C:/steamcmd/steamapps/content/app_2868840/depot_2868841'
$installed = 'C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2'
if ($Branch -eq 'beta') { $source = $installed }
$target = Join-Path $repo ".build/runtime-$Branch"
if (Test-Path $target) {
    if ((Get-ChildItem -LiteralPath $target -Force | Measure-Object).Count -ne 0) { throw "Runtime already exists: $target" }
} else { New-Item -ItemType Directory -Path $target | Out-Null }
$files = @(Get-ChildItem -LiteralPath $source -File) + @(Get-ChildItem -LiteralPath "$source/data_sts2_windows_x86_64" -Recurse -File)
foreach ($file in $files) {
    if ($file.Name -in @('steam_api64.dll','override.cfg','audit.tscn','HKAuditRunner.cs')) { continue }
    $relative = $file.FullName.Substring($source.Length + 1)
    $dest = Join-Path $target $relative
    New-Item -ItemType Directory -Force (Split-Path $dest -Parent) | Out-Null
    New-Item -ItemType HardLink -Path $dest -Target $file.FullName | Out-Null
}
@'
[application]
run/main_scene="res://audit.tscn"
config/use_custom_user_dir=true
config/custom_user_dir_name="HollowKnightAuditBRANCH"
[dotnet]
project/assembly_name="HKAudit"
'@.Replace('BRANCH', $Branch) | Set-Content (Join-Path $target 'override.cfg') -Encoding utf8
foreach ($name in @('BaseLib','HollowKnight')) {
    $dest = Join-Path $target "mods/$name"
    New-Item -ItemType Directory -Force $dest | Out-Null
    if ($name -eq 'BaseLib') { $from = "$installed/mods/BaseLib" }
    else { $from = Join-Path $repo '.build/mods/HollowKnight' }
    Copy-Item "$from/*" $dest
}
Copy-Item -LiteralPath "$PSScriptRoot/HKAuditRunner.cs" -Destination "$target/HKAuditRunner.cs"
@'
[gd_scene load_steps=2 format=3]
[ext_resource type="Script" path="res://HKAuditRunner.cs" id="1"]
[node name="HKAuditRunner" type="Node"]
script=ExtResource("1")
'@ | Set-Content "$target/audit.tscn" -Encoding utf8
New-Item -ItemType Junction -Path "$target/data_HKAudit_windows_x86_64" -Target "$target/data_sts2_windows_x86_64" | Out-Null
Write-Output $target
