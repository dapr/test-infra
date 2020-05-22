# ------------------------------------------------------------
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
# ------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet restore snapshot/*.csproj 
RUN dotnet publish snapshot/*.csproj -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=build-env /out ./
ENTRYPOINT ["dotnet", "snapshot.dll"]