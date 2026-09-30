import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { SelectComponent } from './select.component';
import { SelectOption } from './select.models';

const options: SelectOption[] = [
  { value: 'a', label: 'Apple' },
  { value: 'b', label: 'Banana' },
];

describe('SelectComponent', () => {
  function create(): ReturnType<typeof TestBed.createComponent<SelectComponent>> {
    TestBed.configureTestingModule({ imports: [SelectComponent] });
    return TestBed.createComponent(SelectComponent);
  }

  it('renders one <option> per supplied option', () => {
    const fixture = create();
    fixture.componentRef.setInput('options', options);
    fixture.detectChanges();

    const optionEls: HTMLOptionElement[] = Array.from(fixture.nativeElement.querySelectorAll('option'));
    // Implementation may include a placeholder option; assert at least the supplied count are present.
    const labels = optionEls.map((o) => o.textContent?.trim());
    expect(labels).toEqual(expect.arrayContaining(['Apple', 'Banana']));
  });

  it('emits valueChange when the user picks an option', () => {
    const fixture = create();
    fixture.componentRef.setInput('options', options);
    fixture.detectChanges();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector('select');
    let received = '';
    fixture.componentInstance.valueChange.subscribe((v) => (received = v));

    select.value = 'b';
    select.dispatchEvent(new Event('change'));
    expect(received).toBe('b');
  });
});
