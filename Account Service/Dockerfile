FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src
COPY ["Account Service.csproj", "."]
RUN dotnet restore "Account Service.csproj"
COPY . .
RUN dotnet publish "Account Service.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN apt-get update && apt-get install -y curl
COPY cert.pfx /app/cert.pfx
EXPOSE 7299
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=password
ENTRYPOINT ["dotnet", "Account Service.dll"]