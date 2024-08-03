# Define the process name to look for
$processName = "ms-teams" # Replace with your process name

# Get all instances of the process
$processes = Get-Process -Name $processName -ErrorAction SilentlyContinue

# Check if any instances of the process are running
if ($processes) {
    # Generate a random number of seconds between 30 and 300 (5 minutes)
    $randomSeconds = Get-Random -Minimum 30 -Maximum 300

    # Wait for the random amount of time
    Write-Host "Waiting for $($randomSeconds) seconds before launching"
    Start-Sleep -Seconds $randomSeconds

    # Terminate all instances of the process
    foreach ($process in $processes) {
        try {
            $process.Kill()
            Write-Host "Terminated process $($process.Id) - $processName"
        } catch {
            Write-Host "Failed to terminate process $($process.Id) - $processName. Error: $_"
        }
    }
} else {
    Write-Host "No instances of the process '$processName' are running."
}