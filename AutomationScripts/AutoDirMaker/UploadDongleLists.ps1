# Update lists to latest version
Set-Location -Path "C:\Users\billym\Documents\DirMaker\DongleLists"
svn update
Write-Host "Updated all dongle lists from SVN"

# Define FTP server details
$ftpServer = "ftp://174.21.88.90"
$ftpUsername = "raftech"
$ftpPassword = 'V0m|tT!'
$localFolder = "C:\Users\billym\Documents\DirMaker\DongleLists"

# Function to upload a file to the FTP server
function Upload-FileToFTP {
    param (
        [string]$localFilePath,
        [string]$ftpFilePath
    )

    # Create FTP request
    $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpFilePath)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
    $ftpRequest.UseBinary = $true
    $ftpRequest.KeepAlive = $false

    # Read the file content
    $fileContent = [System.IO.File]::ReadAllBytes($localFilePath)
    $ftpRequest.ContentLength = $fileContent.Length

    # Get the request stream and write the file content
    $requestStream = $ftpRequest.GetRequestStream()
    $requestStream.Write($fileContent, 0, $fileContent.Length)
    $requestStream.Close()

    # Get the FTP server's response
    $ftpResponse = $ftpRequest.GetResponse()
    $ftpResponseStream = $ftpResponse.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($ftpResponseStream)
    $reader.Close()
    $ftpResponse.Close()

    Write-Host "Uploaded $localFilePath to $ftpFilePath"
}

# Get all files in the local folder
$files = Get-ChildItem -Path $localFolder -File

# Loop through each file and upload it to the FTP server
foreach ($file in $files) {
    $localFilePath = $file.FullName
    $ftpFilePath = "$ftpServer/$($file.Name)"
    Upload-FileToFTP -localFilePath $localFilePath -ftpFilePath $ftpFilePath
}

Write-Host "All files have been uploaded."