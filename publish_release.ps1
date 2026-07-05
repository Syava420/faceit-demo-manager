# Attempt to resolve GitHub Token dynamically
$token = $env:GITHUB_TOKEN
if (-not $token) {
    # Find relative git executable path
    $gitPath = Join-Path $PSScriptRoot "..\git\cmd\git.exe"
    if (-not (Test-Path $gitPath)) {
        $gitPath = "git.exe"
    }
    
    $remoteUrl = & $gitPath remote get-url origin 2>$null
    if ($remoteUrl -match 'https://([^@]+)@github\.com') {
        $credentials = $Matches[1]
        if ($credentials -match ':') {
            $token = $credentials.Split(':')[1]
        } else {
            $token = $credentials
        }
    }
}

if (-not $token) {
    Write-Error "GitHub Token not found. Please set GITHUB_TOKEN environment variable or check your git remote URL."
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

$headers = @{
    "Authorization" = "token $token"
    "Accept"        = "application/vnd.github.v3+json"
}

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
    
    # Check if asset already exists in release
    $existingAsset = $releaseResponse.assets | Where-Object { $_.name -eq $fileName }
    if ($existingAsset) {
        Write-Host "Asset $fileName already exists. Deleting existing asset before re-upload..." -ForegroundColor Yellow
        $deleteUri = "https://api.github.com/repos/$owner/$repo/releases/assets/$($existingAsset.id)"
        curl.exe --ssl-no-revoke -s -X DELETE -H "Authorization: token $token" -H "Accept: application/vnd.github.v3+json" $deleteUri
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
}

# Upload assets
Upload-Asset (Join-Path $PSScriptRoot "Setup.exe") "Setup.exe"
Upload-Asset (Join-Path $PSScriptRoot "FaceitDemoManager.exe") "FaceitDemoManager.exe"
