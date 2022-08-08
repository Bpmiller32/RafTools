dotnet build
# dotnet publish --configuration Release --runtime win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:PublishTrimmed=true 
dotnet publish --configuration Release --runtime win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true
Copy-Item -Path .\IoMDirectoryBuilder.App\bin\Release\net6.0-windows\win-x64\publish\IoMDirectoryBuilder.App.exe -Destination ~\Desktop -Force
Rename-Item -Path ~\Desktop\IoMDirectoryBuilder.App.exe -NewName IoMDirectoryBuilder.exe