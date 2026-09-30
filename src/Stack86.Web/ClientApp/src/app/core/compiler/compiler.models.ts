import { IconProp } from '@fortawesome/fontawesome-svg-core';

export enum SupportedLanguage {
  C = 'c',
  Cpp = 'cpp',
  CSharp = 'csharp',
  JavaScript = 'javascript',
  TypeScript = 'typescript',
}

export interface CompilerFile {
  id: string;
  name: string;
  content: string;
  isMain: boolean;
}

export interface CompileRequest {
  language: string;
  files: Record<string, string>;
}

export interface CompilationDiagnostic {
  message: string;
  line: number;
  column: number;
  file?: string;
}

export interface CompileResponse {
  assembly?: string;
  errors: CompilationDiagnostic[];
  warnings: CompilationDiagnostic[];
  consoleMessages: string[];
}

export interface CompileStreamLogEvent {
  type: 'log';
  text: string;
}

export interface CompileStreamHeartbeatEvent {
  type: 'heartbeat';
}

export interface CompileStreamResultEvent {
  type: 'result';
  assembly?: string;
  errors: CompilationDiagnostic[];
  warnings: CompilationDiagnostic[];
}

export type CompileStreamEvent =
  | CompileStreamLogEvent
  | CompileStreamHeartbeatEvent
  | CompileStreamResultEvent;

export interface LanguageOption {
  id: SupportedLanguage;
  label: string;
  monacoLanguage: string;
  mainFileName: string;
  sourceExtensions: string[];
  headerExtensions?: string[];
  faIcon?: IconProp;
  image?: string;
}

export const LANGUAGE_OPTIONS: LanguageOption[] = [
  { id: SupportedLanguage.C, label: 'C', monacoLanguage: 'c', mainFileName: 'main.c', sourceExtensions: ['.c'], headerExtensions: ['.h'], image: 'assets/languages/c.svg' },
  { id: SupportedLanguage.Cpp, label: 'C++', monacoLanguage: 'cpp', mainFileName: 'main.cpp', sourceExtensions: ['.cpp', '.cc', '.cxx'], headerExtensions: ['.h', '.hpp'], image: 'assets/languages/cpp.svg' },
  { id: SupportedLanguage.CSharp, label: 'C#', monacoLanguage: 'csharp', mainFileName: 'main.cs', sourceExtensions: ['.cs'], image: 'assets/languages/csharp.svg' },
  { id: SupportedLanguage.JavaScript, label: 'JavaScript', monacoLanguage: 'javascript', mainFileName: 'main.js', sourceExtensions: ['.js', '.mjs'], faIcon: ['fab', 'js'] },
  { id: SupportedLanguage.TypeScript, label: 'TypeScript', monacoLanguage: 'typescript', mainFileName: 'main.ts', sourceExtensions: ['.ts'], image: 'assets/languages/typescript.svg' },
];

export function getLanguageOption(language: SupportedLanguage): LanguageOption {
  return LANGUAGE_OPTIONS.find((l) => l.id === language) ?? LANGUAGE_OPTIONS[0];
}

export function getMainFileName(language: SupportedLanguage): string {
  return getLanguageOption(language).mainFileName;
}

export function getDefaultSourceExtension(language: SupportedLanguage): string {
  return getLanguageOption(language).sourceExtensions[0];
}

export function getDefaultHeaderExtension(language: SupportedLanguage): string | undefined {
  return getLanguageOption(language).headerExtensions?.[0];
}

export function getAllExtensions(language: SupportedLanguage): string[] {
  const opt = getLanguageOption(language);
  return [...opt.sourceExtensions, ...(opt.headerExtensions ?? [])];
}

export function isHeaderExtension(language: SupportedLanguage, ext: string): boolean {
  return getLanguageOption(language).headerExtensions?.includes(ext) ?? false;
}

export function createMainFile(language: SupportedLanguage, content: string = ''): CompilerFile {
  return { id: crypto.randomUUID(), name: getMainFileName(language), content, isMain: true };
}
