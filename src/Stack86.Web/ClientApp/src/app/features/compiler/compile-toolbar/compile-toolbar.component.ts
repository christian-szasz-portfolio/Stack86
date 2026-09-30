import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ButtonComponent, ButtonVariant, DropdownMenuComponent, DropdownMenuItem } from '@shared/components';
import { CompilerFacade } from '../../../state/compiler.facade';
import { SupportedLanguage, LANGUAGE_OPTIONS, CompilerFile, getMainFileName } from '../../../core/compiler/compiler.models';
import { CompilerStorageUtility } from '../../../core/compiler/compiler-storage.utility';
import { getSamplesForLanguage } from './sample-programs';

@Component({
  selector: 'emu-compile-toolbar',
  imports: [ButtonComponent, DropdownMenuComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './compile-toolbar.component.html',
  styleUrl: './compile-toolbar.component.scss',
})
export class CompileToolbarComponent {
  private readonly facade = inject(CompilerFacade);

  protected readonly ButtonVariant = ButtonVariant;

  public readonly languageItems: DropdownMenuItem[] = LANGUAGE_OPTIONS.map((l) => ({
    value: l.id,
    label: l.label,
    icon: l.faIcon,
    image: l.image,
  }));

  public readonly selectedLanguage = this.facade.selectedLanguage;
  public readonly loading = this.facade.loading;
  public readonly canLoadIntoAssembler = this.facade.canLoadIntoAssembler;
  public readonly selectedSample = signal(
    CompilerStorageUtility.loadSample(this.facade.selectedLanguage())?.toString() ?? '',
  );

  public readonly sampleItems = computed<DropdownMenuItem[]>(() => {
    const lang = this.selectedLanguage();
    return getSamplesForLanguage(lang).map((s, i) => ({
      value: i.toString(),
      label: s.name,
    }));
  });

  public onLanguageChange(value: string): void {
    const language = value as SupportedLanguage;
    this.facade.selectLanguage(language);
    CompilerStorageUtility.saveLanguage(language);
    this.selectedSample.set('');
  }

  public loadSample(value: string): void {
    const lang = this.selectedLanguage();
    const samples = getSamplesForLanguage(lang);
    const mainFileName = getMainFileName(lang);
    const idx = parseInt(value, 10);
    if (!isNaN(idx) && idx >= 0 && idx < samples.length) {
      const sample = samples[idx];
      const files: CompilerFile[] = sample.files.map((f) => ({
        id: crypto.randomUUID(),
        name: f.name,
        content: f.content,
        isMain: f.name === mainFileName,
      }));
      this.facade.loadProject(files);
      CompilerStorageUtility.saveSample(lang, idx);
    }
    this.selectedSample.set(value);
  }

  public compile(): void {
    this.facade.compile();
  }

  public loadIntoAssembler(): void {
    this.facade.loadAssemblyIntoAssembler();
  }
}
