FROM node:20-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npx ng build --configuration production

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api-build
WORKDIR /src
COPY api/ api/
RUN dotnet publish api/HackerNewsApi -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=api-build /app/publish .
COPY --from=frontend-build /src/frontend/dist/frontend/browser/ wwwroot/
ENV PORT=5000
EXPOSE 5000
ENTRYPOINT ["/bin/sh", "-c", "dotnet HackerNewsApi.dll --urls http://+:${PORT}"]
