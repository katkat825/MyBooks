# ===== build stage =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
WORKDIR /src/MyBooks.FileService
RUN dotnet publish MyBooks.FileService.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# install Calibre CLI (for ebook-convert)
RUN apt-get update && \
    apt-get install -y --no-install-recommends calibre && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyBooks.FileService.dll"]
