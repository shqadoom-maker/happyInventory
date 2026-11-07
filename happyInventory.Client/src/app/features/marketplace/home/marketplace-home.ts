import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-marketplace-home',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './marketplace-home.html',
  styleUrl: './marketplace-home.scss',
})
export class MarketplaceHome {}
