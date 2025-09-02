# ===== build stage =====
FROM build-base AS build
WORKDIR /src/MyBooks.CatalogService
RUN dotnet publish MyBooks.CatalogService.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== runtime stage =====
FROM runtime-base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet","MyBooks.CatalogService.dll"]
