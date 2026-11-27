# Frontend Ecosistema Scout (Angular 22) — compila la aplicación y la sirve con nginx.

# ----- Etapa 1: compilar -----
FROM node:22-alpine AS compilacion
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npx ng build --configuration production

# ----- Etapa 2: servir archivos estáticos -----
FROM nginx:1.29-alpine AS ejecucion
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=compilacion /app/dist/frontend/browser /usr/share/nginx/html
EXPOSE 80
