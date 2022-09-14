# syntax=docker/dockerfile:1

# Stage 1 - the build process
FROM node:16.13.1 as build-deps
ARG PORT
ENV NODE_ENV=production

WORKDIR /app

COPY ["package.json", "yarn.lock", "./"]

RUN yarn install --frozen-lockfile

COPY . .

RUN yarn build

# Stage 2 - the production environment
FROM nginx:1.23-alpine
COPY --from=build-deps /app/out /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
