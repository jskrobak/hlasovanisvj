# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY hlasovanisvj/hlasovanisvj.csproj hlasovanisvj/
RUN dotnet restore hlasovanisvj/hlasovanisvj.csproj
COPY hlasovanisvj/ hlasovanisvj/
# Bez --no-restore: SDK přidává balíček s _framework/blazor.web.js podle .razor souborů,
# které při prvním restore (jen .csproj kvůli cache vrstvy) ještě nejsou zkopírované.
RUN dotnet publish hlasovanisvj/hlasovanisvj.csproj -c Release -o /app

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Písma pro PDF (Liberation Sans = metrická náhrada Arialu) – Linux Arial nemá.
RUN apt-get update \
 && apt-get install -y --no-install-recommends fonts-liberation fonts-dejavu-core fontconfig \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /keys && chown app:app /keys
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    DataProtection__KeysPath=/keys \
    TZ=Europe/Prague
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "hlasovanisvj.dll"]
