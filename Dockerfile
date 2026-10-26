# Build the Angular client.
FROM node:22-alpine AS client-build
WORKDIR /src/client

COPY happyInventory.Client/package.json happyInventory.Client/package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY happyInventory.Client/ ./
RUN npm run build

# Build and publish the ASP.NET Core API.
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS api-build
WORKDIR /src

COPY HappyInventory.Core/HappyInventory.Core.csproj HappyInventory.Core/
COPY HappyInventory.Application/HappyInventory.Application.csproj HappyInventory.Application/
COPY HappyInventory.Infrastructure/HappyInventory.Infrastructure.csproj HappyInventory.Infrastructure/
COPY HappyInventory.Api/HappyInventory.Api.csproj HappyInventory.Api/
RUN dotnet restore HappyInventory.Api/HappyInventory.Api.csproj

COPY HappyInventory.Core/ HappyInventory.Core/
COPY HappyInventory.Application/ HappyInventory.Application/
COPY HappyInventory.Infrastructure/ HappyInventory.Infrastructure/
COPY HappyInventory.Api/ HappyInventory.Api/

RUN dotnet publish HappyInventory.Api/HappyInventory.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Serve the Angular production build from the API's wwwroot.
COPY --from=client-build /src/client/dist/happy-inventory-client/browser /app/publish/wwwroot

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY --from=api-build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "HappyInventory.Api.dll"]
