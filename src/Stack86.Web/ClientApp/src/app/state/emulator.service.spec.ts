import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { EmulatorService } from './emulator.service';
import { ExecutionEngine } from '../core/emulator/execution/execution-engine';

describe('EmulatorService', () => {
  it('exposes an ExecutionEngine instance', () => {
    TestBed.configureTestingModule({});
    const service = TestBed.inject(EmulatorService);
    expect(service.engine).toBeInstanceOf(ExecutionEngine);
  });

  it('returns the same engine instance on repeated property access', () => {
    TestBed.configureTestingModule({});
    const service = TestBed.inject(EmulatorService);
    expect(service.engine).toBe(service.engine);
  });
});
