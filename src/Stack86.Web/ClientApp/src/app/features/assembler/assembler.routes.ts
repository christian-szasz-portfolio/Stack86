import { Routes } from '@angular/router';
import { provideState } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { LayoutComponent } from './layout/layout.component';
import { EMULATOR_FEATURE_KEY } from '@state/emulator.state';
import { emulatorReducer } from '@state/emulator.reducer';
import { EmulatorEffects } from '@state/emulator.effects';

export const assemblerRoutes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    providers: [
      provideState(EMULATOR_FEATURE_KEY, emulatorReducer),
      provideEffects(EmulatorEffects),
    ],
  },
];
