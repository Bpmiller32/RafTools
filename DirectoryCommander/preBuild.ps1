# Grab args from command line
param(
    [Parameter(Mandatory)]
    [String]$Project
)

# Set base directory to come back to
$BaseDirectory = Get-Location

# Switch and build project based on argument
if ($Project -eq "Crawler") {
    Set-Location .\Crawler.App
    dotnet build
    Set-Location -Path $BaseDirectory
}
if ($Project -eq "Builder") {
    Set-Location .\Builder.App
    dotnet build
    Copy-Item -Path ".\BuildUtils" -Destination ".\bin\Debug\net6.0-windows\BuildUtils" -Recurse -Force
    Set-Location -Path $BaseDirectory
}
if ($Project -eq "Tester") {
    Set-Location .\Tester.App
    dotnet build
    Set-Location -Path $BaseDirectory
}
if ($Project -eq "Common") {
    Set-Location .\Common.Data
    dotnet build
    Set-Location -Path $BaseDirectory
}
if ($Project -eq "UI") {
    Set-Location .\UI
    npm install
    Set-Location -Path $BaseDirectory
}

# dotnet publishing
# dotnet publish --configuration Release --runtime win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:PublishTrimmed=true 

# node publishing
# Set-Location -Path .\Ui
# npm publish
# Set-Location -Path $BaseDirectory