import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';

export type ToastType = 'success' | 'error' | 'warning' | 'info' | 'confirm';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
  title?: string;
  duration?: number;
  onConfirm?: () => void;
  onCancel?: () => void;
}

export interface ConfirmResult {
  confirmed: boolean;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private toasts = new BehaviorSubject<Toast[]>([]);
  private idCounter = 0;

  readonly toasts$ = this.toasts.asObservable();

  private addToast(toast: Omit<Toast, 'id'>): string {
    const id = (++this.idCounter).toString();
    const newToast: Toast = { ...toast, id };
    this.toasts.next([...this.toasts.value, newToast]);
    return id;
  }

  remove(id: string): void {
    this.toasts.next(this.toasts.value.filter((t) => t.id !== id));
  }

  clear(): void {
    this.toasts.next([]);
  }

  success(message: string, duration = 4000): string {
    return this.addToast({ type: 'success', message, duration });
  }

  error(message: string, duration = 6000): string {
    return this.addToast({ type: 'error', message, duration });
  }

  warning(message: string, duration = 5000): string {
    return this.addToast({ type: 'warning', message, duration });
  }

  info(message: string, duration = 5000): string {
    return this.addToast({ type: 'info', message, duration });
  }

  /**
   * Shows a confirmation toast with Yes/No buttons.
   * Returns an Observable that emits true when confirmed, false when cancelled/dismissed.
   */
  confirm(message: string, title = 'تأكيد', duration = 0): Observable<boolean> {
    return new Observable<boolean>((subscriber) => {
      const id = this.addToast({
        type: 'confirm',
        message,
        title,
        duration,
        onConfirm: () => {
          subscriber.next(true);
          subscriber.complete();
          this.remove(id);
        },
        onCancel: () => {
          subscriber.next(false);
          subscriber.complete();
          this.remove(id);
        },
      });

      return () => {
        subscriber.complete();
        this.remove(id);
      };
    });
  }
}
