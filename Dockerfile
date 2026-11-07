# Build the Angular client.
FROM node:22-alpine AS client-build
WORKDIR /src/client

COPY bariza-client/package.json bariza-client/package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY bariza-client/ ./
RUN npm run build

# Build and publish the ASP.NET Core API.
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS api-build
WORKDIR /src

COPY Bariza.Core/Bariza.Core.csproj Bariza.Core/
COPY Bariza.Application/Bariza.Application.csproj Bariza.Application/
COPY Bariza.Infrastructure/Bariza.Infrastructure.csproj Bariza.Infrastructure/
COPY Bariza.Api/Bariza.Api.csproj Bariza.Api/
RUN dotnet restore Bariza.Api/Bariza.Api.csproj

COPY Bariza.Core/ Bariza.Core/
COPY Bariza.Application/ Bariza.Application/
COPY Bariza.Infrastructure/ Bariza.Infrastructure/
COPY Bariza.Api/ Bariza.Api/

RUN dotnet publish Bariza.Api/Bariza.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Serve the Angular production build from the API's wwwroot.
COPY --from=client-build /src/client/dist/bariza-client/browser /app/publish/wwwroot

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY --from=api-build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Bariza.Api.dll"]
