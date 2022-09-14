ARG NET_IMAGE=6.0-bullseye-slim

FROM mcr.microsoft.com/dotnet/aspnet:${NET_IMAGE} AS base
ARG PORT
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:${NET_IMAGE} AS build
WORKDIR /src

# COPY ["Libs/Common/Repo/", "Libs/Common/Repo/"]
# COPY ["Libs/Common/DaprActor/", "Libs/Common/DaprActor/"]
# COPY ["Libs/Common/DaprAPI/", "Libs/Common/DaprAPI/"]
# COPY ["Libs/Common/DaprState/", "Libs/Common/DaprState/"]
# COPY ["Libs/Common/Utils/", "Libs/Common/Utils/"]
# COPY ["Libs/Scrapper/ScrapperModels/", "Libs/Scrapper/ScrapperModels/"]
# COPY ["Libs/Scrapper/ScrapperRepo/", "Libs/Scrapper/ScrapperRepo/"]
COPY ["Libs/", "./Libs/"]
COPY ["ScrapperAPI/", "ScrapperAPI/"]
COPY ["Eth.sln", "Eth.sln"]
RUN dotnet restore ScrapperAPI

FROM build AS publish
RUN dotnet publish --no-restore -c Release -o /app/publish ScrapperAPI

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN echo ${PORT}
EXPOSE  ${PORT}
ENV PORT ${PORT}

ENTRYPOINT ["dotnet", "ScrapperAPI.dll"]

