FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SmartDine.Menu.API/SmartDine.Menu.API.csproj", "SmartDine.Menu.API/"]
COPY ["SmartDine.Application/SmartDine.Application.csproj", "SmartDine.Application/"]
COPY ["SmartDine.Domain/SmartDine.Domain.csproj", "SmartDine.Domain/"]
COPY ["SmartDine.Infrastructure/SmartDine.Infrastructure.csproj", "SmartDine.Infrastructure/"]
RUN dotnet restore "SmartDine.Menu.API/SmartDine.Menu.API.csproj"
COPY . .
WORKDIR "/src/SmartDine.Menu.API"
RUN dotnet build "SmartDine.Menu.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Menu.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Menu.API.dll"]
