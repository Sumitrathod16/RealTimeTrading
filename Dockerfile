# Stage 1: Build the React frontend
FROM node:18-alpine AS build-frontend
WORKDIR /build
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# Stage 2: Build the ASP.NET Core backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-backend
WORKDIR /src
COPY Trading.Api/Trading.Api.csproj ./Trading.Api/
RUN dotnet restore ./Trading.Api/Trading.Api.csproj
COPY Trading.Api/ ./Trading.Api/
WORKDIR /src/Trading.Api
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Ensure data directory exists
RUN mkdir -p /app/data

# Copy backend publish output
COPY --from=build-backend /app/publish .

# Copy frontend build output to the backend wwwroot directory
COPY --from=build-frontend /build/dist ./wwwroot

# Expose HTTP port
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ConnectionStrings__TradingDb="Data Source=data/trades.db"

# Start the application
ENTRYPOINT ["dotnet", "Trading.Api.dll"]
