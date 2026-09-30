import { Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { DebuggerPanelBase } from './debugger-panel.base';
import { EmulatorFacade } from '../../../state/emulator.facade';
import { provideTestStores } from '../../../testing/test-helpers';

@Component({
  template: '',
})
class TestPanelComponent extends DebuggerPanelBase {
  public override readonly panelTitle = 'Test Panel';

  public exposeFacade(): EmulatorFacade {
    // protected member access via a public surface — typed, no cast required
    return inject(EmulatorFacade);
  }
}

describe('DebuggerPanelBase', () => {
  it('subclass receives the EmulatorFacade and exposes a panelTitle', () => {
    TestBed.configureTestingModule({
      providers: [provideTestStores()],
    });
    const fixture = TestBed.createComponent(TestPanelComponent);
    expect(fixture.componentInstance.panelTitle).toBe('Test Panel');
  });
});
