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
COPY ["src/Services/Menu/SmartDine.Menu.API/SmartDine.Menu.API.csproj", "Services/Menu/SmartDine.Menu.API/"]

RUN dotnet restore "SmartDine.Menu.API/SmartDine.Menu.API.csproj"

# Copy all source
COPY src/ .
WORKDIR "/src/Services/Menu/SmartDine.Menu.API"
RUN dotnet build "SmartDine.Menu.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Menu.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Menu.API.dll"]
