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


# ===== Stage 2: Nginx runtime (HTTP only, TLS handled by host) =====
FROM nginx:alpine

# remove default Nginx static files
RUN rm -rf /usr/share/nginx/html/*

# copy built Angular app (Angular 17+ output path)
COPY --from=build /app/dist/my-books.ui/browser /usr/share/nginx/html

# use locked-down production Nginx config (from your extracted file)
COPY default.conf /etc/nginx/conf.d/default.conf
RUN chmod 444 /etc/nginx/conf.d/default.conf

# fix .mjs mime type handling
RUN echo '\
types {\n\
    text/javascript js;\n\
    application/javascript mjs;\n\
}' > /etc/nginx/conf.d/mime.types.override && \
    sed -i "/http {/a \\\tinclude /etc/nginx/conf.d/mime.types.override;" /etc/nginx/nginx.conf

EXPOSE 80

# start Nginx in foreground
CMD ["nginx", "-g", "daemon off;"]
