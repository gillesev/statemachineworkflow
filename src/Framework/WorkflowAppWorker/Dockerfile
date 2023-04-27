#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

ARG ACR={acr-name}.azurecr.io
FROM $ACR/{acr-worker-base-image}:6.0.5 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG PAT
WORKDIR /src
#COPY ["nuget.config", "."]
COPY ["WorkflowAppWorker/WorkflowAppWorker.csproj", "WorkflowAppWorker/"]
COPY ["WorkflowAppWorker.Domain/WorkflowAppWorker.Domain.csproj", "WorkflowAppWorker.Domain/"]
COPY ["WorkflowAppWorker.Core/WorkflowAppWorker.Core.csproj", "WorkflowAppWorker.Core/"]
COPY ["WorkflowAppWorker.Infrastructure/WorkflowAppWorker.Infrastructure.csproj", "WorkflowAppWorker.Infrastructure/"]

RUN dotnet nuget add source {NUGET_FEED_URL/nuget/v3/index.json} -p $PAT -n NuGet_Authed --store-password-in-clear-text
RUN dotnet restore "WorkflowAppWorker/WorkflowAppWorker.csprojj"
COPY . .
WORKDIR "/src/WorkflowAppWorker"
RUN dotnet build "WorkflowAppWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WorkflowAppWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorkflowAppWorker.dll"]
