# Simplified Dockerfile - Frontend must be built outside Docker first!
# Run: cd frontend && npm run build

# Backend build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build

# Declare build args for GitHub authentication
ARG GITHUB_USERNAME
ARG GITHUB_TOKEN

WORKDIR /app/backend

# Copy and restore backend
COPY backend/*.csproj ./
COPY backend/nuget.config ./

# Replace placeholders in nuget.config and restore
RUN sed -i "s|%GITHUB_USERNAME%|${GITHUB_USERNAME}|g" nuget.config && \
    sed -i "s|%GITHUB_TOKEN%|${GITHUB_TOKEN}|g" nuget.config && \
    dotnet restore

# Build and publish backend
COPY backend/ ./
RUN dotnet publish -c Release -o /app/publish

# Final runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install nginx and curl
RUN apt-get update && apt-get install -y nginx curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published backend
COPY --from=backend-build /app/publish .

# Copy pre-built frontend
COPY frontend/dist ./wwwroot

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Create startup script
RUN printf '#!/bin/bash\nnginx &\nexec dotnet SfabGl07Gateway.Api.dll\n' > /app/start.sh && chmod +x /app/start.sh

EXPOSE 80 5000

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000
ENV TZ=Europe/Oslo

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 CMD curl -f http://localhost/health || exit 1

CMD ["/app/start.sh"]
