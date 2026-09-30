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
    path: 'assembler',
    loadChildren: () =>
      import('./features/assembler/assembler.routes').then(
        (m) => m.assemblerRoutes,
      ),
  },
  {
    path: '',
    redirectTo: 'assembler',
    pathMatch: 'full',
  },
  {
    path: '**',
    redirectTo: 'assembler',
  },
];
