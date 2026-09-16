# Build and run the web app. Build context is the repository root:
#   docker build -f src/FolketingetVotes.Web/Dockerfile -t folketinget-web .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/FolketingetVotes.Core/FolketingetVotes.Core.csproj src/FolketingetVotes.Core/
COPY src/FolketingetVotes.Data/FolketingetVotes.Data.csproj src/FolketingetVotes.Data/
COPY src/FolketingetVotes.Web/FolketingetVotes.Web.csproj src/FolketingetVotes.Web/
RUN dotnet restore src/FolketingetVotes.Web/FolketingetVotes.Web.csproj
COPY src/ src/
RUN dotnet publish src/FolketingetVotes.Web/FolketingetVotes.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "FolketingetVotes.Web.dll"]
