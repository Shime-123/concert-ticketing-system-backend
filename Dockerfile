# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the csproj and restore dependencies
COPY ["concert-backend.csproj", "./"]
RUN dotnet restore "concert-backend.csproj"

# Copy the rest of the code and build
COPY . .
RUN dotnet publish "concert-backend.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Render uses port 10000 by default
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# This must match your output assembly name
ENTRYPOINT ["dotnet", "concert-backend.dll"]