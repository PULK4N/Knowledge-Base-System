FROM node:24.15.0-alpine AS dependencies

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

FROM dependencies AS source

COPY . .

FROM source AS test

RUN npm test

FROM source AS build

RUN npm run build

FROM nginx:1.28-alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/knowledge-base-spa/browser /usr/share/nginx/html

EXPOSE 80
