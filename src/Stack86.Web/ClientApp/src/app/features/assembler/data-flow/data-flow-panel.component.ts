import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { PanelHeaderComponent } from '@shared/components';
import { DataFlowDiagramComponent } from './diagram/data-flow-diagram.component';

@Component({
  selector: 'emu-data-flow-panel',
  imports: [PanelHeaderComponent, DataFlowDiagramComponent, FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './data-flow-panel.component.html',
  styleUrl: './data-flow-panel.component.scss',
})
export class DataFlowPanelComponent {}
