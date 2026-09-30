import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, it, expect } from 'vitest';
import { NavigationComponent } from './navigation.component';
import { provideTestIcons } from '../../../testing/provide-icons';

describe('NavigationComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavigationComponent],
      providers: [provideRouter([]), provideTestIcons()],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(NavigationComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render brand text', () => {
    const fixture = TestBed.createComponent(NavigationComponent);
    fixture.detectChanges();
    const brand = fixture.nativeElement.querySelector('.shell-nav__brand');
    expect(brand).toBeTruthy();
    expect(brand.textContent).toContain('Stack86');
  });

  it('should render assembler tab', () => {
    const fixture = TestBed.createComponent(NavigationComponent);
    fixture.detectChanges();
    const tabs: HTMLAnchorElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('.shell-nav__tab'),
    );
    const assemblerTab = tabs.find((t) => t.textContent?.includes('Assembler'));
    expect(assemblerTab).toBeTruthy();
  });

  it('should render compiler tab', () => {
    const fixture = TestBed.createComponent(NavigationComponent);
    fixture.detectChanges();
    const tabs: HTMLAnchorElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('.shell-nav__tab'),
    );
    const compilerTab = tabs.find((t) => t.textContent?.includes('Compiler'));
    expect(compilerTab).toBeTruthy();
  });

  it('should have exactly two navigation tabs', () => {
    const fixture = TestBed.createComponent(NavigationComponent);
    fixture.detectChanges();
    const tabs = fixture.nativeElement.querySelectorAll('.shell-nav__tab');
    expect(tabs.length).toBe(2);
  });

  it('should link assembler tab to /assembler', () => {
    const fixture = TestBed.createComponent(NavigationComponent);
    fixture.detectChanges();
    const tabs: HTMLAnchorElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('.shell-nav__tab'),
    );
    const assemblerTab = tabs.find((t) => t.textContent?.includes('Assembler'));
    expect(assemblerTab?.getAttribute('href')).toBe('/assembler');
  });

  it('should link compiler tab to /compiler', () => {
    const fixture = TestBed.createComponent(NavigationComponent);
    fixture.detectChanges();
    const tabs: HTMLAnchorElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('.shell-nav__tab'),
    );
    const compilerTab = tabs.find((t) => t.textContent?.includes('Compiler'));
    expect(compilerTab?.getAttribute('href')).toBe('/compiler');
  });
});
