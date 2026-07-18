$url = "https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.2903.40/microsoft.web.webview2.1.0.2903.40.nupkg"
$zipPath = "C:\Users\Neuron\AppData\Local\Temp\webview2.zip"
$extractPath = "C:\Users\Neuron\.nuget\packages\microsoft.web.webview2\1.0.2903.40"

Write-Host "Downloading nupkg from $url..."
$wc = New-Object System.Net.WebClient
$wc.DownloadFile($url, $zipPath)
Write-Host "Downloaded nupkg size: $((Get-Item $zipPath).Length) bytes"

if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
New-Item -ItemType Directory -Path $extractPath -Force

Write-Host "Extracting nupkg archive..."
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $extractPath)
Remove-Item $zipPath -Force
Write-Host "Nuget package successfully extracted!"
