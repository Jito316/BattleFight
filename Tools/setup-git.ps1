# BattleFight: リポジトリを取ってきたら一度実行する(このリポジトリだけの設定。ほかのリポジトリには影響しない)
#   1. Git LFS を有効にする(画像・モデル・音・フォントを LFS で扱う)
#   2. Unity のマージツール(UnityYAMLMerge)を、シーン・プレハブ・アセットのマージに使う
# 使い方: リポジトリのいちばん上で  powershell -ExecutionPolicy Bypass -File Tools/setup-git.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# Unity のバージョンは ProjectVersion.txt から読む
$versionLine = Get-Content "UnityProject/ProjectSettings/ProjectVersion.txt" | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
$version = ($versionLine -split ":")[1].Trim()

# Unity Hub の標準の場所から探す。見つからなければ UNITY_EDITOR_PATH(Unity.exe の場所)を見る
$candidates = @(
    "C:\Program Files\Unity\Hub\Editor\$version\Editor\Data\Tools\UnityYAMLMerge.exe",
    "/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/Tools/UnityYAMLMerge"
)
if ($env:UNITY_EDITOR_PATH) {
    $candidates += (Join-Path (Split-Path -Parent $env:UNITY_EDITOR_PATH) "Data\Tools\UnityYAMLMerge.exe")
}
$merge = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

Write-Host "Unity $version"
git lfs install --local | Out-Null
Write-Host "Git LFS: 有効にしました"

if ($merge) {
    $mergePath = $merge -replace "\\", "/"
    git config --local merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
    # Windows PowerShell 5.1 は、外のコマンドに渡す引数の中の " を消すので、\" で渡す
    $driver = '\"' + $mergePath + '\" merge -p %O %B %A %A'
    if ($PSVersionTable.PSVersion.Major -ge 7) { $driver = '"' + $mergePath + '" merge -p %O %B %A %A' }
    git config --local merge.unityyamlmerge.driver $driver
    git config --local merge.unityyamlmerge.recursive binary
    Write-Host "UnityYAMLMerge: $mergePath を使います"
} else {
    Write-Warning "UnityYAMLMerge が見つかりません。Unity $version を Unity Hub で入れるか、UNITY_EDITOR_PATH に Unity.exe の場所を入れてから、もう一度実行してください"
}
