# ------------------------------------------------------------
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
# ------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 3000 9988

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet restore workflow-gen/*.csproj 
RUN dotnet publish workflow-gen/*.csproj -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=build-env /out ./
ENTRYPOINT ["dotnet", "workflow-gen.dll"]