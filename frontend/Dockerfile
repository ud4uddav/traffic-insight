FROM node:22-alpine AS build
WORKDIR /app

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run test && npm run lint && npm run build

FROM nginxinc/nginx-unprivileged:alpine AS runtime
COPY --from=build --chown=101:101 /app/dist /usr/share/nginx/html
COPY frontend/nginx.conf.template /etc/nginx/templates/nginx.conf.template
COPY --chmod=755 frontend/docker-entrypoint.sh /usr/local/bin/traffic-insight-entrypoint

ENV API_UPSTREAM=http://api:8080
EXPOSE 8080

ENTRYPOINT ["/usr/local/bin/traffic-insight-entrypoint"]
CMD ["nginx", "-c", "/tmp/nginx.conf", "-g", "daemon off;"]
