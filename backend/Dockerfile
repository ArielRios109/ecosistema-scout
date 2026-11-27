# API Ecosistema Scout (.NET 10) — imagen en dos etapas: compilación y ejecución.

# ----- Etapa 1: compilar y publicar -----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /src

# Restaurar primero solo con los .csproj para aprovechar la caché de Docker
COPY EcosistemaScout.slnx ./
COPY src/EcosistemaScout.Api/EcosistemaScout.Api.csproj src/EcosistemaScout.Api/
RUN dotnet restore src/EcosistemaScout.Api/EcosistemaScout.Api.csproj

COPY src/ src/
RUN dotnet publish src/EcosistemaScout.Api/EcosistemaScout.Api.csproj -c Release -o /app --no-restore

# ----- Etapa 2: imagen liviana solo con el runtime -----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS ejecucion
WORKDIR /app
COPY --from=compilacion /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Carpeta de archivos subidos (fotos de bienes); en Docker Compose se monta un volumen aquí.
# Se crea con dueño "app" para que el usuario sin privilegios pueda escribir en ella.
RUN mkdir -p /app/archivos && chown app:app /app/archivos

# Usuario sin privilegios incluido en la imagen oficial
USER app
ENTRYPOINT ["dotnet", "EcosistemaScout.Api.dll"]
