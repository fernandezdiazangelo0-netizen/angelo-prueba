# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SexShopAPI/SexShopAPI.csproj", "SexShopAPI/"]
RUN dotnet restore "SexShopAPI/SexShopAPI.csproj"
COPY . .
WORKDIR "/src/SexShopAPI"
RUN dotnet build "SexShopAPI.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "SexShopAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SexShopAPI.dll"]
