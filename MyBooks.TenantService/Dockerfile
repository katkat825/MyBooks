# ===== build stage =====
FROM build-base AS build
WORKDIR /src/MyBooks.TenantService
RUN dotnet publish MyBooks.TenantService.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== runtime stage =====
FROM runtime-base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet","MyBooks.TenantService.dll"]
