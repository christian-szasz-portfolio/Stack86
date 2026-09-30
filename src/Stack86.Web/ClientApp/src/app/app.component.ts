import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavigationComponent } from './features/shell/navigation/navigation.component';
import { ErrorModalComponent } from './shared/modals/error-modal.component';

@Component({
  selector: 'emu-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, NavigationComponent, ErrorModalComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {}
