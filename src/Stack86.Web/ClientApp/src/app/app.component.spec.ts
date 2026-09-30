import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, it, expect } from 'vitest';
import { AppComponent } from './app.component';
import { provideTestIcons } from './testing/provide-icons';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([]), provideTestIcons()],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should have the emu-root selector', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('emu-navigation')).toBeTruthy();
  });

  it('should render navigation component', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const nav = fixture.nativeElement.querySelector('emu-navigation');
    expect(nav).toBeTruthy();
  });

  it('should render router-outlet', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const outlet = fixture.nativeElement.querySelector('router-outlet');
    expect(outlet).toBeTruthy();
  });

  it('should have a main.shell-content wrapper', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const main = fixture.nativeElement.querySelector('main.shell-content');
    expect(main).toBeTruthy();
  });
});
