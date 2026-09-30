import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { SplitComponent, SplitAreaComponent } from 'angular-split';
import { ToolbarComponent } from './toolbar/toolbar.component';
import { EditorComponent } from '../editor/editor.component';
import { RegistersComponent } from '../debugger/registers/registers.component';
import { FlagsComponent } from '../debugger/flags/flags.component';
import { MemoryViewComponent } from '../debugger/memory-view/memory-view.component';
import { StackViewComponent } from '../debugger/stack-view/stack-view.component';
import { ConsoleComponent } from '../console/console.component';
import { DataFlowPanelComponent } from '../data-flow/data-flow-panel.component';
import { AssemblerBridgeService } from '../../../core/compiler/assembler-bridge.service';
import { LANGUAGE_OPTIONS } from '../../../core/compiler/compiler.models';

@Component({
  host: {
    '(window:resize)': 'onResize()',
  },
  selector: 'emu-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    SplitComponent,
    SplitAreaComponent,
    ToolbarComponent,
    EditorComponent,
    RegistersComponent,
    FlagsComponent,
    MemoryViewComponent,
    StackViewComponent,
    ConsoleComponent,
    DataFlowPanelComponent,
  ],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss',
})
export class LayoutComponent {
  private readonly assemblerBridge = inject(AssemblerBridgeService);

  private static readonly NARROW_BREAKPOINT = 720;
  private static readonly COMPACT_TOOLBAR_BREAKPOINT = 1120;

  public readonly debugPanelWidth = signal(this.computeDebugWidth());
  public readonly splittersDisabled = signal(this.isNarrowViewport());
  public readonly compactToolbar = signal(this.isCompactToolbar());
  public readonly showDataFlow = signal(true);

  public readonly sourceTitle = this.buildSourceTitle();

  public onResize(): void {
    this.debugPanelWidth.set(this.computeDebugWidth());
    this.splittersDisabled.set(this.isNarrowViewport());
    this.compactToolbar.set(this.isCompactToolbar());
  }

  private isNarrowViewport(): boolean {
    const vw = typeof window !== 'undefined' ? window.innerWidth : 1200;
    return vw < LayoutComponent.NARROW_BREAKPOINT;
  }

  private isCompactToolbar(): boolean {
    const vw = typeof window !== 'undefined' ? window.innerWidth : 1200;
    return vw < LayoutComponent.COMPACT_TOOLBAR_BREAKPOINT;
  }

  private computeDebugWidth(): number {
    const vw = typeof window !== 'undefined' ? window.innerWidth : 1200;
    return Math.min(575, Math.max(320, vw - 400));
  }

  private buildSourceTitle(): string | null {
    const origin = this.assemblerBridge.getSourceOrigin();
    if (!origin) {
      return null;
    }
    const langLabel = LANGUAGE_OPTIONS.find((l) => l.id === origin.language)?.label ?? origin.language;
    const title = origin.sampleName ?? 'Custom Program';
    return `${langLabel} — ${title}`;
  }
}