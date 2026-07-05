$downloadsFolder = "$env:USERPROFILE\Downloads"
$cs2Directory = "D:\Pro\Steam\steamapps\common\Counter-Strike Global Offensive\game\csgo"

# 1. Check if CS2 directory exists
if (-not (Test-Path $cs2Directory)) {
    Write-Error "CS2 directory not found at: $cs2Directory"
    exit 1
}

# 2. Find all .dem.zst files in Downloads
$zstFiles = Get-ChildItem -Path $downloadsFolder -Filter "*.dem.zst"

if ($zstFiles.Count -eq 0) {
    Write-Host "No *.dem.zst files found in Downloads ($downloadsFolder)." -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($zstFiles.Count) demo archive(s)." -ForegroundColor Green

foreach ($file in $zstFiles) {
    $zstPath = $file.FullName
    
    # Extract short unique name from match ID
    $shortName = "faceit_latest"
    if ($file.Name -match "(1-([a-f0-9]{8})-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})") {
        $matchId = $Matches[1]
        $shortId = $Matches[2]
        $defaultShortName = "faceit_" + $shortId
        
        Write-Host "Fetching match details from Faceit API..."
        try {
            $response = Invoke-RestMethod -Uri "https://api.faceit.com/match/v2/match/$matchId" -TimeoutSec 10
            $payload = $response.payload
            $date = $payload.startedAt.Substring(0, 10)
            $mapName = $payload.maps.name.Replace(" ", "")
            $score = "$($payload.summaryResults.factions.faction1.score)-$($payload.summaryResults.factions.faction2.score)"
            
            if ($date -and $mapName -and $score) {
                $shortName = "faceit_${date}_${mapName}_${score}_${shortId}"
            } else {
                $shortName = $defaultShortName
            }
        } catch {
            Write-Host "Warning: Could not fetch match details: $_. Using default name." -ForegroundColor Yellow
            $shortName = $defaultShortName
        }
    } else {
        $shortName = "faceit_" + $file.BaseName.Replace(".dem", "")
    }

    Write-Host "`nProcessing $($file.Name) -> $shortName.dem" -ForegroundColor Cyan

    # Extract in Downloads folder using local zstd.exe
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $zstdPathExe = Join-Path $scriptDir "zstd.exe"
    
    Write-Host "Extracting..."
    $extractedBaseName = $file.BaseName # e.g. "1-d9be9777-3fec-4c73-b551-153abe4764b9-1-1.dem"
    $extractedPath = Join-Path $downloadsFolder $extractedBaseName
    
    & "$zstdPathExe" -d "$zstPath" -o "$extractedPath" --force
    
    if (Test-Path $extractedPath) {
        $targetDir = Join-Path $cs2Directory "faceit_demos\General"
        if (-not (Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }
        
        $destPathUnique = Join-Path $targetDir "$shortName.dem"
        $destPathLatest = Join-Path $targetDir "faceit.dem"
        
        Write-Host "Moving to CS2 directory: $destPathUnique"
        Move-Item -Path $extractedPath -Destination $destPathUnique -Force
        
        Write-Host "Creating a copy as: $destPathLatest (for quick launch)"
        Copy-Item -Path $destPathUnique -Destination $destPathLatest -Force
        
        Write-Host "Removing original archive: $zstPath"
        Remove-Item -Path $zstPath -Force
        
        Write-Host "Successfully processed!" -ForegroundColor Green
        Write-Host "To play this match in CS2, open the console and type:" -ForegroundColor White
        Write-Host "  playdemo faceit_demos/General/$shortName" -ForegroundColor Yellow
        Write-Host "or just play the latest one with:" -ForegroundColor White
        Write-Host "  playdemo faceit_demos/General/faceit" -ForegroundColor Yellow
    } else {
        Write-Error "Failed to locate extracted file: $extractedPath"
    }
}
