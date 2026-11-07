import { Component, Input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-logo',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './logo.html',
  styleUrl: './logo.scss',
})
export class Logo {
  @Input() variant: 'full' | 'icon' = 'full';
  @Input() theme: 'auto' | 'light' | 'dark' = 'auto';
}
