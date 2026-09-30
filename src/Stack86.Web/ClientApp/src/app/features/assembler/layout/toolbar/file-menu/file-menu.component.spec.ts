import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { FileMenuComponent, FileMenuItem } from './file-menu.component';
import { provideTestIcons } from '../../../../../testing/provide-icons';

interface ProtectedFileMenu {
  isOpen(): boolean;
  toggle(): void;
  onItemClick(item: FileMenuItem): void;
  isItemDisabled(item: FileMenuItem): boolean;
}

function asProto(c: FileMenuComponent): ProtectedFileMenu {
  return c as unknown as ProtectedFileMenu;
}

@Component({
  imports: [FileMenuComponent],
  template: `<emu-file-menu [exportDisabled]="exportDisabled" (menuAction)="onAction($event)" />`,
})
class HostComponent {
  public exportDisabled = false;
  public actions: string[] = [];

  public onAction(name: string): void {
    this.actions.push(name);
  }
}

describe('FileMenuComponent', () => {
  let host: HostComponent;
  let menu: FileMenuComponent;

  function build(exportDisabled = false): void {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideTestIcons()],
    });
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.exportDisabled = exportDisabled;
    fixture.detectChanges();
    host = fixture.componentInstance;
    menu = fixture.debugElement.children[0].componentInstance as FileMenuComponent;
  }

  beforeEach(() => build());

  it('toggle flips isOpen', () => {
    const proto = asProto(menu);
    expect(proto.isOpen()).toBe(false);
    proto.toggle();
    expect(proto.isOpen()).toBe(true);
    proto.toggle();
    expect(proto.isOpen()).toBe(false);
  });

  it('onItemClick emits the action and closes the menu', () => {
    const proto = asProto(menu);
    proto.toggle();
    proto.onItemClick({ label: 'Import ASM...', action: 'importAsm' });
    expect(host.actions).toEqual(['importAsm']);
    expect(proto.isOpen()).toBe(false);
  });

  it('onItemClick ignores separators', () => {
    const proto = asProto(menu);
    proto.toggle();
    proto.onItemClick({ label: '', action: '', separator: true });
    expect(host.actions).toEqual([]);
    expect(proto.isOpen()).toBe(true);
  });

  it('exportCom action is disabled when exportDisabled input is true', () => {
    build(true);
    const proto = asProto(menu);
    expect(proto.isItemDisabled({ label: 'Export COM', action: 'exportCom' })).toBe(true);
    proto.toggle();
    proto.onItemClick({ label: 'Export COM', action: 'exportCom' });
    expect(host.actions).toEqual([]);
  });

  it('exportCom action emits when exportDisabled is false', () => {
    const proto = asProto(menu);
    proto.toggle();
    proto.onItemClick({ label: 'Export COM', action: 'exportCom' });
    expect(host.actions).toEqual(['exportCom']);
  });

  it('escape closes the menu', () => {
    const proto = asProto(menu);
    proto.toggle();
    menu.onEscape();
    expect(proto.isOpen()).toBe(false);
  });

  it('document click outside closes the menu', () => {
    const proto = asProto(menu);
    proto.toggle();
    const evt = new MouseEvent('click');
    Object.defineProperty(evt, 'target', { value: document.body });
    menu.onDocumentClick(evt);
    expect(proto.isOpen()).toBe(false);
  });
});
