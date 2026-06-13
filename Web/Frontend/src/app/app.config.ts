import { ApplicationConfig, isDevMode } from "@angular/core";
import { provideRouter } from "@angular/router";
import { provideHttpClient, withInterceptorsFromDi, withInterceptors } from "@angular/common/http";
import { provideStore } from "@ngrx/store";
import { provideEffects } from "@ngrx/effects";
import { provideStoreDevtools } from "@ngrx/store-devtools";
import { provideAnimationsAsync } from "@angular/platform-browser/animations/async";
import { providePrimeNG } from "primeng/config";
import { MessageService } from "primeng/api";
import Aura from "@primeng/themes/aura";
import { routes } from "./app.routes";
import { appReducer } from "./store";
import { AuthEffects } from "./store/effects/auth.effects";
import { CarEffects } from "./store/effects/car.effects";
import { RecordingEffects } from "./store/effects/recording.effects";

import { authInterceptor } from "./core/interceptors/auth.interceptor";

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi(), withInterceptors([authInterceptor])),
    provideStore(appReducer),
    provideEffects([AuthEffects, CarEffects, RecordingEffects]),  // Register Auth + Car + Recording Effects
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: Aura,
        options: {
          darkModeSelector: ".app-dark-mode",
          cssLayer: false,
        },
      },
    }),
    provideStoreDevtools({
      maxAge: 25,
      logOnly: !isDevMode(),
      autoPause: true,
      trace: true,
      traceLimit: 75,
    }),
    MessageService,
  ],
};
