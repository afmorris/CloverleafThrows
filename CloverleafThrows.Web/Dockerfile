FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CloverleafThrows.Web/CloverleafThrows.Web.csproj", "CloverleafThrows.Web/"]
COPY ["CloverleafThrows.Data/CloverleafThrows.Data.csproj", "CloverleafThrows.Data/"]
COPY ["CloverleafThrows.Models/CloverleafThrows.Models.csproj", "CloverleafThrows.Models/"]
RUN dotnet restore "CloverleafThrows.Web/CloverleafThrows.Web.csproj"
COPY . .
WORKDIR "/src/CloverleafThrows.Web"
RUN dotnet build "./CloverleafThrows.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CloverleafThrows.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CloverleafThrows.Web.dll"]
