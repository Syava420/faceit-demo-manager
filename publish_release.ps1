$token = $env:GITHUB_TOKEN
if (-not $token) {
    try {
        $gitPath = "C:\Users\Neuron\.gemini\antigravity\scratch\git\cmd\git.exe"
        if (-not (Test-Path $gitPath)) {
            $gitPath = "git"
        }
        $remoteUrl = & $gitPath remote get-url origin 2>$null
        if ($remoteUrl -match 'https://([^@]+)@github\.com') {
            $token = $Matches[1]
        }
    } catch {}
}
if (-not $token) {
    Write-Error "GitHub token not found! Please set GITHUB_TOKEN environment variable or configure git remote credentials."
    exit 1
}
$owner = "Syava420"
$repo = "faceit-demo-manager"
$tag = "v1.5.0"
$releaseName = "FACEIT Demo Hub v1.5.0"
$body = @"
### Релиз FACEIT Demo Hub v1.5.0

**Основные изменения:**
* Добавлен глобальный темный стиль для скроллбаров (с прозрачным треком и закругленными серыми ползунками) для более современного и цельного вида.
* Исправлены ошибки интерфейса и решена проблема с аварийным завершением приложения при удалении элементов.
* Улучшен парсинг статистики ADR и обработка извлеченных файлов демок (.dem).
* Исправлена кодировка и логирование.

**Файлы для загрузки:**
* **Setup.exe** — удобный установщик приложения со встроенными зависимостями (включая zstd).
* **FaceitDemoManager.exe** — портативная версия исполняемого файла (требуется наличие zstd.exe в той же папке).
"@

Write-Host "Checking if release $tag already exists via curl..." -ForegroundColor Cyan

$existingUri = "https://api.github.com/repos/$owner/$repo/releases/tags/$tag"
$checkJson = curl.exe --ssl-no-revoke -s -H "Authorization: token $token" -H "Accept: application/vnd.github.v3+json" $existingUri

$releaseResponse = $null
if ($checkJson) {
    try {
        $releaseResponse = $checkJson | ConvertFrom-Json
    } catch {}
}

if ($releaseResponse -and $releaseResponse.id) {
    Write-Host "Found existing release at: $($releaseResponse.html_url)" -ForegroundColor Green
} else {
    Write-Host "Release not found. Creating new release for $tag..." -ForegroundColor Cyan
    
    $releaseBody = @{
        tag_name = $tag
        target_commitish = "main"
        name = $releaseName
        body = $body
        draft = $false
        prerelease = $false
    } | ConvertTo-Json -Compress
    
    $createUri = "https://api.github.com/repos/$owner/$repo/releases"
    # Write request body temporarily to file because passing long JSON containing quotes via command line arguments to curl on Windows is error-prone.
    $tempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($tempFile, $releaseBody, [System.Text.Encoding]::UTF8)
    
    $createJson = curl.exe --ssl-no-revoke -s -X POST -H "Authorization: token $token" -H "Accept: application/vnd.github.v3+json" -H "Content-Type: application/json; charset=utf-8" -d "@$tempFile" $createUri
    
    Remove-Item $tempFile -ErrorAction SilentlyContinue
    
    if ($createJson) {
        try {
            $releaseResponse = $createJson | ConvertFrom-Json
        } catch {}
    }
    
    if ($releaseResponse -and $releaseResponse.id) {
        Write-Host "Release created successfully: $($releaseResponse.html_url)" -ForegroundColor Green
    } else {
        Write-Error "Failed to create release: $createJson"
        exit 1
    }
}

$releaseId = $releaseResponse.id
$uploadUrlTemplate = $releaseResponse.upload_url
$uploadUrlBase = $uploadUrlTemplate -replace '\{.*?\}', ''

# Function to upload asset using curl.exe --ssl-no-revoke
function Upload-Asset($filePath, $fileName) {
    if (-not (Test-Path $filePath)) {
        Write-Error "File not found: $filePath"
        return
    }
    
    $assetUri = "$uploadUrlBase?name=$fileName"
    Write-Host "Uploading $fileName..." -ForegroundColor Cyan
    
    # Run curl.exe with ssl-no-revoke
    $curlCmd = "curl.exe"
    $curlArgs = @(
        "--ssl-no-revoke",
        "-s",
        "-X", "POST",
        "-H", "Authorization: token $token",
        "-H", "Content-Type: application/octet-stream",
        "--data-binary", "@$filePath",
        $assetUri
    )
    
    $uploadResult = & $curlCmd $curlArgs
    Write-Host "Finished upload attempt for $fileName." -ForegroundColor Green
    if ($uploadResult) {
        Write-Host "Upload result detail: $uploadResult" -ForegroundColor Gray
    }
}

# Upload assets
Upload-Asset "C:\Users\Neuron\.gemini\antigravity\scratch\faceit-demo-manager\Setup.exe" "Setup.exe"
Upload-Asset "C:\Users\Neuron\.gemini\antigravity\scratch\faceit-demo-manager\FaceitDemoManager.exe" "FaceitDemoManager.exe"
