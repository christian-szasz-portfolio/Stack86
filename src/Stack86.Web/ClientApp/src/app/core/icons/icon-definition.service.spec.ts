import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { FaIconLibrary } from '@fortawesome/angular-fontawesome';
import { faPlay } from '@fortawesome/free-solid-svg-icons';
import { IconDefinitionService } from './icon-definition.service';

describe('IconDefinitionService', () => {
  it('registers the icons consumed by the app', () => {
    TestBed.configureTestingModule({});
    const service = TestBed.inject(IconDefinitionService);
    const library = TestBed.inject(FaIconLibrary);

    service.initialize();

    const icon = library.getIconDefinition('fas', faPlay.iconName);
    expect(icon).toBeTruthy();
    expect(icon?.iconName).toBe('play');
  });

  it('registers brand icons', () => {
    TestBed.configureTestingModule({});
    const service = TestBed.inject(IconDefinitionService);
    const library = TestBed.inject(FaIconLibrary);

    service.initialize();

    expect(library.getIconDefinition('fab', 'js')).toBeTruthy();
  });
});
