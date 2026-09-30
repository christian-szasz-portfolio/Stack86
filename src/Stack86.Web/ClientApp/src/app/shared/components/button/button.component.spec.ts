import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { ButtonComponent, ButtonVariant, TooltipPosition } from './button.component';
import { provideTestIcons } from '../../../testing/provide-icons';

describe('ButtonComponent', () => {
  function create(): { fixture: ReturnType<typeof TestBed.createComponent<ButtonComponent>>; component: ButtonComponent } {
    TestBed.configureTestingModule({
      imports: [ButtonComponent],
      providers: [provideTestIcons()],
    });
    const fixture = TestBed.createComponent(ButtonComponent);
    return { fixture, component: fixture.componentInstance };
  }

  it('emits pressed when clicked and not disabled', () => {
    const { fixture, component } = create();
    let count = 0;
    component.pressed.subscribe(() => count++);
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    expect(count).toBe(1);
  });

  it('does not emit when disabled', () => {
    const { fixture, component } = create();
    fixture.componentRef.setInput('disabled', true);
    let count = 0;
    component.pressed.subscribe(() => count++);
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    expect(count).toBe(0);
  });

  it('does not emit when loading', () => {
    const { fixture, component } = create();
    fixture.componentRef.setInput('loading', true);
    let count = 0;
    component.pressed.subscribe(() => count++);
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    expect(count).toBe(0);
  });

  it('reflects variant via host attribute', () => {
    const { fixture } = create();
    fixture.componentRef.setInput('variant', ButtonVariant.Primary);
    fixture.detectChanges();
    expect(fixture.nativeElement.getAttribute('variant')).toBe('primary');
  });

  it('exposes the TooltipPosition enum to the template', () => {
    const { component } = create();
    const proto = component as unknown as { TooltipPosition: typeof TooltipPosition };
    expect(proto.TooltipPosition).toBe(TooltipPosition);
  });
});
