# ---------------------------------------------------------------------------- #
#                              Configure days here                             #
# ---------------------------------------------------------------------------- #

# Add or modify holidays here
$holidays = @(
    "2024-02-19", 
    "2024-02-20",

    "2024-05-27",
    
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

# Add or modify vacation days here
$vacationDays = @(
    "2024-08-29",
    "2024-08-30",

    "2024-10-14",
    "2024-10-15",
    "2024-10-16",
    "2024-10-17",
    "2024-10-18",

    "2024-10-21",
    "2024-10-22",
    "2024-10-23",
    "2024-10-24",
    "2024-10-25"
)

# Add or modify skipped days here
$skipDays = @(
  
)

# ---------------------------------------------------------------------------- #
#                                     Setup                                    #
# ---------------------------------------------------------------------------- #

$today = (Get-Date).ToString('yyyy-MM-dd') # Date in YYYY-MM-DD format

# Check if today is a holiday
foreach ($holiday in $holidays) {
    if ($today -eq $holiday) {
        Write-Host "Holiday, not logging time today"
        return
    }
}

# Check if today is a vacation day
foreach ($vacationDay in $vacationDays) {
    if ($today -eq $vacationDay) {
        Write-Host "Vacation day, not logging time today"
        return
    }
}

# Check if today is a day to skip
foreach ($skipDay in $skipDays) {
    if ($today -eq $skipDay) {
        Write-Host "For whatever reason, not logging time today"
        return
    }
}

# Check if it's Saturday (DayOfWeek value of 6) or Sunday (DayOfWeek value of 0)
if ((Get-Date).DayOfWeek.value__ -eq 6 -or (Get-Date).DayOfWeek.value__ -eq 0) {
    Write-Host "Weekend, no time entered today"
    return
}

# ---------------------------------------------------------------------------- #
#                                     Main                                     #
# ---------------------------------------------------------------------------- #

# Define the process name to check and the command to run if the process is not found
$processName = "ms-teams" # Replace with your process name
$commandToRun = "ms-teams" # Replace with the command you want to run

# Check if the process is running
$process = Get-Process -Name $processName -ErrorAction SilentlyContinue

# If the process is not running, run the command
if (-not $process) {
    # Generate a random number of seconds between 30 and 300 (5 minutes)
    $randomSeconds = Get-Random -Minimum 30 -Maximum 300

    # Wait for the random amount of time
    Write-Host "Waiting for $($randomSeconds) seconds before launching"
    Start-Sleep -Seconds $randomSeconds

    # Run the command
    Start-Process $commandToRun
}
else {
    Write-Host "Process is already running"
    return
}



