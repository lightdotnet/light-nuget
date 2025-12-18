echo "Begin build projects..."
dotnet build src/Files/Files.csproj --configuration Release --no-restore
dotnet build src/Serilog/Serilog.csproj --configuration Release --no-restore
dotnet build src/Mail.Contracts/Mail.Contracts.csproj --configuration Release --no-restore
dotnet build src/SmtpMail/SmtpMail.csproj --configuration Release --no-restore
dotnet build src/Graph/Graph.csproj --configuration Release --no-restore

echo "Begin pack projects..."
dotnet pack --no-build src/Files/Files.csproj --configuration Release --output D:\Publish\LightFramework
dotnet pack --no-build src/Serilog/Serilog.csproj --configuration Release --output D:\Publish\LightFramework
dotnet pack --no-build src/Mail.Contracts/Mail.Contracts.csproj --configuration Release --output D:\Publish\LightFramework
dotnet pack --no-build src/SmtpMail/SmtpMail.csproj --configuration Release --output D:\Publish\LightFramework
dotnet pack --no-build src/Graph/Graph.csproj --configuration Release --output D:\Publish\LightFramework

:: hold for view messages
pause