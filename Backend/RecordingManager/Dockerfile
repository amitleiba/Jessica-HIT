FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RecordingManager/RecordingManager.csproj", "RecordingManager/"]
COPY ["Common/Common.csproj", "Common/"]
COPY ["Aspire/Aspire.ServiceDefaults/Aspire.ServiceDefaults.csproj", "Aspire/Aspire.ServiceDefaults/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["Aspire/Directory.Packages.props", "Aspire/"]
RUN dotnet restore "RecordingManager/RecordingManager.csproj"
COPY . .
WORKDIR "/src/RecordingManager"
RUN dotnet build "RecordingManager.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RecordingManager.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RecordingManager.dll"]

