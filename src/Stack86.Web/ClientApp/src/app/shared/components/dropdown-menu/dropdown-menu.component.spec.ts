import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { DropdownMenuComponent } from './dropdown-menu.component';
import { DropdownMenuItem } from './dropdown-menu.models';
import { provideTestIcons } from '../../../testing/provide-icons';

const items: DropdownMenuItem[] = [
  { value: 'a', label: 'Alpha' },
  { value: 'b', label: 'Beta' },
  { value: 'c', label: 'Gamma', disabled: true },
];

describe('DropdownMenuComponent', () => {
  function create(): ReturnType<typeof TestBed.createComponent<DropdownMenuComponent>> {
    TestBed.configureTestingModule({
      imports: [DropdownMenuComponent],
      providers: [provideTestIcons()],
    });
    const fixture = TestBed.createComponent(DropdownMenuComponent);
    fixture.componentRef.setInput('label', 'Pick');
    fixture.componentRef.setInput('items', items);
    fixture.detectChanges();
    return fixture;
  }

  it('emits itemSelected with the item value when an item is clicked', () => {
    const fixture = create();
    let received: string | null = null;
    fixture.componentInstance.itemSelected.subscribe((v: string) => (received = v));

    // Open the menu first via the trigger.
    const trigger: HTMLElement = fixture.nativeElement.querySelector('[role="button"], button');
    trigger?.click();
    fixture.detectChanges();

    const buttons: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('button, [role="menuitem"]'));
    const alpha = buttons.find((b) => b.textContent?.includes('Alpha'));
    expect(alpha).toBeTruthy();
    alpha?.click();
    expect(received).toBe('a');
  });

  it('closes the menu on Escape key', () => {
    const fixture = create();
    const cmp = fixture.componentInstance as unknown as { isOpen: { set: (v: boolean) => void; (): boolean } };
    cmp.isOpen.set(true);
    fixture.componentInstance.onEscape();
    expect(cmp.isOpen()).toBe(false);
  });

  it('closes the menu when clicking outside', () => {
    const fixture = create();
    const cmp = fixture.componentInstance as unknown as { isOpen: { set: (v: boolean) => void; (): boolean } };
    cmp.isOpen.set(true);
    const outside = document.createElement('div');
    document.body.appendChild(outside);
    fixture.componentInstance.onDocumentClick({ target: outside } as unknown as MouseEvent);
    expect(cmp.isOpen()).toBe(false);
    outside.remove();
  });
});
