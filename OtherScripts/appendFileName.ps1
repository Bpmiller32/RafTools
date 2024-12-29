# Get the current directory
$currentDirectory = Get-Location

# Get all files in the current directory
$files = Get-ChildItem -Path $currentDirectory -File

# Loop through each file and rename it
foreach ($file in $files) {
  # Create the new file name by appending "_blank" before the file extension
    $newFileName = "$($file.BaseName)_blank$($file.Extension)"
    
    # Rename the file
    Rename-Item -Path $file.FullName -NewName $newFileName
}

Write-Host "Renaming complete!"