FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SmartDine.Identity.API/SmartDine.Identity.API.csproj", "SmartDine.Identity.API/"]
COPY ["SmartDine.Application/SmartDine.Application.csproj", "SmartDine.Application/"]
COPY ["SmartDine.Domain/SmartDine.Domain.csproj", "SmartDine.Domain/"]
COPY ["SmartDine.Infrastructure/SmartDine.Infrastructure.csproj", "SmartDine.Infrastructure/"]
RUN dotnet restore "SmartDine.Identity.API/SmartDine.Identity.API.csproj"
COPY . .
WORKDIR "/src/SmartDine.Identity.API"
RUN dotnet build "SmartDine.Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Identity.API.dll"]
