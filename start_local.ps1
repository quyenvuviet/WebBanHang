$ErrorActionPreference = "Stop"
Set-Location -LiteralPath $PSScriptRoot
& (Join-Path $PSScriptRoot "database\restore_localdb.ps1") -IfMissing
& dotnet restore .\src\BanHangcoiWeb\BanHangcoiWeb.csproj
if ($LASTEXITCODE -ne 0) { throw "Package restore failed." }
& dotnet run --project .\src\BanHangcoiWeb\BanHangcoiWeb.csproj --launch-profile http --urls http://localhost:5188
if ($LASTEXITCODE -ne 0) { throw "Website startup failed." }
