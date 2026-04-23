# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebHS.csproj", "."]
RUN dotnet restore "./WebHS.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "WebHS.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebHS.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebHS.dll"]
