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
COPY ["src/Services/Order/SmartDine.Order.API/SmartDine.Order.API.csproj", "Services/Order/SmartDine.Order.API/"]
RUN dotnet restore "SmartDine.Order.API/SmartDine.Order.API.csproj"

#Copyy all source
COPY src/ .
WORKDIR "/src/Services/Order/SmartDine.Order.API"
RUN dotnet build "SmartDine.Order.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartDine.Order.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartDine.Order.API.dll"]
