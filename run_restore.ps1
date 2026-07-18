$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:MSBUILDDISABLENODEREUSE = "1"
$env:NUGET_SHOW_STACK = "true"

Write-Host "Running dotnet restore with environment variables..."
& "C:\Users\Neuron\dotnet\dotnet.exe" restore FaceitDemoManager.csproj -p:NuGetInteractive=false -v m
