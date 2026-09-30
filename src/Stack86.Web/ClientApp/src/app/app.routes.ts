import { Routes } from '@angular/router';

export const appRoutes: Routes = [
  {
    path: 'compiler',
    loadChildren: () =>
      import('./features/compiler/compiler.routes').then(
        (m) => m.compilerRoutes,
      ),
  },
  {
    path: '8086-emulator',
    loadChildren: () =>
      import('./features/assembler/assembler.routes').then(
        (m) => m.assemblerRoutes,
      ),
  },
  {
    path: '',
    redirectTo: '8086-emulator',
    pathMatch: 'full',
  },
  {
    path: '**',
    redirectTo: '8086-emulator',
  },
];
