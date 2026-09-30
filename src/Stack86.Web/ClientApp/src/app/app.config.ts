import { ApplicationConfig, ErrorHandler, inject, provideAppInitializer } from '@angular/core';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { appRoutes } from './app.routes';
import { compilerReducer } from './state/compiler.reducer';
import { CompilerEffects } from './state/compiler.effects';
import { COMPILER_FEATURE_KEY } from './state/compiler.state';
import { IconDefinitionService } from './core/icons';
import { errorInterceptor } from './core/errors/error.interceptor';
import { GlobalErrorHandler } from './core/errors/global-error.handler';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(appRoutes),
    provideHttpClient(withInterceptors([errorInterceptor])),
    provideStore({ [COMPILER_FEATURE_KEY]: compilerReducer }),
    provideEffects(CompilerEffects),
    provideStoreDevtools({ maxAge: 50 }),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideAppInitializer(() => inject(IconDefinitionService).initialize()),
  ],
};
