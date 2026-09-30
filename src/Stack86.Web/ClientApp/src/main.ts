import './styles.scss';
import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';
import { installNamespacedStorage } from './app/core/storage/namespaced-storage.utility';

// Keep every key this demo writes under one prefix, so it stays in its own contained group in the
// visitor's browser storage rather than scattering loose entries across their device.
installNamespacedStorage('stack86-demo:');

bootstrapApplication(AppComponent, appConfig).catch((err: unknown) =>
  console.error(err),
);
