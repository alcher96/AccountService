FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AccountService.csproj", "."]
RUN dotnet restore "AccountService.csproj"
COPY . .
RUN dotnet publish "AccountService.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY cert.pfx /app/cert.pfx
EXPOSE 80
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=password
ENTRYPOINT ["dotnet", "AccountService.dll"]