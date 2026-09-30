import { provideAppInitializer, inject } from '@angular/core';
import { IconDefinitionService } from '../core/icons/icon-definition.service';

/**
 * Provides Font Awesome icon registration for component tests.
 * Add to TestBed providers when testing components that use fa-icon.
 */
export function provideTestIcons() {
  return provideAppInitializer(() => inject(IconDefinitionService).initialize());
}
