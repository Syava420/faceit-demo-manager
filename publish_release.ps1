# Attempt to resolve GitHub Token dynamically from git remote first
$token = $null
$gitPath = Join-Path $PSScriptRoot "..\git\cmd\git.exe"
if (-not (Test-Path $gitPath)) {
    $gitPath = "git.exe"
}

$remoteUrlRaw = & $gitPath remote get-url origin 2>$null
if ($remoteUrlRaw) {
    $remoteUrl = $remoteUrlRaw.Trim()
    if ($remoteUrl -match 'https://([^@]+)@github\.com') {
        $credentials = $Matches[1]
        if ($credentials -match ':') {
            $token = $credentials.Split(':')[1]
        } else {
            $token = $credentials
        }
    }
}

# Fallback to environment variable if git remote didn't have it
if (-not $token) {
    $token = $env:GITHUB_TOKEN
}

if (-not $token) {
    Write-Error "GitHub Token not found. Please check your git remote URL or set GITHUB_TOKEN environment variable."
    exit 1
}

$owner = "Syava420"
$repo = "faceit-demo-manager"
$tag = "v1.7.0"
$releaseName = "FACEIT Demo Hub v1.7.0"
$body = @"
### Релиз FACEIT Demo Hub v1.7.0

**Основные изменения:**
* **Назначение никнеймов папкам:**
  * Добавлена возможность задавать кастомный никнейм игрока для любой папки в библиотеке по правому клику (Никнейм игрока).
  * Настроен механизм наследования: если у подпапки не задан собственный никнейм, она автоматически наследует никнейм родительской папки (при импорте и авто-импорте).
  * Добавлены аккуратные фиолетовые плашки с никнеймами у категорий в боковом меню.
* **Кастомные горячие клавиши (Бинды):**
  * Полностью реализован веб-интерфейс для управления биндами: включение/выключение, добавление новых действий, редактирование клавиш и команд CS2, сброс к значениям по умолчанию и удаление.
  * Исправлен баг, при котором бинды не сохранялись из JS-интерфейса в настройки C#.
* **Детали матча в Библиотеке:**
  * Реализована новая правая боковая панель «Детали матча» при выборе конкретной демки.
  * Панель позволяет видеть и изменять Карту, Счет, K/D, Дату добавления и Заметку о матче.
  * Изменения автоматически сохраняются по мере ввода текста!
* **Исправление ошибок:**
  * Исправлено самопроизвольное закрытие WPF диалогов ввода и выбора директорий с возвратом пустого/негативного результата.
  * Исправлен баг с авто-сбросом выбранной целевой папки импорта в выпадающем списке на вкладке «Импорт».

**Файлы для загрузки:**
* **Setup.exe** — установщик приложения со всеми встроенными зависимостями.
* **FACEIT Demo Hub.exe** — портативная версия исполняемого файла.
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
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($tempFile, $releaseBody, $utf8NoBom)
    
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
Upload-Asset (Join-Path $PSScriptRoot "FaceitDemoManager.exe") "FACEIT Demo Hub.exe"
