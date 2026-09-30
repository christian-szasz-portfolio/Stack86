import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { MenuBarComponent } from './menu-bar.component';
import { MenuDefinition, MenuItem } from './menu-definitions';
import { provideTestIcons } from '../../../../testing/provide-icons';

interface ProtectedMenuBar {
  openMenuIndex(): number | null;
  toggleMenu(index: number): void;
  onMenuHover(index: number): void;
  onItemClick(item: MenuItem): void;
}

function asProto(c: MenuBarComponent): ProtectedMenuBar {
  return c as unknown as ProtectedMenuBar;
}

@Component({
  imports: [MenuBarComponent],
  template: `<emu-menu-bar [menus]="menus" (menuAction)="onAction($event)" />`,
})
class HostComponent {
  public actions: string[] = [];
  public menus: MenuDefinition[] = [
    {
      label: 'File',
      items: [
        { label: 'New', action: 'new' },
        { separator: true, label: '' },
        { label: 'Disabled', action: 'noop', disabled: true },
        { label: 'Save', action: 'save' },
      ],
    },
    { label: 'Edit', items: [{ label: 'Undo', action: 'undo' }] },
  ];

  public onAction(name: string): void {
    this.actions.push(name);
  }
}

describe('MenuBarComponent', () => {
  let host: HostComponent;
  let menuBar: MenuBarComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideTestIcons()],
    });
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    host = fixture.componentInstance;
    const child = fixture.debugElement.children[0];
    menuBar = child.componentInstance as MenuBarComponent;
  });

  it('toggleMenu opens then closes the same index', () => {
    const proto = asProto(menuBar);
    proto.toggleMenu(0);
    expect(proto.openMenuIndex()).toBe(0);
    proto.toggleMenu(0);
    expect(proto.openMenuIndex()).toBeNull();
  });

  it('onMenuHover switches menus only when one is already open', () => {
    const proto = asProto(menuBar);
    proto.onMenuHover(1);
    expect(proto.openMenuIndex()).toBeNull();
    proto.toggleMenu(0);
    proto.onMenuHover(1);
    expect(proto.openMenuIndex()).toBe(1);
  });

  it('onItemClick emits the action and closes the menu', () => {
    const proto = asProto(menuBar);
    proto.toggleMenu(0);
    proto.onItemClick({ label: 'Save', action: 'save' });
    expect(host.actions).toEqual(['save']);
    expect(proto.openMenuIndex()).toBeNull();
  });

  it('onItemClick ignores separators, disabled items, and items without an action', () => {
    const proto = asProto(menuBar);
    proto.toggleMenu(0);
    proto.onItemClick({ label: '', separator: true });
    proto.onItemClick({ label: 'D', action: 'x', disabled: true });
    proto.onItemClick({ label: 'No action' });
    expect(host.actions).toEqual([]);
    expect(proto.openMenuIndex()).toBe(0);
  });

  it('document click outside the host closes the menu', () => {
    const proto = asProto(menuBar);
    proto.toggleMenu(0);
    const evt = new MouseEvent('click');
    Object.defineProperty(evt, 'target', { value: document.body });
    menuBar.onDocumentClick(evt);
    expect(proto.openMenuIndex()).toBeNull();
  });

  it('escape key closes the menu', () => {
    const proto = asProto(menuBar);
    proto.toggleMenu(0);
    menuBar.onEscape();
    expect(proto.openMenuIndex()).toBeNull();
  });
});
