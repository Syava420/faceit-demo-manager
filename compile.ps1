$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscPath)) {
    Write-Error "C# Compiler (csc.exe) not found at: $cscPath"
    exit 1
}

$output = "C:\Users\Neuron\.gemini\antigravity\scratch\faceit-demo-manager\FaceitDemoManager.exe"

Write-Host "Compiling FaceitDemoManager Modular WPF Application..." -ForegroundColor Cyan

# Compile all .cs files in the directory
& $cscPath /target:winexe /lib:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319","C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF" /r:PresentationFramework.dll /r:PresentationCore.dll /r:WindowsBase.dll /r:System.Xaml.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:$output *.cs

if (Test-Path $output) {
    Write-Host "Compilation successful!" -ForegroundColor Green
    Write-Host "Output file: $output" -ForegroundColor Green
} else {
    Write-Error "Compilation failed."
}
