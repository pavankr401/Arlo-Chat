FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/Arlo_chat.Api/Arlo_chat.Api.csproj backend/Arlo_chat.Api/
RUN dotnet restore backend/Arlo_chat.Api/Arlo_chat.Api.csproj

COPY backend/Arlo_chat.Api/ backend/Arlo_chat.Api/
RUN dotnet publish backend/Arlo_chat.Api/Arlo_chat.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Arlo_chat.Api.dll"]
