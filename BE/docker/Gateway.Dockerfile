FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/Gateway/SmartDine.Gateway/SmartDine.Gateway.csproj", "Services/Gateway/SmartDine.Gateway/"]
RUN dotnet restore "Services/Gateway/SmartDine.Gateway/SmartDine.Gateway.csproj"
COPY src/ .
WORKDIR "/src/Services/Gateway/SmartDine.Gateway"
RUN dotnet build "SmartDine.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Gateway.dll"]
