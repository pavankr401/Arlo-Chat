import { APP_INITIALIZER, ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { authErrorInterceptor } from './interceptors/auth-error.interceptor';
import { AuthService } from './services/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(
      withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-CSRF-Token' }),
      withInterceptors([authErrorInterceptor])
    ),
    {
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService) => () => firstValueFrom(authService.bootstrap()),
      deps: [AuthService],
      multi: true
    }
  ]
};
