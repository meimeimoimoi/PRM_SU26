FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/SmartDine.Identity.API/SmartDine.Identity.API.csproj", "Services/SmartDine.Identity.API/"]
COPY ["Shared/SmartDine.Application/SmartDine.Application.csproj", "Shared/SmartDine.Application/"]
COPY ["Shared/SmartDine.Domain/SmartDine.Domain.csproj", "Shared/SmartDine.Domain/"]
COPY ["Shared/SmartDine.Infrastructure/SmartDine.Infrastructure.csproj", "Shared/SmartDine.Infrastructure/"]
RUN dotnet restore "Services/SmartDine.Identity.API/SmartDine.Identity.API.csproj"
COPY . .
WORKDIR "/src/Services/SmartDine.Identity.API"
RUN dotnet build "SmartDine.Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Identity.API.dll"]
