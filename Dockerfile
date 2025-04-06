# Fase di build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copia il file .csproj e ripristina le dipendenze
COPY *.csproj ./
RUN dotnet restore

# Copia il resto dei file sorgenti nel container e costruisci l'applicazione
COPY . ./
RUN dotnet publish -c Release -o out

# Fase di runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app
EXPOSE 80

# Copia i file pubblicati dalla fase di build
COPY --from=build /app/out .

# Comando per eseguire l'applicazione
ENTRYPOINT ["dotnet", "FarmTrackBE.dll"]
