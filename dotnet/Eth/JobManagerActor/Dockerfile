ARG NET_IMAGE=6.0-bullseye-slim

FROM mcr.microsoft.com/dotnet/aspnet:${NET_IMAGE} AS base
ARG PORT
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:${NET_IMAGE} AS build
WORKDIR /src

# COPY ["Libs/Common/DaprActor/", "Libs/Common/DaprActor/"]
# COPY ["Libs/Scrapper/ScrapperModels/", "Libs/Scrapper/ScrapperModels/"]
COPY ["Libs/", "./Libs/"]
COPY ["JobManagerActor/", "JobManagerActor/"]
COPY ["Eth.sln", "Eth.sln"]
RUN dotnet restore JobManagerActor

FROM build AS publish
RUN dotnet publish --no-restore -c Release -o /app/publish JobManagerActor

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN echo ${PORT}
EXPOSE  ${PORT}
ENV PORT ${PORT}

ENTRYPOINT ["dotnet", "JobManagerActor.dll"]

