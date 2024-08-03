# ---------------------------------------------------------------------------- #
#                                Function setup                                #
# ---------------------------------------------------------------------------- #

function Send-HttpPost {
    param (
        $directory,
        $module,
        [Parameter(Mandatory = $false)]
        [string]$dataYearMonth = "string"
    )

    # Define the web address (URL) and the request body
    $url = "https://termite-grand-moose.ngrok-free.app/$($directory)/$($module)"

    $requestBody = @{
    }

    if ($module -eq "Crawler") {
        $requestBody.Add("moduleCommand", "start")
    }

    if ($module -eq "Builder") {
        $requestBody.Add("moduleCommand", "start")
        $requestBody.Add("dataYearMonth", $dataYearMonth)
    }

    $requestBody = $requestBody | ConvertTo-Json

    # Define the headers if necessary
    $headers = @{
        "Content-Type" = "application/json"
    }

    # Send the HTTP POST request and check for status code 200 success
    try {
        Invoke-RestMethod -Uri $url -Method Post -Body $requestBody -Headers $headers -OperationTimeoutSeconds 30 -ErrorAction Stop
    }
    catch {
        Write-Host "Request to $url failed. Error: $_"
    }
}

# Define the function to get and parse the SSE message for the below function Send-HttpGet
function Send-DirBuild {
    param (
        [string]$directory
    )

    $readyToBuild = $status.$($directory).Crawler.ReadyToBuild.DataYearMonth
    $buildComplete = $status.$($directory).Builder.BuildComplete.DataYearMonth
    
    # Convert the delimited lists into arrays
    $array1 = $readyToBuild -split '\|'
    $array2 = $buildComplete -split '\|'
    
    # Sort the arrays
    $array1 = $array1 | Sort-Object
    $array2 = $array2 | Sort-Object
    
    # Compare the arrays
    $inArray1NotArray2 = Compare-Object -ReferenceObject $array1 -DifferenceObject $array2 -PassThru | Where-Object { $_.SideIndicator -eq "<=" }
    
    # Output the differences
    if ($inArray1NotArray2.Count -gt 0) {
        Write-Host "Entries in array1 but not in array2: $($inArray1NotArray2)"

        foreach ($dataYearMonth in $inArray1NotArray2) {
            Send-HttpPost -directory $directory -module "Builder" -dataYearMonth $dataYearMonth

            # Only build one at a time/script run so that the builder modules can process. Shouldn't have more than 1 to build anyway
            break;
        }
    }
    else {
        Write-Host "No entries found in array1 that are not in array2."
    }
}

# Define the function to get and parse the SSE message for the below function Send-HttpGet
function Get-SSEMessage {
    param (
        [string]$url
    )

    # Call the .NET method to get the SSE message
    $task = [SSEClient]::GetSSEMessageAsync($url)
    $task.Wait()
    $message = $task.Result

    # Parse the JSON response
    if ($message) {
        $json = $message | ConvertFrom-Json
        return $json
    }
    else {
        Write-Host "No data received from SSE endpoint."
        return $null
    }
}

function Send-HttpGet {
    # Define the URL for the SSE endpoint
    $url = "https://termite-grand-moose.ngrok-free.app/status" 

    # Define a .NET HttpClient
    Add-Type @"
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

public class SSEClient
{
    private static HttpClient _httpClient = new HttpClient();

    public static async Task<string> GetSSEMessageAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var reader = new System.IO.StreamReader(stream))
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data:"))
                {
                    return line.Substring(5).Trim();
                }
            }
        }

        return null;
    }
}
"@

    # Get and parse the SSE message
    $response = Get-SSEMessage -url $url

    # Output the response
    if ($response) {
        return $response
    }
}

# ---------------------------------------------------------------------------- #
#                                 Timing setup                                 #
# ---------------------------------------------------------------------------- #

# Get the current date
$currentDate = Get-Date

# Get the last day of the current month
$lastDayOfMonth = [datetime]::DaysInMonth($currentDate.Year, $currentDate.Month)
$endOfMonth = Get-Date -Year $currentDate.Year -Month $currentDate.Month -Day $lastDayOfMonth

# Calculate the difference in days between today and the end of the month
$daysUntilEndOfMonth = ($endOfMonth - $currentDate).Days

# Check if today is within 10 or fewer days from the end of the month
if ($daysUntilEndOfMonth -le 14) {
    Write-Host "Today is within 14 or fewer days from the end of the current month, continuing with script."
}
else {
    Write-Host "Today is more than 14 days from the end of the current month, ending script."
    return
}

# ---------------------------------------------------------------------------- #
#                                     Main                                     #
# ---------------------------------------------------------------------------- #

Write-Host "Sending SmartMatch crawler"
Send-HttpPost -directory "SmartMatch" -module "Crawler"
Start-Sleep -Seconds 5

Write-Host "Sending Parascript crawler"
Send-HttpPost -directory "Parascript" -module "Crawler"
Start-Sleep -Seconds 5

Write-Host "Sending RoyalMail crawler"
Send-HttpPost -directory "RoyalMail" -module "Crawler"
Start-Sleep -Seconds 5

Write-Host "Getting status of all directories"
$status = Send-HttpGet

Write-Host "Building all ready to build SmartMatch"
Send-DirBuild -directory "SmartMatch"
Start-Sleep -Seconds 5

Write-Host "Building all ready to build Parascript"
Send-DirBuild -directory "Parascript"
Start-Sleep -Seconds 5

Write-Host "Building all ready to build RoyalMail"
Send-DirBuild -directory "RoyalMail"
