import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { StatusBadgeComponent } from './status-badge.component';

describe('StatusBadgeComponent', () => {
  function create(): ReturnType<typeof TestBed.createComponent<StatusBadgeComponent>> {
    TestBed.configureTestingModule({ imports: [StatusBadgeComponent] });
    return TestBed.createComponent(StatusBadgeComponent);
  }

  it('renders the status text', () => {
    const fixture = create();
    fixture.componentRef.setInput('status', 'running');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('running');
  });

  it('applies a CSS class derived from the status value', () => {
    const fixture = create();
    fixture.componentRef.setInput('status', 'halted');
    fixture.detectChanges();
    expect(fixture.nativeElement.innerHTML).toContain('status-badge--halted');
  });
});
