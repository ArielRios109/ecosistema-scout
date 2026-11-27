import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { respuestaInterceptor } from './core/api/respuesta-interceptor';
import { authInterceptor } from './core/auth/auth-interceptor';
import { cargaInterceptor } from './core/carga/carga-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // Orden: spinner global → autenticación y errores (snackbar) → desenvolver ResponseData
    provideHttpClient(withInterceptors([cargaInterceptor, authInterceptor, respuestaInterceptor])),
  ],
};
