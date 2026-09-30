import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { DataFlowPanelComponent } from './data-flow-panel.component';
import { provideTestStores } from '../../../testing/test-helpers';
import { provideTestIcons } from '../../../testing/provide-icons';

@Component({
  imports: [DataFlowPanelComponent],
  template: '<emu-data-flow-panel />',
})
class HostComponent {}

describe('DataFlowPanelComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideTestStores(), provideTestIcons()],
    });
  });

  it('creates and renders without throwing', () => {
    const fixture = TestBed.createComponent(HostComponent);
    expect(() => fixture.detectChanges()).not.toThrow();
  });
});
