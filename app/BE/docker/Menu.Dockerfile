FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Services/SmartDine.Menu.API/SmartDine.Menu.API.csproj", "Services/SmartDine.Menu.API/"]
COPY ["Shared/SmartDine.Application/SmartDine.Application.csproj", "Shared/SmartDine.Application/"]
COPY ["Shared/SmartDine.Domain/SmartDine.Domain.csproj", "Shared/SmartDine.Domain/"]
COPY ["Shared/SmartDine.Infrastructure/SmartDine.Infrastructure.csproj", "Shared/SmartDine.Infrastructure/"]
RUN dotnet restore "Services/SmartDine.Menu.API/SmartDine.Menu.API.csproj"
COPY . .
WORKDIR "/src/Services/SmartDine.Menu.API"
RUN dotnet build "SmartDine.Menu.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Menu.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Menu.API.dll"]
