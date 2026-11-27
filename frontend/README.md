# Talí · Frontend

Aplicación Angular 22 con Angular Material 3 de **Talí · Ecosistema Digital Scout**.

## Comandos

```bash
npm install        # instalar dependencias (la primera vez)
npm start          # servidor de desarrollo en http://localhost:4200
npx ng build       # compilar para producción (carpeta dist/)
npx ng test        # pruebas unitarias (Vitest)
```

En desarrollo, las llamadas a `/api` se reenvían a la API en `http://localhost:5095` (ver `proxy.conf.json`). Para levantar la API y la base de datos, consulta el [README principal](../README.md).

## Estructura

```
src/app/core/       Servicios únicos: autenticación, interceptor HTTP, notificaciones
src/app/layout/     Shell: barra superior y menú lateral
src/app/features/   Un módulo por carpeta (login, inicio, miembros, inventario, finanzas, portal)
src/app/shared/     Componentes reutilizables
public/             Logo, favicon y fondos del login
```

Antes de modificar la interfaz, revisa las [convenciones del proyecto](../docs/convenciones.md).
