# Unblock-DafsDll.ps1
# This script helps troubleshoot and resolve issues with loading the DafsClrHelper.dll

# Get the path to the DLL
$dllPath = Join-Path -Path $PSScriptRoot -ChildPath "DafsClrHelper.dll"

# Check if the DLL exists
if (-not (Test-Path -Path $dllPath -PathType Leaf)) {
    Write-Host "ERROR: DafsClrHelper.dll not found at $dllPath" -ForegroundColor Red
    Write-Host "Please ensure the DLL is in the same directory as this script." -ForegroundColor Yellow
    exit 1
}

Write-Host "Found DafsClrHelper.dll at: $dllPath" -ForegroundColor Green

# Check if the file is blocked
$zoneid = $null
try {
    $zoneid = Get-Item $dllPath -Stream "Zone.Identifier" -ErrorAction SilentlyContinue
}
catch {
    Write-Host "Could not check if file is blocked: $_" -ForegroundColor Yellow
}

if ($zoneid) {
    Write-Host "The DLL appears to be blocked by Windows security." -ForegroundColor Yellow
    
    # Try to unblock the file
    try {
        Write-Host "Attempting to unblock the DLL..." -ForegroundColor Cyan
        Unblock-File -Path $dllPath -ErrorAction Stop
        Write-Host "Successfully unblocked the DLL!" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to automatically unblock the DLL: $_" -ForegroundColor Red
        Write-Host "Please try to manually unblock the file:" -ForegroundColor Yellow
        Write-Host "1. Right-click on $dllPath" -ForegroundColor Yellow
        Write-Host "2. Select Properties" -ForegroundColor Yellow
        Write-Host "3. Check the 'Unblock' checkbox at the bottom of the General tab" -ForegroundColor Yellow
        Write-Host "4. Click OK" -ForegroundColor Yellow
    }
}
else {
    Write-Host "The DLL does not appear to be blocked by Windows security." -ForegroundColor Green
}

# Try to load the DLL using different methods
Write-Host "`nAttempting to load the DLL using different methods..." -ForegroundColor Cyan

# Method 1: Using Add-Type
Write-Host "`nMethod 1: Using Add-Type" -ForegroundColor Cyan
try {
    Add-Type -Path $dllPath
    Write-Host "SUCCESS: Loaded DafsClrHelper.dll using Add-Type" -ForegroundColor Green
}
catch {
    Write-Host "FAILED: Could not load using Add-Type" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    
    # Method 2: Using Assembly.LoadFrom
    Write-Host "`nMethod 2: Using Assembly.LoadFrom" -ForegroundColor Cyan
    try {
        $assembly = [System.Reflection.Assembly]::LoadFrom($dllPath)
        Write-Host "SUCCESS: Loaded DafsClrHelper.dll using Assembly.LoadFrom" -ForegroundColor Green
    }
    catch {
        Write-Host "FAILED: Could not load using Assembly.LoadFrom" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
        
        # Method 3: Using Assembly.LoadFile
        Write-Host "`nMethod 3: Using Assembly.LoadFile" -ForegroundColor Cyan
        try {
            $assembly = [System.Reflection.Assembly]::LoadFile($dllPath)
            Write-Host "SUCCESS: Loaded DafsClrHelper.dll using Assembly.LoadFile" -ForegroundColor Green
        }
        catch {
            Write-Host "FAILED: Could not load using Assembly.LoadFile" -ForegroundColor Red
            Write-Host "Error: $_" -ForegroundColor Red
            
            Write-Host "`nAll methods to load the DLL failed." -ForegroundColor Red
            Write-Host "`nPossible solutions:" -ForegroundColor Yellow
            Write-Host "1. Run PowerShell as Administrator" -ForegroundColor Yellow
            Write-Host "2. Manually unblock the DLL file (right-click > Properties > Unblock)" -ForegroundColor Yellow
            Write-Host "3. Copy the DLL to a local drive if it's on a network share" -ForegroundColor Yellow
            Write-Host "4. Check if the DLL is compatible with your PowerShell version" -ForegroundColor Yellow
            Write-Host "5. Verify that the DLL is not corrupted by re-copying it from the source" -ForegroundColor Yellow
            exit 1
        }
    }
}

Write-Host "`nDLL loading test completed successfully!" -ForegroundColor Green
Write-Host "You should now be able to run the Get-DafsProperties.ps1 script." -ForegroundColor Green
