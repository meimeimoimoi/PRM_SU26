FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SmartDine.Order.API/SmartDine.Order.API.csproj", "SmartDine.Order.API/"]
COPY ["SmartDine.Application/SmartDine.Application.csproj", "SmartDine.Application/"]
COPY ["SmartDine.Domain/SmartDine.Domain.csproj", "SmartDine.Domain/"]
COPY ["SmartDine.Infrastructure/SmartDine.Infrastructure.csproj", "SmartDine.Infrastructure/"]
RUN dotnet restore "SmartDine.Order.API/SmartDine.Order.API.csproj"
COPY . .
WORKDIR "/src/SmartDine.Order.API"
RUN dotnet build "SmartDine.Order.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Order.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Order.API.dll"]
