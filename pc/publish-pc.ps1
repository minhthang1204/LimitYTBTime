# Đóng gói Service + Agent + CLI thành một thư mục tự chứa (không cần cài .NET trên máy đích).
#   .\pc\publish-pc.ps1               → out\pc
param([string]$Output = (Join-Path $PSScriptRoot '..\out\pc'))

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\tools\dev-env.ps1')

$pc = $PSScriptRoot
dotnet test (Join-Path $pc 'LimitYT.Core.Tests') -v q
if ($LASTEXITCODE) { throw 'Test thất bại, dừng đóng gói.' }

if (Test-Path $Output) { Remove-Item -Recurse -Force $Output }
foreach ($project in 'LimitYT.Service', 'LimitYT.Agent', 'LimitYT.Cli') {
    dotnet publish (Join-Path $pc $project) -c Release -r win-x64 --self-contained true -p:DebugType=none -o $Output -v q
    if ($LASTEXITCODE) { throw "Đóng gói $project thất bại." }
}
Copy-Item (Join-Path $PSScriptRoot 'install\*.ps1') $Output

$size = (Get-ChildItem $Output -Recurse | Measure-Object Length -Sum).Sum / 1MB
Write-Host ("Xong: {0} ({1:N0} MB)" -f (Resolve-Path $Output), $size)
