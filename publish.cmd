dotnet publish -c Release -o publish/portable src/journey-markdown-converter.fsproj
dotnet publish -c Release -r win-x86 -p:PublishSingleFile=true --self-contained true -o publish/win-x86 src/journey-markdown-converter.fsproj
dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true -o publish/linux-x64 src/journey-markdown-converter.fsproj
dotnet publish -c Release -r osx-x64 -p:PublishSingleFile=true --self-contained true -o publish/osx-x64 src/journey-markdown-converter.fsproj
dotnet publish -c Release -r osx.11.0-arm64 -p:PublishSingleFile=true --self-contained true -o publish/osx.11.0-arm64 src/journey-markdown-converter.fsproj