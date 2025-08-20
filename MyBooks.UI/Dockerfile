# ===== build (Debian base avoids Alpine/native module issues) =====
FROM node:20-bookworm-slim AS build
WORKDIR /app

# Helpful for larger Angular builds
ENV NODE_OPTIONS=--max_old_space_size=4096
ENV npm_config_fund=false npm_config_audit=false

# Install deps
COPY package*.json ./
RUN npm ci

# Copy workspace & sanity check
COPY . .
RUN test -f angular.json || (echo "angular.json missing in /app" && ls -la && exit 1)
RUN npx ng version

# Build (your script is "build": "ng build")
RUN npm run build -- --configuration=production

# Show what got built so we can confirm paths in the logs
RUN echo "=== dist ===" && ls -la dist && echo "=== dist/my-books.ui ===" && ls -la dist/my-books.ui || true

# ===== runtime =====
FROM nginx:alpine
# Your outputPath is dist/my-books.ui (no /browser)
COPY --from=build /app/dist/my-books.ui /usr/share/nginx/html

# SPA routing
RUN printf "server { \
    listen 80; \
    server_name _; \
    root /usr/share/nginx/html; \
    include /etc/nginx/mime.types; \
    location / { try_files \$uri \$uri/ /index.html; } \
}" > /etc/nginx/conf.d/default.conf

EXPOSE 80
