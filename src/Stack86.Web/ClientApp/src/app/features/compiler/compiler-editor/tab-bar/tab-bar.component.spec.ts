import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { beforeEach, describe, it, expect, vi } from 'vitest';
import { TabBarComponent } from './tab-bar.component';
import { CompilerFile } from '../../../../core/compiler/compiler.models';
import { provideTestIcons } from '../../../../testing/provide-icons';

const MAIN_FILE: CompilerFile = { id: 'main-id', name: 'main.c', content: '', isMain: true };
const UTILS_FILE: CompilerFile = { id: 'utils-id', name: 'utils.c', content: '', isMain: false };
const HEADER_FILE: CompilerFile = { id: 'header-id', name: 'types.h', content: '', isMain: false };

@Component({
  imports: [TabBarComponent],
  template: `
    <emu-tab-bar
      [files]="files()"
      [activeFileId]="activeFileId()"
      (tabSelected)="onTabSelected($event)"
      (tabClosed)="onTabClosed($event)"
      (addFileRequested)="onAddFileRequested()"
    />
  `,
})
class TestHostComponent {
  public readonly files = signal<CompilerFile[]>([MAIN_FILE, UTILS_FILE, HEADER_FILE]);
  public readonly activeFileId = signal('main-id');

  public readonly onTabSelected = vi.fn();
  public readonly onTabClosed = vi.fn();
  public readonly onAddFileRequested = vi.fn();
}

describe('TabBarComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [provideTestIcons()],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  function getTabs(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.tab'));
  }

  function getAddButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('.tab-bar__add');
  }

  it('should create', () => {
    const tabBar = fixture.nativeElement.querySelector('emu-tab-bar');
    expect(tabBar).toBeTruthy();
  });

  it('should render a tab for each file', () => {
    expect(getTabs()).toHaveLength(3);
  });

  it('should display file names', () => {
    const names = getTabs().map((t) => t.querySelector('.tab__name')!.textContent!.trim());
    expect(names).toEqual(['main.c', 'utils.c', 'types.h']);
  });

  it('should mark the active tab', () => {
    const tabs = getTabs();
    expect(tabs[0].classList.contains('tab--active')).toBe(true);
    expect(tabs[1].classList.contains('tab--active')).toBe(false);
  });

  it('should update active tab when activeFileId changes', () => {
    host.activeFileId.set('utils-id');
    fixture.detectChanges();
    const tabs = getTabs();
    expect(tabs[0].classList.contains('tab--active')).toBe(false);
    expect(tabs[1].classList.contains('tab--active')).toBe(true);
  });

  it('should emit tabSelected when a tab is clicked', () => {
    getTabs()[1].click();
    expect(host.onTabSelected).toHaveBeenCalledWith('utils-id');
  });

  it('should not show close button on main file tab', () => {
    const mainTab = getTabs()[0];
    expect(mainTab.querySelector('.tab__close')).toBeNull();
  });

  it('should show close button on non-main file tabs', () => {
    const utilsTab = getTabs()[1];
    expect(utilsTab.querySelector('.tab__close')).toBeTruthy();
  });

  it('should emit tabClosed when close button is clicked', () => {
    const closeBtn = getTabs()[1].querySelector('.tab__close') as HTMLElement;
    closeBtn.click();
    expect(host.onTabClosed).toHaveBeenCalledWith('utils-id');
  });

  it('should stop propagation on close click so tab does not also select', () => {
    const closeBtn = getTabs()[2].querySelector('.tab__close') as HTMLElement;
    closeBtn.click();
    expect(host.onTabClosed).toHaveBeenCalledWith('header-id');
    expect(host.onTabSelected).not.toHaveBeenCalledWith('header-id');
  });

  it('should hide close buttons when only one file remains', () => {
    host.files.set([MAIN_FILE]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.tab__close')).toBeNull();
  });

  it('should emit addFileRequested when add button is clicked', () => {
    getAddButton().click();
    expect(host.onAddFileRequested).toHaveBeenCalled();
  });

  it('should use file-code icon for .c files', () => {
    const tabBar = fixture.debugElement.children[0].componentInstance as TabBarComponent;
    expect(tabBar.getFileIcon('main.c')).toBe('file-code');
  });

  it('should use file-lines icon for .h files', () => {
    const tabBar = fixture.debugElement.children[0].componentInstance as TabBarComponent;
    expect(tabBar.getFileIcon('types.h')).toBe('file-lines');
  });

  it('should update tabs when files list changes', () => {
    host.files.set([MAIN_FILE]);
    fixture.detectChanges();
    expect(getTabs()).toHaveLength(1);
    expect(getTabs()[0].querySelector('.tab__name')!.textContent!.trim()).toBe('main.c');
  });
});
