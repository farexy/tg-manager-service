#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["TG.Manager.Service/TG.Manager.Service.csproj", "TG.Manager.Service/"]
COPY nuget.config .
RUN dotnet restore "TG.Manager.Service/TG.Manager.Service.csproj"
COPY . .
WORKDIR "/src/TG.Manager.Service"
RUN dotnet build "TG.Manager.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TG.Manager.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TG.Manager.Service.dll"]