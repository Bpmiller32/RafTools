param (
    [string]$folderPath
)

# Path to the folder containing the images
# $folderPath = "\\techred\grays\US\TJX\20240818"

# Expected suffixes for a complete set
$expectedSuffixes = @("LB", "LF", "RB", "RF", "TOP")

# Get all the image files in the folder
$imageFiles = Get-ChildItem -Path $folderPath -Filter "*.jpg"

# Create a hashtable to group files by their base number (ignoring suffix)
$fileGroups = @{}

foreach ($file in $imageFiles) {
    # Extract the base number (two sets of numbers) and the suffix
    if ($file.Name -match "^(\d+_\d+)_([A-Z]+)\.jpg$") {
        $baseNumber = $matches[1]
        $suffix = $matches[2]

        # Add the suffix to the hashtable entry for the base number
        if (-not $fileGroups.ContainsKey($baseNumber)) {
            $fileGroups[$baseNumber] = @()
        }
        $fileGroups[$baseNumber] += $suffix
    }
}

# Check for incomplete sets, write out result of set that was processed
$totalSets = 0
$setsMissingASuffix = 0

foreach ($baseNumber in $fileGroups.Keys) {
    $existingSuffixes = $fileGroups[$baseNumber]
    $missingSuffixes = $expectedSuffixes | Where-Object { $_ -notin $existingSuffixes }

    if ($missingSuffixes.Count -gt 0) {
        Write-Host "  Incomplete set for $($baseNumber): Missing - $($missingSuffixes -join ', ')"
        $setsMissingASuffix++
    }
    else {
        Write-Host "  Complete set for $baseNumber"
        $totalSets++
    }
}

# Final result summary
Write-Host "All sets have been checked."
Write-Host "Folder: $($folderPath)"
Write-Host "Total sets: $($totalSets)"
Write-Host "Sets missing a suffix: $($setsMissingASuffix)"
