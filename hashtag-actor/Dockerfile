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
RUN dotnet restore hashtag-actor/*.csproj 
RUN dotnet publish hashtag-actor/*.csproj -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=build-env /out ./
ENTRYPOINT ["dotnet", "hashtag-actor.dll"]