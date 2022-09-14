# syntax=docker/dockerfile:1

FROM node:16.13.1
ARG PORT
ENV NODE_ENV=production

WORKDIR /app

COPY ["package.json", "yarn.lock", "./"]

RUN yarn install --frozen-lockfile
RUN yarn global add typescript

COPY . .

RUN tsc --project tsconfig.json

RUN echo ${PORT}
EXPOSE  ${PORT}
ENV PORT ${PORT}

ENTRYPOINT [ "node", "build/index.js" ]