param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
    [switch]$Baseline
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$runName = if ($Baseline) { 'Before' } else { 'After' }
$project = Join-Path $repo "Temp/AudioStartupValidation/$runName"
New-Item -ItemType Directory -Force -Path "$project/Assets/Editor", "$project/Assets/Data/Sounds/Ingame", "$project/Packages", "$project/ProjectSettings" | Out-Null
Set-Content -LiteralPath "$project/Packages/manifest.json" -Value '{"dependencies":{"com.unity.modules.audio":"1.0.0"}}' -Encoding utf8
Copy-Item -LiteralPath "$repo/ProjectSettings/ProjectVersion.txt" -Destination "$project/ProjectSettings/ProjectVersion.txt"
foreach ($name in @('SettingsManager', 'SoundManager')) {
    $source = if ($Baseline) { "$repo/Temp/AudioStartupValidation/Baseline/$name.cs" } elseif ($name -eq 'SettingsManager') { "$repo/Assets/Scripts/Managers/$name.cs" } else { "$repo/Assets/Scripts/Audio/$name.cs" }
    Copy-Item -LiteralPath $source -Destination "$project/Assets/$name.cs"
}
foreach ($name in @('GameSoundEventDefinition', 'WeaponSoundProfileDefinition')) {
    Copy-Item -LiteralPath "$repo/Assets/Scripts/Audio/$name.cs" -Destination "$project/Assets/$name.cs"
}
foreach ($name in @('ValidationDependencies', 'AudioStartupProbe', 'AudioVolumeAwakeProbe')) {
    Copy-Item -LiteralPath "$PSScriptRoot/$name.cs" -Destination "$project/Assets/$name.cs"
}
Copy-Item -LiteralPath "$PSScriptRoot/AudioStartupValidation.cs" -Destination "$project/Assets/Editor/AudioStartupValidation.cs"
Set-Content -LiteralPath "$project/scenario.txt" -Value 'saved' -Encoding ascii
$writer = [System.IO.BinaryWriter]::new([System.IO.File]::Create("$project/Assets/tone.wav"))
try {
    $data = [byte[]]::new(44100 * 2 * 2)
    $writer.Write([System.Text.Encoding]::ASCII.GetBytes('RIFF'))
    $writer.Write([int](36 + $data.Length))
    $writer.Write([System.Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
    $writer.Write([int]16)
    $writer.Write([short]1)
    $writer.Write([short]1)
    $writer.Write([int]44100)
    $writer.Write([int]88200)
    $writer.Write([short]2)
    $writer.Write([short]16)
    $writer.Write([System.Text.Encoding]::ASCII.GetBytes('data'))
    $writer.Write([int]$data.Length)
    $writer.Write($data)
} finally { $writer.Dispose() }
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"{0}"' -f $project), '-executeMethod', 'AudioStartupValidation.Run', '-logFile', ('"{0}"' -f "$project/Editor.log"))
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(180000)) {
    $process.Kill()
    throw "Validation Editor timed out. See $project/Editor.log"
}
if (Test-Path -LiteralPath "$project/results.txt") { Get-Content -LiteralPath "$project/results.txt" }
Write-Output "Editor log: $project/Editor.log"
exit $process.ExitCode
