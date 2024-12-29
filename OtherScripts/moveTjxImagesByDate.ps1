# Define the source folder and destination folder base path
param (
    [string]$sourceFolder
)
$destinationFolderBase = '\\techred\grays$\US\TJX'

# Define the log file path (on your desktop)
$logFile = [System.IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), "ImageMoveLog3.txt")

# Log folder to be processed
$folderMessage = "Starting processing folder: $sourceFolder"
Write-Host $folderMessage
$folderMessage | Add-Content -Path $logFile

# Get a list of image files in the source folder (e.g., jpg, png)
$images = Get-ChildItem $sourceFolder -Filter "*.jpg"

$totalImages = $images.Count
$currentImageIndex = 0

$imagesMovedTotal = 0
$errorTotal = 0

foreach ($image in $images) {
    $currentImageIndex++

    # Prepare the progress information message
    $progressMessage = "Processing image $currentImageIndex of $totalImages : $($image.Name)"
    
    # Display and log the progress information
    Write-Host $progressMessage
    $progressMessage | Add-Content -Path $logFile

    # Get the 'LastWriteTime' property (date modified) of the image
    $modifiedDate = $image.LastWriteTime

    # Format the date as YYYYMMDD for folder naming
    $folderName = $modifiedDate.ToString("yyyyMMdd")

    # Create the full destination folder path based on the date
    $destinationFolder = Join-Path $destinationFolderBase $folderName

    # Check if the folder exists, if not, create it
    if (-not (Test-Path $destinationFolder)) {
        New-Item -ItemType Directory -Path $destinationFolder
    }

    # Check if the source folder is within the destination folder base
    $isInDestinationBase = $image.FullName.StartsWith($destinationFolderBase)

    if ($isInDestinationBase -and ($image.DirectoryName -eq $destinationFolder)) {
        # Prepare the log message for files that don't need to be moved
       $logMessage = "No need to move: $($image.Name) is already in folder $folderName"
        
        # Display and log the message
        Write-Host $logMessage
        $logMessage | Add-Content -Path $logFile
    } else {
        try {
            # Move the image to the corresponding date folder
            Move-Item $image.FullName -Destination $destinationFolder -ErrorAction Stop
            
            $imagesMovedTotal++

            # Prepare the success message
            $successMessage = "Moved: $($image.Name) to folder $folderName"
            
            # Display and log the success message
            Write-Host $successMessage
            $successMessage | Add-Content -Path $logFile
        }
        catch {
        $errorTotal++

            # Prepare error message
            $errorMessage = "Unable to move: $($image.Name) to folder $folderName , it already exists or is locked, moving to duplicate folder"

            Move-Item $image.FullName -Destination "\\techred\grays$\US\TJX\duplicates" -ErrorAction Stop
            

         # Display and log the success message
         Write-Host $errorMessage
         $errorMessage | Add-Content -Path $logFile
        }

    }
}

# Final completion message
$completionMessage = "All images processed successfully. Total moved: $imagesMovedTotal , Total error: $errorTotal , for folder: $sourceFolder"
Write-Host $completionMessage
$completionMessage | Add-Content -Path $logFile