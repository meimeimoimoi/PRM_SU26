FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

#Copy shared layers
COPY ["src/Shared/SmartDine.Domain/SmartDine.Domain.csproj", "Shared/SmartDine.Domain/"]
COPY ["src/Shared/SmartDine.Application/SmartDine.Application.csproj", "Shared/SmartDine.Application/"]
COPY ["src/Shared/SmartDine.Infrastructure/SmartDine.Infrastructure.csproj", "Shared/SmartDine.Infrastructure/"]
# Copy service
COPY ["src/Services/Table/SmartDine.Table.API/SmartDine.Table.API.csproj", "Services/Table/SmartDine.Table.API/"]
RUN dotnet restore "SmartDine.Table.API/SmartDine.Table.API.csproj"

#Copy all source
COPY src/ .
WORKDIR "/src/Services/Table/SmartDine.Table.API"
RUN dotnet build "SmartDine.Table.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Table.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Table.API.dll"]
