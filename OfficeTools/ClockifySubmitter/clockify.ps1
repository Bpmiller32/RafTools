function Get-Workspace {
    param (
        $baseUrl,
        $headers
    )

    # Make GET request to list all workspaces
    $response = Invoke-RestMethod -Uri "$($baseUrl)/workspaces" -Headers $headers

    # Check response
    if (-not ($response)) {
        return
    }

    # Iterate through workspaces and display their names
    foreach ($workspace in $response) {
        if ($workspace.name -eq "RAF Software Technology Inc. - Volaris") {
            return $workspace
        }
    }
}

function Get-Project {
    param (
        $baseUrl,
        $headers,
        $workspace
    )

    # Make GET request to list all projects
    $response = Invoke-RestMethod -Uri "$($baseUrl)/workspaces/$($workspace.id)/projects" -Headers $headers

    # Check response
    if (-not ($response)) {
        return
    }

    # Make a returnable object
    $projectsList = New-Object PSObject -Property @{
        techservices = $null
        holiday      = $null
        dto          = $null        
    }

    # Iterate through projects and display their names
    foreach ($project in $response) {
        if ($project.name -eq "Tech Services - Admin") {
            $projectsList.techservices = $project
        }

        if ($project.name -eq "Holiday/Floating Holiday") {
            $projectsList.holiday = $project
        }

        if ($project.name -eq "DTO") {
            $projectsList.dto = $project
        }
    }

    return $projectsList
}

function Submit-Time {
    param (
        $baseUrl,
        $headers,
        $workspace,
        $projectsList,
        $holidays
    )

    # Define parameters
    $today = (Get-Date).ToString('yyyy-MM-dd') # Date in YYYY-MM-DD format
    $start = "15:00:00"   # Start time in HH:MM:SS format
    $end = "23:00:00"     # End time in HH:MM:SS format
    
    $projectToSubmit = $null

    # Check if today should be submitted as a holiday
    foreach ($holiday in $holdays) {
        if ($today -eq $holiday) {
            $projectToSubmit = $projectsList.holiday
            break
        }
    }

    if ($null -eq $projectToSubmit) {
        $projectToSubmit = $projectsList.techservices
    }

    # Check if it's Saturday (DayOfWeek value of 6) or Sunday (DayOfWeek value of 0)
    if ((Get-Date).DayOfWeek.value__ -eq 6 -or (Get-Date).DayOfWeek.value__ -eq 0) {
        Write-Host "Weekend, no time entered today"
        return
    }

    # Construct time entry payload
    $timeEntry = @{
        start     = "$($today)T$($start)Z"
        end       = "$($today)T$($end)Z"
        projectId = $projectToSubmit.id
        # description = ""
    }

    # Convert payload to JSON
    $jsonPayload = $timeEntry | ConvertTo-Json

    # Make POST request to create time entry
    $response = Invoke-RestMethod -Uri "$($baseUrl)/workspaces/$($workspace.id)/time-entries" -Method Post -Headers $headers -Body $jsonPayload

    # Check response
    if ($response) {
        Write-Host "Time entry submitted successfully - $($today) as $($projectToSubmit.name)."
    }
    else {
        Write-Host "Failed to submit time entry."
    }
}


# Define Clockify API endpoints
$baseUrl = "https://api.clockify.me/api/v1"
$apiKey = "ODgyYjY2NDEtOWVhMS00N2Q1LWIxYTMtNzM3MjA0Y2U0NzU3"
$headers = @{
    "X-Api-Key"    = $apiKey
    "Content-Type" = "application/json"
}

$holidays = @(
    "2024-02-19", 
    "2024-02-20",

    "2024-05-27",
    "2024-05-28", 
    
    "2024-07-04",
    "2024-07-05", 
    "2024-07-06", 

    "2024-09-02", 
    "2024-09-03", 

    "2024-11-28",
    "2024-11-29", 
    "2024-11-30", 

    "2024-12-24",
    "2024-12-25", 
    "2024-12-26", 
    "2024-12-27"
)

$rafWorkspace = Get-Workspace -baseUrl $baseUrl -headers $headers
if ($null -eq $rafWorkspace) {
    Write-Host "Failed to retrieve workspaces."
    return
}

$projectsList = Get-Project -baseUrl $baseUrl -headers $headers -workspace $rafWorkspace
if (($null -eq $projectsList.techservices) -or ($null -eq $projectsList.holiday) -or ($null -eq $projectsList.dto)) {
    Write-Host "Failed to retrieve projects."
    return
}

Submit-Time -baseUrl $baseUrl -headers $headers -workspace $rafWorkspace -projectsList $projectsList -holidays $holdays
