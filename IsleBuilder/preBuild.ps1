dotnet build .\IoMDirectoryBuilder.Common\IoMDirectoryBuilder.Common.csproj
dotnet build .\IoMDirectoryBuilder.Console\IoMDirectoryBuilder.Console.csproj

dotnet publish .\IoMDirectoryBuilder.Console\IoMDirectoryBuilder.Console.csproj --configuration Release --runtime win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true
Copy-Item -Path .\IoMDirectoryBuilder.Console\bin\Release\net6.0-windows\win-x64\publish\IoMDirectoryBuilder.Console.exe -Destination ~\Desktop -Force
