# Build the ingestion CLI. Run it on a schedule (cron / platform cron job):
#   docker build -f src/FolketingetVotes.Ingest/Dockerfile -t folketinget-ingest .
#   docker run --rm -e ConnectionStrings__Folketinget=... folketinget-ingest sync
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/FolketingetVotes.Core/FolketingetVotes.Core.csproj src/FolketingetVotes.Core/
COPY src/FolketingetVotes.Data/FolketingetVotes.Data.csproj src/FolketingetVotes.Data/
COPY src/FolketingetVotes.Ingest/FolketingetVotes.Ingest.csproj src/FolketingetVotes.Ingest/
RUN dotnet restore src/FolketingetVotes.Ingest/FolketingetVotes.Ingest.csproj
COPY src/ src/
RUN dotnet publish src/FolketingetVotes.Ingest/FolketingetVotes.Ingest.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FolketingetVotes.Ingest.dll"]
