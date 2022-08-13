Remove-Item -Path .\BillyService.App\bin -Recurse -Force
Remove-Item -Path .\BillyService.App\obj -Recurse -Force
dotnet build
dotnet publish --configuration Release --runtime win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:PublishTrimmed=true 
