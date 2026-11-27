FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Copy solution & csproj trước để cache NuGet layer
COPY ["EcommercePlatform.sln", "./"]
COPY ["src/EcommercePlatform.Domain/EcommercePlatform.Domain.csproj", "src/EcommercePlatform.Domain/"]
COPY ["src/EcommercePlatform.Application/EcommercePlatform.Application.csproj", "src/EcommercePlatform.Application/"]
COPY ["src/EcommercePlatform.Infrastructure/EcommercePlatform.Infrastructure.csproj", "src/EcommercePlatform.Infrastructure/"]
COPY ["src/EcommercePlatform.Api/EcommercePlatform.Api.csproj", "src/EcommercePlatform.Api/"]

RUN dotnet restore "src/EcommercePlatform.Api/EcommercePlatform.Api.csproj"

# 2. Copy toàn bộ mã nguồn src và compile publish
COPY src/ src/
RUN dotnet publish "src/EcommercePlatform.Api/EcommercePlatform.Api.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# 3. Runtime Image .NET 10 siêu nhẹ
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EcommercePlatform.Api.dll"]
