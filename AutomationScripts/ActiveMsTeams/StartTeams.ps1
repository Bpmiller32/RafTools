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

# Check if today is a holiday, vacation, or skip day
if ($holidays -contains $today) {
    Write-Host "Holiday, not logging time today"
    return
}
if ($vacationDays -contains $today) {
    Write-Host "Vacation day, not logging time today"
    return
}
if ($skipDays -contains $today) {
    Write-Host "For whatever reason, not logging time today"
    return
}

# Check if it's a weekend
if ((Get-Date).DayOfWeek -eq "Saturday" -or (Get-Date).DayOfWeek -eq "Sunday") {
    Write-Host "Weekend, no time entered today"
    return
}

# ---------------------------------------------------------------------------- #
#                                     Main                                     #
# ---------------------------------------------------------------------------- #

# Define the process and command to launch Teams
$processName = "ms-teams"
$commandToRun = "ms-teams"

# Check if Teams is running
$process = Get-Process -Name $processName -ErrorAction SilentlyContinue

# If not running, start Teams
if (-not $process) {
    # Generate a random delay between 30 and 300 seconds
    # $randomSeconds = Get-Random -Minimum 30 -Maximum 300
    $randomSeconds = Get-Random -Minimum 1 -Maximum 3
    Write-Host "Waiting for $randomSeconds seconds before launching Teams..."
    Start-Sleep -Seconds $randomSeconds

    # Launch Teams
    Start-Process $commandToRun
    Start-Sleep -Seconds 15  # Give it some time to start
}

# Find the Teams main window
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class User32 {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
}
"@

# Bring Teams to the foreground
$teamsWindow = (Get-Process -Name $processName -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 })

if ($teamsWindow) {
    $handle = $teamsWindow.MainWindowHandle
    if ($handle -ne 0) {
        Write-Host "Bringing Teams to the foreground..."
        [User32]::ShowWindowAsync($handle, 5)  # Restore window if minimized
        [User32]::SetForegroundWindow($handle) # Set as active window

        # Keep active for 60 seconds
        Start-Sleep -Seconds 60

        # Close the window (simulate clicking X)
        Write-Host "Closing Teams window (sending to tray)..."
        $teamsWindow.CloseMainWindow() | Out-Null
    }
} else {
    Write-Host "Could not find Teams window handle."
}