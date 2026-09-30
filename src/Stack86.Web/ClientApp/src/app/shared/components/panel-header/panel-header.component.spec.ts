import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { PanelHeaderComponent } from './panel-header.component';
import { provideTestIcons } from '../../../testing/provide-icons';

describe('PanelHeaderComponent', () => {
  it('renders the title text', () => {
    TestBed.configureTestingModule({
      imports: [PanelHeaderComponent],
      providers: [provideTestIcons()],
    });
    const fixture = TestBed.createComponent(PanelHeaderComponent);
    fixture.componentRef.setInput('title', 'Memory');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Memory');
  });
});
