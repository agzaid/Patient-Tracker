FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PatientTracker.API/PatientTracker.API.csproj", "PatientTracker.API/"]
RUN dotnet restore "PatientTracker.API/PatientTracker.API.csproj"
COPY . .
WORKDIR "/src/PatientTracker.API"
RUN dotnet build "PatientTracker.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PatientTracker.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PatientTracker.API.dll"]
