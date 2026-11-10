$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$validationDir = Join-Path $env:TEMP ('NeoSurvive-RunTraitTests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $validationDir | Out-Null
$unityData = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data'
$referencePack = Get-ChildItem 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref' -Directory |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$packVersion = [version]$referencePack.Name
$framework = 'net' + $packVersion.Major + '.' + $packVersion.Minor
$references = Get-ChildItem (Join-Path $referencePack.FullName ('ref\' + $framework)) -Filter '*.dll' |
    ForEach-Object { '-r:' + $_.FullName }
$sources = @(
    'RunTraitEffect', 'DamageBonusEffect', 'RunTraitDefinition', 'RunTraitCatalog',
    'RunTraitManager', 'RunTraitTier', 'RunTraitTierChances', 'RunTraitStat', 'WeaponStatModifierEffect',
    'RunTraitHitSettings', 'OnHitStatusEffect', 'RunTraitWeaponStats', 'RunTraitSlowReceiver',
    'SpecialTraitEffect', 'RunTraitDamageContext', 'RunTraitShield', 'RunTraitStaggerReceiver',
    'RunTraitChainState', 'RunTraitControl', 'RunTraitAmplificationReceiver', 'RunTraitTarget', 'RunTraitBossControl', 'RunTraitStatus',
    'RunTraitCloneLifetime'
) | ForEach-Object { Join-Path $repoRoot ('Assets\Scripts\RunTrait\' + $_ + '.cs') }
$sources += @(
    (Join-Path $repoRoot 'Assets\Editor\RunTrait\RunTraitDebugActions.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\CharacterType.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Player\Weapon\WeaponBase.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Player\Weapon\WeaponSource.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Player\Weapon\SynergyTag.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Stats\Stat.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Character.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Enemy\Boss\Boss.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Enemy\Boss\Patterns\BossPatternBase.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Buff\StatusFlags.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Buff\IBuff.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Buff\BuffHandler.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Buff\Debuff\StunDebuff.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Buff\Debuff\RootDebuff.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Characters\Buff\Debuff\FrenzyDebuff.cs'),
    (Join-Path $PSScriptRoot 'UnityApiShim.cs'), (Join-Path $PSScriptRoot 'Program.cs')
)
$outputDll = Join-Path $validationDir 'RunTraitTests.dll'
Push-Location $repoRoot
try {
    & (Join-Path $unityData 'NetCoreRuntime\dotnet.exe') (Join-Path $unityData 'DotNetSdkRoslyn\csc.dll') `
        -nologo -noconfig -nostdlib -target:exe ('-out:' + $outputDll) @references @sources
    if ($LASTEXITCODE -ne 0) { throw "Test compilation failed: $LASTEXITCODE" }
    $runtimeConfig = @{ runtimeOptions = @{ tfm = $framework; framework = @{
        name = 'Microsoft.NETCore.App'; version = ($packVersion.Major.ToString() + '.' + $packVersion.Minor + '.0')
    } } } | ConvertTo-Json -Depth 5
    [IO.File]::WriteAllText((Join-Path $validationDir 'RunTraitTests.runtimeconfig.json'), $runtimeConfig)
    & dotnet $outputDll
    if ($LASTEXITCODE -ne 0) { throw "Tests failed: $LASTEXITCODE" }
} finally { Pop-Location }
