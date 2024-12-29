# Define the path to your .oft files
$oftenFilesPath = "C:\Users\billym\Documents\MonthlyDirectoryEmails"

# Import Outlook COM object
$outlook = New-Object -ComObject Outlook.Application

# Send each .oft file
Get-ChildItem -Path $oftenFilesPath -Filter "*.oft" | ForEach-Object {
    $templatePath = $_.FullName

    # Open the .oft template
    $mail = $outlook.CreateItemFromTemplate($templatePath)
    
    # Uncomment if you want to add additional customizations, e.g., CC, Body, etc.
    # $mail.Subject = $subject
    # $mail.To = $recipients
    # $mail.CC = "cc@example.com"
    # $mail.Body = "This email was sent automatically from an .oft template."

    # Send the email
    $mail.Send()
    Write-Output "Sent email using template: $templatePath"
}

# Release Outlook COM object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($outlook) | Out-Null
$outlook = $null
[GC]::Collect()
[GC]::WaitForPendingFinalizers()