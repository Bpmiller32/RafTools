# Define the log file path (on your desktop)
$logFile = [System.IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), "TimeStamp.txt")

$timestampStart = Get-Date
Write-Host $timestampStart
$timeStampStart | Add-Content -Path $logFile

# Define the path to the script you want to call
$scriptToRun = "C:\Users\billym\Desktop\moveTjxImagesByDate.ps1"

# Run the script using the call operator
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240816"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240817"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240818"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240819"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240820"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240821"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240822"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240829"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240830"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240901"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240902"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240905"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240906"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240908"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240909"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240910"
& $scriptToRun -SourceFolder "\\techred\grays$\US\TJX\20240911"

$timestampEnd = Get-Date
Write-Host $timestampEnd
$timeStampEnd | Add-Content -Path $logFile