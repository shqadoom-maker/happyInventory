import { Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';

import { AuthService } from '../../../core/services/auth.service';

gsap.registerPlugin(ScrollTrigger);

@Component({
  selector: 'app-marketplace-home',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './marketplace-home.html',
  styleUrl: './marketplace-home.scss',
})
export class MarketplaceHome implements OnInit {
  private readonly auth = inject(AuthService);
  @ViewChild('hero') hero?: ElementRef<HTMLElement>;

  ngOnInit(): void {}

  ngAfterViewInit(): void {
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches) {
      return;
    }

    if (this.hero?.nativeElement) {
      gsap.from(this.hero.nativeElement.querySelectorAll('.hero-copy > *'), {
        y: 24,
        opacity: 0,
        duration: 0.7,
        stagger: 0.12,
        ease: 'power3.out',
      });
    }

    gsap.from('.catalog-section .coming-card', {
      scrollTrigger: {
        trigger: '.catalog-section',
        start: 'top 80%',
      },
      y: 30,
      opacity: 0,
      duration: 0.6,
      ease: 'power2.out',
    });
  }
}
