# ===== build stage =====
FROM build-base AS build
WORKDIR /src/MyBooks.EmailService
RUN dotnet publish MyBooks.EmailService.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== runtime stage =====
FROM runtime-base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet","MyBooks.EmailService.dll"]
