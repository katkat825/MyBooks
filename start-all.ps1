# Run all API microservices
Start-Process "dotnet" "run --project ./MyBooks.AuthService/MyBooks.AuthService.csproj"
Start-Process "dotnet" "run --project ./MyBooks.CatalogService/MyBooks.CatalogService.csproj"
Start-Process "dotnet" "run --project ./MyBooks.FileService/MyBooks.FileService.csproj"

# Run Angular UI (ng serve --open is equivalent to npm start in your esproj setup)
Start-Process "cmd.exe" "/c npm run start --prefix ./MyBooks.UI"