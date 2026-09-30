import { describe, it, expect, vi, afterEach } from 'vitest';
import { AssemblerBridgeService } from './assembler-bridge.service';
import { SupportedLanguage } from './compiler.models';

const ORIGIN = { language: SupportedLanguage.C, sampleName: 'Hello Arithmetic' };

describe('AssemblerBridgeService', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should create', () => {
    const service = new AssemblerBridgeService();
    expect(service).toBeTruthy();
  });

  it('should store pending assembly', () => {
    const service = new AssemblerBridgeService();

    service.loadAssemblyIntoAssembler('MOV AX, 1', ORIGIN);

    expect(service.consumePendingAssembly()).toBe('MOV AX, 1');
  });

  it('should clear pending assembly after consumption', () => {
    const service = new AssemblerBridgeService();

    service.loadAssemblyIntoAssembler('MOV AX, 1', ORIGIN);
    service.consumePendingAssembly();

    expect(service.consumePendingAssembly()).toBeNull();
  });

  it('should return null when no pending assembly', () => {
    const service = new AssemblerBridgeService();

    expect(service.consumePendingAssembly()).toBeNull();
  });

  it('should store the last assembly when called multiple times', () => {
    const service = new AssemblerBridgeService();

    service.loadAssemblyIntoAssembler('MOV AX, 1', ORIGIN);
    service.loadAssemblyIntoAssembler('MOV AX, 5\nADD AX, BX\nHLT', ORIGIN);

    expect(service.consumePendingAssembly()).toBe('MOV AX, 5\nADD AX, BX\nHLT');
  });

  it('should store source origin', () => {
    const service = new AssemblerBridgeService();

    service.loadAssemblyIntoAssembler('MOV AX, 1', ORIGIN);

    expect(service.getSourceOrigin()).toEqual(ORIGIN);
  });

  it('should store null sample name for custom programs', () => {
    const service = new AssemblerBridgeService();
    const customOrigin = { language: SupportedLanguage.C, sampleName: null };

    service.loadAssemblyIntoAssembler('MOV AX, 1', customOrigin);

    expect(service.getSourceOrigin()).toEqual(customOrigin);
  });
});
