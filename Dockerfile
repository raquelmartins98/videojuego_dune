# Use the official .NET ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Dune.MCP.Server/Dune.MCP.Server.csproj", "Dune.MCP.Server/"]
COPY ["Dune.Domain/Dune.Domain.csproj", "Dune.Domain/"]
COPY ["Dune.Simulation.Service/Dune.Simulation.Service.csproj", "Dune.Simulation.Service/"]
COPY ["Dune.Persistence.Service/Dune.Persistence.Service.csproj", "Dune.Persistence.Service/"]
RUN dotnet restore "Dune.MCP.Server/Dune.MCP.Server.csproj"
COPY . .
WORKDIR "/src/Dune.MCP.Server"
RUN dotnet build "Dune.MCP.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Dune.MCP.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dune.MCP.Server.dll"]