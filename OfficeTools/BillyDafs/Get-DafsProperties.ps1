<#
.SYNOPSIS
    Extracts properties from .dr files and exports them to CSV.

.DESCRIPTION
    This script uses DafsClrHelper.dll to extract specified properties from all .dr files
    in a given folder and exports the results to a CSV file.

.PARAMETER FolderPath
    The path to the folder containing .dr files.

.PARAMETER Properties
    An array of property names to extract from each file.

.PARAMETER OutputCsv
    The path to the output CSV file. Default is "DafsProperties.csv" in the current directory.

.PARAMETER Section
    The section name to use when extracting properties. Default is empty string.

.EXAMPLE
    .\Get-DafsProperties.ps1 -FolderPath "C:\Path\To\DrFiles" -Properties "RM: Postcode","RM: Final" -OutputCsv "results.csv"

.EXAMPLE
    .\Get-DafsProperties.ps1 -FolderPath "C:\Path\To\DrFiles" -Properties "RM: Postcode","RM: Final","RM: Directory XML" -Section "Settings" -OutputCsv "detailed_results.csv"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$FolderPath,
    
    [Parameter(Mandatory=$true)]
    [string[]]$Properties,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputCsv = "DafsProperties.csv",
    
    [Parameter(Mandatory=$false)]
    [string]$Section = ""
)

# Load the DafsClrHelper Assembly
try {
    $dllPath = Join-Path -Path $PSScriptRoot -ChildPath "DafsClrHelper.dll"
    
    # Check if the DLL exists
    if (-not (Test-Path -Path $dllPath -PathType Leaf)) {
        Write-Error "DafsClrHelper.dll not found at $dllPath"
        exit 1
    }
    
    # Try to unblock the file if it's blocked
    try {
        Unblock-File -Path $dllPath -ErrorAction SilentlyContinue
        Write-Verbose "Attempted to unblock DafsClrHelper.dll"
    }
    catch {
        Write-Verbose "Could not unblock file, continuing anyway: $_"
    }
    
    # Try different methods to load the assembly
    try {
        # Method 1: Using Add-Type
        Add-Type -Path $dllPath
        Write-Verbose "Successfully loaded DafsClrHelper.dll using Add-Type"
    }
    catch {
        Write-Verbose "Failed to load using Add-Type: $_"
        
        try {
            # Method 2: Using Assembly.LoadFrom
            $assembly = [System.Reflection.Assembly]::LoadFrom($dllPath)
            Write-Verbose "Successfully loaded DafsClrHelper.dll using Assembly.LoadFrom"
        }
        catch {
            Write-Verbose "Failed to load using Assembly.LoadFrom: $_"
            
            try {
                # Method 3: Using Assembly.LoadFile
                $assembly = [System.Reflection.Assembly]::LoadFile($dllPath)
                Write-Verbose "Successfully loaded DafsClrHelper.dll using Assembly.LoadFile"
            }
            catch {
                Write-Error "All methods to load DafsClrHelper.dll failed. Last error: $_"
                Write-Error "This may be due to security restrictions or the DLL being blocked."
                Write-Error "Try manually unblocking the DLL: Right-click the file, select Properties, and check 'Unblock' if available."
                exit 1
            }
        }
    }
    
    Write-Verbose "Successfully loaded DafsClrHelper.dll from $dllPath"
}
catch {
    Write-Error "Failed to load DafsClrHelper.dll: $_"
    exit 1
}

# Function to safely get property value
function Get-SafePropertyValue {
    param(
        [string]$FilePath,
        [string]$Section,
        [string]$PropertyName,
        [string]$PropertyType = "String"
    )
    
    try {
        switch ($PropertyType) {
            "String" { 
                return [DafsClrHelper.DafsFunctions]::GetStringPropFromFile($FilePath, $Section, $PropertyName) 
            }
            "WideString" { 
                return [DafsClrHelper.DafsFunctions]::GetWideStringPropFromFile($FilePath, $Section, $PropertyName) 
            }
            "Bool" { 
                return [DafsClrHelper.DafsFunctions]::GetBoolValueFromFile($FilePath, $Section, $PropertyName) 
            }
            "Long" { 
                return [DafsClrHelper.DafsFunctions]::GetLongValueFromFile($FilePath, $Section, $PropertyName) 
            }
            default { 
                return [DafsClrHelper.DafsFunctions]::GetStringPropFromFile($FilePath, $Section, $PropertyName) 
            }
        }
    }
    catch {
        Write-Warning "Error getting property '$PropertyName' from file '$FilePath': $_"
        return $null
    }
}

# Validate folder path
if (-not (Test-Path -Path $FolderPath -PathType Container)) {
    Write-Error "Folder path '$FolderPath' does not exist or is not a directory."
    exit 1
}

# Get all .dr files
$drFiles = Get-ChildItem -Path $FolderPath -Filter "*.dr" -File -Recurse

if ($drFiles.Count -eq 0) {
    Write-Warning "No .dr files found in '$FolderPath'."
    exit 0
}

Write-Host "Found $($drFiles.Count) .dr files to process."

# Create results array
$results = @()

# Process each file
$processedCount = 0
foreach ($file in $drFiles) {
    $processedCount++
    Write-Progress -Activity "Processing .dr files" -Status "Processing file $processedCount of $($drFiles.Count)" -PercentComplete (($processedCount / $drFiles.Count) * 100)
    
    $fileResult = [PSCustomObject]@{
        FileName = $file.Name
        FilePath = $file.FullName
    }
    
    # Extract each property
    foreach ($prop in $Properties) {
        # Determine property type based on property name (can be expanded)
        $propType = "String"
        if ($prop -like "*Final*") { $propType = "Bool" }
        if ($prop -like "*Level of Sort*") { $propType = "Long" }
        
        # Get property value
        $value = Get-SafePropertyValue -FilePath $file.FullName -Section $Section -PropertyName $prop -PropertyType $propType
        
        # Add to result object
        $fileResult | Add-Member -MemberType NoteProperty -Name $prop -Value $value
    }
    
    $results += $fileResult
}

Write-Progress -Activity "Processing .dr files" -Completed

# Export to CSV
try {
    $results | Export-Csv -Path $OutputCsv -NoTypeInformation
    Write-Host "Processed $($drFiles.Count) files. Results exported to '$OutputCsv'."
}
catch {
    Write-Error "Failed to export results to CSV: $_"
    exit 1
}
