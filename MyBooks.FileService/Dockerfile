# ===== build stage =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
WORKDIR /src/MyBooks.FileService
RUN dotnet publish MyBooks.FileService.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet","MyBooks.FileService.dll"]
