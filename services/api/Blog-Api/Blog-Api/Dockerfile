FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
# Precreate the data protection keys folder inside user home directory
RUN mkdir -p /home/app/keys
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Blog-Api/Blog-Api.csproj", "Blog-Api/"]
COPY ["Blog-Api.Contracts/Blog-Api.Contracts.csproj", "Blog-Api.Contracts/"]
RUN dotnet restore "Blog-Api/Blog-Api.csproj"
COPY . .
WORKDIR "/src/Blog-Api"
RUN dotnet build "./Blog-Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Blog-Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Blog-Api.dll"]
