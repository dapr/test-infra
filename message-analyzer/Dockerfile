# ------------------------------------------------------------
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
# -

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet restore message-analyzer/*.csproj 
RUN dotnet publish message-analyzer/*.csproj -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=build-env /out ./
ENTRYPOINT ["dotnet", "message-analyzer.dll"]