FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine
COPY ./publish/portable /app
ENTRYPOINT ["dotnet", "/app/journey-markdown-converter.dll"]