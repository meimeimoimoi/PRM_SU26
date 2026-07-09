# docker/identity.Dockerfile (cập nhật đường dẫn)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared layers
COPY ["src/Shared/SmartDine.Domain/SmartDine.Domain.csproj", "Shared/SmartDine.Domain/"]
COPY ["src/Shared/SmartDine.Application/SmartDine.Application.csproj", "Shared/SmartDine.Application/"]
COPY ["src/Shared/SmartDine.Infrastructure/SmartDine.Infrastructure.csproj", "Shared/SmartDine.Infrastructure/"]

# Copy service
COPY ["src/Services/Identity/SmartDine.Identity.API/SmartDine.Identity.API.csproj", "Services/Identity/SmartDine.Identity.API/"]

# Restore
RUN dotnet restore "Services/Identity/SmartDine.Identity.API/SmartDine.Identity.API.csproj"

# Copy all source
COPY src/ .

WORKDIR "/src/Services/Identity/SmartDine.Identity.API"
RUN dotnet build "SmartDine.Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Identity.API.dll"]