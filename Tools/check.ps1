# BattleFight: 手元でまとめて確かめる(データとプレハブの更新 → EditMode テスト → PlayMode テスト → WebGL ビルド)
# 使い方(リポジトリのいちばん上で):
#   powershell -ExecutionPolicy Bypass -File Tools/check.ps1                 # ふだん(シーンには手を付けない)
#   powershell -ExecutionPolicy Bypass -File Tools/check.ps1 -RebuildScene   # シーンもプレハブから作り直す
#   powershell -ExecutionPolicy Bypass -File Tools/check.ps1 -SkipWebGL      # ビルドを飛ばす
# 結果は TestResults/ に出る(status.txt に進み具合、*.xml にテスト結果、*.log に Unity のログ)。
# どれかが失敗すると終了コード 1。Unity は ProjectVersion.txt のバージョンを Unity Hub の標準の場所から探す
# (別の場所なら -Unity か、環境変数 UNITY_EDITOR_PATH に Unity.exe の場所を入れる)。

param(
    [switch]$RebuildScene,
    [switch]$SkipData,
    [switch]$SkipTests,
    [switch]$SkipWebGL,
    [string]$Unity = $env:UNITY_EDITOR_PATH,
    [string]$OutDir
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "UnityProject"
if (-not $OutDir) { $OutDir = Join-Path $root "TestResults" }
New-Item -ItemType Directory -Force $OutDir | Out-Null

if (-not $Unity) {
    $versionLine = Get-Content (Join-Path $project "ProjectSettings/ProjectVersion.txt") | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
    $version = ($versionLine -split ":")[1].Trim()
    $Unity = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
}
if (-not (Test-Path $Unity)) { throw "Unity が見つかりません: $Unity" }

$status = Join-Path $OutDir "status.txt"
"started" | Out-File $status -Encoding utf8
$failed = $false

function Invoke-Unity([string[]]$UnityArgs, [string]$Name) {
    $process = Start-Process -FilePath $Unity -ArgumentList $UnityArgs -PassThru -WindowStyle Hidden
    $process.WaitForExit()
    "$Name=$($process.ExitCode)" | Out-File $status -Append -Encoding utf8
    # Unity が終わったあと、プロジェクトのロックが外れるまで少し待つ
    Start-Sleep -Seconds 3
    return $process.ExitCode
}

# テスト結果の数を数える(失敗・通過・全部)
function Get-TestSummary([string]$Path) {
    if (-not (Test-Path $Path)) { return "結果なし" }
    [xml]$xml = Get-Content $Path -Encoding UTF8
    $run = $xml.'test-run'
    return "$($run.passed)/$($run.total) 通過(失敗 $($run.failed))"
}

if (-not $SkipData) {
    $method = if ($RebuildScene) { "RebuildSceneFromCommandLine" } else { "BuildFromCommandLine" }
    $code = Invoke-Unity @("-batchmode", "-nographics", "-quit", "-projectPath", $project,
        "-executeMethod", "BattleFight.EditorTools.PrototypeSceneBuilder.$method", "-logFile", (Join-Path $OutDir "data.log")) "data"
    if ($code -ne 0) {
        Write-Host "データ・プレハブの更新か、シーンの確かめで問題がありました(TestResults/data.log)"
        "done" | Out-File $status -Append -Encoding utf8
        exit 1
    }
}

if (-not $SkipTests) {
    foreach ($platform in @("EditMode", "PlayMode")) {
        $name = $platform.ToLower()
        $code = Invoke-Unity @("-batchmode", "-projectPath", $project, "-runTests", "-testPlatform", $platform,
            "-testResults", (Join-Path $OutDir "$name.xml"), "-logFile", (Join-Path $OutDir "$name.log")) $name
        Write-Host "$platform テスト: $(Get-TestSummary (Join-Path $OutDir "$name.xml"))"
        if ($code -ne 0) { $failed = $true }
    }
}

if (-not $SkipWebGL) {
    $code = Invoke-Unity @("-batchmode", "-nographics", "-quit", "-projectPath", $project, "-buildTarget", "WebGL",
        "-executeMethod", "BattleFight.EditorTools.WebGLBuilder.BuildFromCommandLine", "-logFile", (Join-Path $OutDir "webgl.log")) "webgl"
    Write-Host ("WebGL ビルド: " + $(if ($code -eq 0) { "成功" } else { "失敗(TestResults/webgl.log)" }))
    if ($code -ne 0) { $failed = $true }
}

"done" | Out-File $status -Append -Encoding utf8
if ($failed) { exit 1 }
