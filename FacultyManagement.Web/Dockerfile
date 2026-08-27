FROM node:24-alpine AS build
WORKDIR /app
COPY FacultyManagement.Web/package.json FacultyManagement.Web/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY FacultyManagement.Web/ ./
RUN npm run build

FROM nginx:1.29-alpine AS final
COPY FacultyManagement.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s --retries=3 CMD wget -q -O /dev/null http://localhost/ || exit 1
