import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { EmptyStateComponent } from './empty-state.component';
import { provideTestIcons } from '../../../testing/provide-icons';

describe('EmptyStateComponent', () => {
  it('renders the message', () => {
    TestBed.configureTestingModule({
      imports: [EmptyStateComponent],
      providers: [provideTestIcons()],
    });
    const fixture = TestBed.createComponent(EmptyStateComponent);
    fixture.componentRef.setInput('message', 'No items yet');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No items yet');
  });
});
