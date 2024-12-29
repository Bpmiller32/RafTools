# Define the paths for the folders
$folder1 = "C:\Users\billym\Downloads\ucsd\flat"  # Replace with the path to the first folder
$folder2 = "\\techred.raf.com\groundtruth$\US\UCSD"  # Replace with the path to the second folder
$destination = "C:\Users\billym\Downloads\diffr"  # Replace with the destination folder path

# Ensure the destination folder exists
if (!(Test-Path $destination)) {
    New-Item -ItemType Directory -Path $destination
}

# Get the file names from both folders
$filesInFolder1 = Get-ChildItem -Path $folder1 -File | Select-Object -ExpandProperty Name
$filesInFolder2 = Get-ChildItem -Path $folder2 -File | Select-Object -ExpandProperty Name

# Find files that are in Folder1 but not in Folder2
$filesOnlyInFolder1 = $filesInFolder1 | Where-Object { $_ -notin $filesInFolder2 }

# Find files that are in Folder2 but not in Folder1
$filesOnlyInFolder2 = $filesInFolder2 | Where-Object { $_ -notin $filesInFolder1 }

# Combine the unique files
$uniqueFiles = $filesOnlyInFolder1 + $filesOnlyInFolder2

# Copy the unique files to the destination folder
foreach ($file in $uniqueFiles) {
    $sourcePath = 
        if ($filesOnlyInFolder1 -contains $file) { Join-Path $folder1 $file }
        elseif ($filesOnlyInFolder2 -contains $file) { Join-Path $folder2 $file }
    
    Write-Host "Copying $file to $destination"
    Copy-Item -Path $sourcePath -Destination $destination
}

Write-Host "Unique files have been copied to $destination"