# Create Outlook COM Object
$Outlook = New-Object -ComObject Outlook.Application
$Namespace = $Outlook.GetNamespace("MAPI")
$Inbox = $Namespace.GetDefaultFolder([Microsoft.Office.Interop.Outlook.OlDefaultFolders]::olFolderInbox)

# Get all emails from the Inbox for the year 2024
$Items = $Inbox.Items | Where-Object {
    $_.ReceivedTime -ge (Get-Date -Year 2023 -Month 1 -Day 1) -and
    $_.ReceivedTime -lt (Get-Date -Year 2024 -Month 1 -Day 1)
}

# Array to store results for CSV export
$emailData = @()

# Process each email
foreach ($Mail in $Items) {
    if ($Mail -and $Mail.Subject -and $Mail.Subject -match "project review") {
        # Add the email details to the array
        $emailData += [pscustomobject]@{
            Subject    = $Mail.Subject
            Sender     = $Mail.SenderName
            DateSent   = $Mail.ReceivedTime.ToString("yyyy-MM-dd HH:mm")
            Body       = $Mail.Body
        }
    }
}

# Path to the CSV file
$csvFile = "$env:USERPROFILE\Documents\ProjectReviewEmails2023.csv"

# Export the results to a CSV file
$emailData | Export-Csv -Path $csvFile -NoTypeInformation -Encoding UTF8

Write-Output "Project review emails from 2024 have been logged to $csvFile"