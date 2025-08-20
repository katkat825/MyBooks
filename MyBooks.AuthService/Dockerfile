# ===== build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the whole repo so restore can find all project references
COPY . .
RUN dotnet restore ./MyBooks.sln

# Publish only the AuthService project
RUN dotnet publish ./MyBooks.AuthService/MyBooks.AuthService.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MyBooks.AuthService.dll"]
