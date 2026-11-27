import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { respuestaInterceptor } from './core/api/respuesta-interceptor';
import { authInterceptor } from './core/auth/auth-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // Orden: autenticación y errores (snackbar) → desenvolver ResponseData
    provideHttpClient(withInterceptors([authInterceptor, respuestaInterceptor])),
  ],
};
