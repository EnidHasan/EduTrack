FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["EduTrack.Web/EduTrack.Web.csproj", "EduTrack.Web/"]
RUN dotnet restore "EduTrack.Web/EduTrack.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/EduTrack.Web"
RUN dotnet build "EduTrack.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduTrack.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

# Render maps incoming traffic to port 8080 by default (or reads the PORT env variable). 
# .NET 8+ defaults to 8080. We make sure it uses 8080.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EduTrack.Web.dll"]
