# Multi-stage build for SvelteKit static site served by Nginx (production)

FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies first (better layer caching)
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci || npm install

# Copy the rest of the app
COPY frontend/ ./

# Build the app (adapter-static outputs to build/)
RUN npm run build

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/build ./
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

