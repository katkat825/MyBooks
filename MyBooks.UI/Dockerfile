# ===== Stage 1: Build Angular app =====
FROM node:20-bookworm-slim AS build
WORKDIR /app

# increase memory limit for Angular builds
ENV NODE_OPTIONS=--max_old_space_size=4096
ENV npm_config_fund=false npm_config_audit=false

# install dependencies
COPY package*.json ./
RUN npm ci

# copy source and build
COPY . .
RUN npm run build -- --configuration=production

# ===== Stage 2: Nginx HTTPS runtime =====
FROM nginx:alpine

# install openssl and generate self-signed cert for https://localhost
RUN apk add --no-cache openssl && \
    mkdir -p /etc/ssl/private && \
    openssl req -x509 -nodes -days 365 \
      -subj "/CN=localhost" \
      -newkey rsa:2048 \
      -keyout /etc/ssl/private/nginx-selfsigned.key \
      -out /etc/ssl/certs/nginx-selfsigned.crt

# remove default Nginx static files
RUN rm -rf /usr/share/nginx/html/*

# copy built Angular app (Angular 17+ output path)
COPY --from=build /app/dist/my-books.ui/browser /usr/share/nginx/html

# copy custom HTTPS nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
EXPOSE 443

# start Nginx in foreground
CMD ["nginx", "-g", "daemon off;"]
