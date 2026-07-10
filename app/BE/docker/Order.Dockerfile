FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/SmartDine.Order.API/SmartDine.Order.API.csproj", "Services/SmartDine.Order.API/"]
COPY ["Shared/SmartDine.Application/SmartDine.Application.csproj", "Shared/SmartDine.Application/"]
COPY ["Shared/SmartDine.Domain/SmartDine.Domain.csproj", "Shared/SmartDine.Domain/"]
COPY ["Shared/SmartDine.Infrastructure/SmartDine.Infrastructure.csproj", "Shared/SmartDine.Infrastructure/"]
RUN dotnet restore "Services/SmartDine.Order.API/SmartDine.Order.API.csproj"
COPY . .
WORKDIR "/src/Services/SmartDine.Order.API"
RUN dotnet build "SmartDine.Order.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Order.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Order.API.dll"]
