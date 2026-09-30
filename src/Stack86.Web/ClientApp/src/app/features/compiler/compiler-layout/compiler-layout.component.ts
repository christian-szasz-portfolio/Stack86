import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SplitComponent, SplitAreaComponent } from 'angular-split';
import { CompileToolbarComponent } from '../compile-toolbar/compile-toolbar.component';
import { CompilerEditorComponent } from '../compiler-editor/compiler-editor.component';
import { CompileResultsComponent } from '../compile-results/compile-results.component';
import { CompileConsoleComponent } from '../compile-console/compile-console.component';

@Component({
  selector: 'emu-compiler-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    SplitComponent,
    SplitAreaComponent,
    CompileToolbarComponent,
    CompilerEditorComponent,
    CompileResultsComponent,
    CompileConsoleComponent,
  ],
  templateUrl: './compiler-layout.component.html',
  styleUrl: './compiler-layout.component.scss',
})
export class CompilerLayoutComponent {}
