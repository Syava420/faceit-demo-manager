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
$tag = "v1.6.0"
$releaseName = "FACEIT Demo Hub v1.6.0"
$body = @"
### Релиз FACEIT Demo Hub v1.6.0

**Основные изменения:**
* **Переход на .NET 8.0:** Повышена общая производительность приложения, оптимизирована скорость запуска. Приложение публикуется как Self-Contained.
* **Новая премиум-иконка:** Разработан и интегрирован новый современный логотип приложения.
* **Улучшенный Установщик:** В `Setup.exe` интегрировано автоматическое разблокирование безопасности файлов и добавление приложения в исключения Windows Defender для предотвращения ложных срабатываний.
* **Глобальный мониторинг ошибок:** Добавлен обработчик Runtime-ошибок с детальным выводом в специальном окне `ErrorHandlerWindow`.
* **Функционал Drag and Drop:**
  * Реализована поддержка перетаскивания файлов прямо в рабочую область (DragDropZone).
  * Добавлена возможность перетаскивания папок для изменения их вложенности и создания подструктур.
* **Улучшения интерфейса (UI/UX):**
  * Плавные анимации затухания и появления окон (Fade-in/Fade-out) и плавные переходы при переключении вкладок.
  * Тонкие аккуратные скроллбары во всех списках.
  * Векторные кнопки управления окном (Свернуть/Закрыть) с чистыми эффектами наведения.
  * Кнопка очистки консоли логов перенесена под консоль и стилизована под элегантный Ghost.
* **Управление файлами демок:**
  * Добавлена опция сохранения/удаления архивов после распаковки демок.
  * Новая колонка «Дата добавления» с дефолтной хронологической сортировкой.
  * Динамические кнопки для фильтрации по картам.
* **Улучшенные горячие клавиши (Бинды):**
  * Добавлена страница кастомных биндов (создание и удаление биндов).
  * Реализован бинд по умолчанию для 4-кратного ускорения просмотра демо.
  * Добавлен дефолтный бинд для переключения голосового чата игроков в GOTV CS2.
  * По правому клику на папки теперь доступно контекстное меню для быстрых действий.

**Файлы для загрузки:**
* **Setup.exe** — удобный установщик приложения со всеми встроенными зависимостями (включая zstd).
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
Upload-Asset (Join-Path $PSScriptRoot "FaceitDemoManager.exe") "FaceitDemoManager.exe"
