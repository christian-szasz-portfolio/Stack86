import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { SettingsDropdownComponent } from './settings-dropdown.component';
import { SettingsOption } from './settings-dropdown.models';
import { provideTestIcons } from '../../../testing/provide-icons';

const options: SettingsOption[] = [
  { label: 'Show grid', checked: true },
  { label: 'Animate', checked: false },
];

describe('SettingsDropdownComponent', () => {
  function create(): ReturnType<typeof TestBed.createComponent<SettingsDropdownComponent>> {
    TestBed.configureTestingModule({
      imports: [SettingsDropdownComponent],
      providers: [provideTestIcons()],
    });
    const fixture = TestBed.createComponent(SettingsDropdownComponent);
    fixture.componentRef.setInput('options', options);
    fixture.detectChanges();
    return fixture;
  }

  it('toggle flips the open signal', () => {
    const fixture = create();
    expect(fixture.componentInstance.isOpen()).toBe(false);
    fixture.componentInstance.toggle();
    expect(fixture.componentInstance.isOpen()).toBe(true);
    fixture.componentInstance.toggle();
    expect(fixture.componentInstance.isOpen()).toBe(false);
  });

  it('emits optionToggled with the index and checked state', () => {
    const fixture = create();
    let payload: { index: number; checked: boolean } | null = null;
    fixture.componentInstance.optionToggled.subscribe((e) => (payload = e));

    const checkbox = document.createElement('input');
    checkbox.type = 'checkbox';
    checkbox.checked = true;
    fixture.componentInstance.onCheckboxChange(1, { target: checkbox } as unknown as Event);
    expect(payload).toEqual({ index: 1, checked: true });

    checkbox.checked = false;
    fixture.componentInstance.onCheckboxChange(0, { target: checkbox } as unknown as Event);
    expect(payload).toEqual({ index: 0, checked: false });
  });

  it('closes when clicking outside while open', () => {
    const fixture = create();
    fixture.componentInstance.toggle();
    const outside = document.createElement('div');
    document.body.appendChild(outside);
    fixture.componentInstance.onDocumentClick({ target: outside } as unknown as MouseEvent);
    expect(fixture.componentInstance.isOpen()).toBe(false);
    outside.remove();
  });
});
