$dotnetPath = "dotnet"
if (-not (Get-Command $dotnetPath -ErrorAction SilentlyContinue)) {
    $dotnetPath = "C:\Users\Neuron\dotnet\dotnet.exe"
}
if (-not (Test-Path $dotnetPath)) {
    Write-Error ".NET SDK (dotnet.exe) not found at: $dotnetPath"
    exit 1
}

$output = "C:\Users\Neuron\.gemini\antigravity\scratch\faceit-demo-manager\FaceitDemoManager.exe"

Write-Host "Compiling FaceitDemoManager WPF Application using .NET 8.0 SDK..." -ForegroundColor Cyan

# Clean previous build artifacts
if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path $output) { Remove-Item -Path $output -Force -ErrorAction SilentlyContinue }

# Run dotnet publish with SingleFile and ReadyToRun optimizations
& $dotnetPath publish FaceitDemoManager.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true

$publishExe = "bin\Release\net8.0-windows\win-x64\publish\FaceitDemoManager.exe"

if (Test-Path $publishExe) {
    Copy-Item -Path $publishExe -Destination $output -Force
    Write-Host "Compilation successful!" -ForegroundColor Green
    Write-Host "Output file: $output" -ForegroundColor Green
} else {
    Write-Error "Compilation failed: Executable not found at $publishExe"
    exit 1
}
