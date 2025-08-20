# ===== build =====
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# adjust if your script name differs
RUN npm run build -- --configuration=production

# ===== runtime =====
FROM nginx:alpine
COPY --from=build /app/dist/* /usr/share/nginx/html
RUN printf "server { \
    listen 80; \
    server_name _; \
    root /usr/share/nginx/html; \
    include /etc/nginx/mime.types; \
    location / { try_files \$uri \$uri/ /index.html; } \
}" > /etc/nginx/conf.d/default.conf
EXPOSE 80
