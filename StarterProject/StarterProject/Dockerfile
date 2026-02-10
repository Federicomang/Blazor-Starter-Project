# Base image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# Install tzdata (required for timezone support)
USER root
RUN apt-get update && apt-get install -y tzdata

# Set the timezone
ENV TZ=Europe/Rome

# Switch back to original user
USER $APP_UID

WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["StarterProject/StarterProject/StarterProject.csproj", "StarterProject/StarterProject/"]
COPY ["StarterProject/StarterProject.Client/StarterProject.Client.csproj", "StarterProject/StarterProject.Client/"]
RUN dotnet restore "./StarterProject/StarterProject/StarterProject.csproj"
COPY . .
WORKDIR "/src/StarterProject/StarterProject"
RUN dotnet build "./StarterProject.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./StarterProject.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StarterProject.dll"]
