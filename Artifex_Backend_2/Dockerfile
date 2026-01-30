# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["Artifex_Backend_2/Artifex_Backend_2.csproj", "Artifex_Backend_2/"]
RUN dotnet restore "Artifex_Backend_2/Artifex_Backend_2.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/Artifex_Backend_2"

# Build and Publish the release version
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# FIX: Change the URL binding to 0.0.0.0
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# EXPOSE is good practice to tell Render which port to map
EXPOSE 8080

ENTRYPOINT ["dotnet", "Artifex_Backend_2.dll"]
