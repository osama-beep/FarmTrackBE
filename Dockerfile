# Fase di build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /app

# Copia i file di progetto e ripristina le dipendenze
COPY *.csproj ./
RUN dotnet restore

# Copia il resto dei file e costruisci l'applicazione
COPY . ./
RUN dotnet publish -c Release -o out

# Fase di runtime
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app
EXPOSE 80

# Copia l'applicazione compilata dalla fase di build
COPY --from=build /app/out .

# Comando per avviare l'applicazione
ENTRYPOINT ["dotnet", "FarmTrackBE.dll"]
