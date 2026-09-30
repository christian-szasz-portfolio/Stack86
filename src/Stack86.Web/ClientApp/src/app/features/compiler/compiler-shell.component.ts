import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CompilerLayoutComponent } from './compiler-layout/compiler-layout.component';

/**
 * Compiler shell — hosts the compiler editor layout.
 */
@Component({
  selector: 'emu-compiler-shell',
  imports: [CompilerLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './compiler-shell.component.html',
  styleUrl: './compiler-shell.component.scss',
})
export class CompilerShellComponent {}
