import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { describe, it, expect, beforeEach } from 'vitest';
import { CompilerService } from './compiler.service';
import { SupportedLanguage } from './compiler.models';

describe('CompilerService', () => {
  let service: CompilerService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CompilerService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  it('should create', () => {
    expect(service).toBeTruthy();
  });

  it('should POST to /api/Compiler/compile', () => {
    service.compile(SupportedLanguage.C, { 'main.c': 'int main() {}' }).subscribe();

    const req = httpTesting.expectOne('/api/Compiler/compile');
    expect(req.request.method).toBe('POST');
    req.flush({ assembly: 'MOV AX, 0', errors: [], warnings: [] });
  });

  it('should send language and files in request body', () => {
    const files = { 'main.c': 'cout << 1;' };
    service.compile(SupportedLanguage.Cpp, files).subscribe();

    const req = httpTesting.expectOne('/api/Compiler/compile');
    expect(req.request.body).toEqual({
      language: SupportedLanguage.Cpp,
      files,
    });
    req.flush({ assembly: null, errors: [], warnings: [] });
  });

  it('should return CompileResponse on success', () => {
    const mockResponse = {
      assembly: 'MOV AX, 5',
      errors: [],
      warnings: [{ message: 'unused', line: 1, column: 1 }],
    };

    let result: unknown;
    service.compile(SupportedLanguage.C, { 'main.c': 'int x = 5;' }).subscribe((r) => (result = r));

    const req = httpTesting.expectOne('/api/Compiler/compile');
    req.flush(mockResponse);

    expect(result).toEqual(mockResponse);
  });
});
