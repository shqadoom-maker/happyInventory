import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { ToastService, Toast } from '../services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    <div class="toast-container" aria-live="polite" aria-atomic="true">
      @for (toast of toasts(); track toast.id) {
        <div
          class="toast toast-{{ toast.type }}"
          role="alert"
          [class.toast-confirm]="toast.type === 'confirm'"
        >
          @if (toast.title) {
            <div class="toast-title">{{ toast.title }}</div>
          }
          <div class="toast-message">{{ toast.message | translate }}</div>
          @if (toast.type === 'confirm') {
            <div class="toast-actions">
              <button class="btn toast-yes" type="button" (click)="confirm(toast)">
                {{ 'common.yes' | translate }}
              </button>
              <button class="btn toast-no toast-secondary" type="button" (click)="cancel(toast)">
                {{ 'common.no' | translate }}
              </button>
            </div>
          } @else {
            <button class="toast-close" type="button" (click)="dismiss(toast.id)">
              <span class="material-icons-round" aria-hidden="true">close</span>
            </button>
          }
        </div>
      }
    </div>
  `,
  styles: `
    :host {
      position: fixed;
      top: 20px;
      right: 20px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 10px;
      pointer-events: none;
    }

    .toast-container {
      display: flex;
      flex-direction: column;
      gap: 10px;
      pointer-events: auto;
    }

    .toast {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 12px 16px;
      border-radius: 8px;
      color: #fff;
      font-size: 0.9rem;
      min-width: 300px;
      max-width: 500px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      animation: slideIn 0.3s ease forwards;
      pointer-events: auto;
    }

    .toast-confirm {
      background: #1e293b;
      border: 1px solid #334155;
    }

    .toast-success { background: #10b981; }
    .toast-error { background: #ef4444; }
    .toast-warning { background: #f59e0b; }
    .toast-info { background: #3b82f6; }

    .toast-title {
      font-weight: 600;
      font-size: 0.85rem;
      opacity: 0.9;
      margin-bottom: 4px;
    }

    .toast-message {
      flex: 1;
      line-height: 1.4;
    }

    .toast-close {
      background: none;
      border: none;
      color: inherit;
      opacity: 0.7;
      cursor: pointer;
      padding: 4px;
      font-size: 1.2rem;
      line-height: 1;
    }

    .toast-close:hover {
      opacity: 1;
    }

    .toast-actions {
      display: flex;
      gap: 8px;
      margin-top: 10px;
    }

    .toast-yes {
      background: #3b82f6;
      border: none;
      color: #fff;
      padding: 6px 14px;
      border-radius: 6px;
      font-size: 0.85rem;
      cursor: pointer;
    }

    .toast-yes:hover {
      background: #2563eb;
    }

    .toast-secondary {
      background: transparent;
      border: 1px solid #475569;
      color: #cbd5e1;
      padding: 6px 14px;
      border-radius: 6px;
      font-size: 0.85rem;
      cursor: pointer;
    }

    .toast-secondary:hover {
      background: #334155;
      color: #fff;
    }

    @keyframes slideIn {
      from {
        transform: translateX(100%);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }

    @media (max-width: 640px) {
      :host {
        left: 20px;
        right: 20px;
      }
      .toast {
        min-width: auto;
        width: 100%;
      }
    }
  `,
})
export class ToastContainer {
  private readonly toastService = inject(ToastService);
  protected readonly toasts = signal<Toast[]>([]);
  private dismissTimers = new Map<string, ReturnType<typeof setTimeout>>();

  constructor() {
    this.toastService.toasts$.subscribe((toasts) => {
      this.toasts.set(toasts);
      // Clear existing timers
      this.dismissTimers.forEach((timer) => clearTimeout(timer));
      this.dismissTimers.clear();

      // Set up auto-dismiss timers
      toasts.forEach((toast) => {
        if (toast.duration && toast.duration > 0 && toast.type !== 'confirm') {
          const timer = setTimeout(() => {
            this.dismiss(toast.id);
          }, toast.duration);
          this.dismissTimers.set(toast.id, timer);
        }
      });
    });
  }

  dismiss(id: string): void {
    this.toastService.remove(id);
    this.dismissTimers.delete(id);
  }

  confirm(toast: Toast): void {
    if (toast.onConfirm) {
      toast.onConfirm();
    }
  }

  cancel(toast: Toast): void {
    if (toast.onCancel) {
      toast.onCancel();
    }
    this.dismiss(toast.id);
  }
}
