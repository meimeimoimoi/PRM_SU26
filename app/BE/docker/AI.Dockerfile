FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/SmartDine.AI.API/SmartDine.AI.API.csproj", "Services/SmartDine.AI.API/"]
COPY ["Shared/SmartDine.Application/SmartDine.Application.csproj", "Shared/SmartDine.Application/"]
COPY ["Shared/SmartDine.Domain/SmartDine.Domain.csproj", "Shared/SmartDine.Domain/"]
COPY ["Shared/SmartDine.Infrastructure/SmartDine.Infrastructure.csproj", "Shared/SmartDine.Infrastructure/"]
RUN dotnet restore "Services/SmartDine.AI.API/SmartDine.AI.API.csproj"
COPY . .
WORKDIR "/src/Services/SmartDine.AI.API"
RUN dotnet build "SmartDine.AI.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.AI.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.AI.API.dll"]
