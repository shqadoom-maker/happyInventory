import { DatePipe } from '@angular/common';
import {
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { PagedResult } from '../../core/models/paged-result.model';
import {
  SystemLog,
  SystemLogCleanupResult,
  SystemLogFilter,
  SystemLogLevel,
} from '../../core/models/system-log.model';
import { User } from '../../core/models/user.model';
import { SystemLogService } from '../../core/services/system-log.service';
import { ToastService } from '../../core/services/toast.service';
import { UserService } from '../../core/services/user.service';

@Component({
  selector: 'app-system-log-list',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './system-log-list.html',
  styleUrl: './system-log-list.scss',
})
export class SystemLogList implements OnInit, OnDestroy {
  private readonly logService = inject(SystemLogService);
  private readonly userService = inject(UserService);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);

  protected readonly result = signal<PagedResult<SystemLog> | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly search = signal('');
  protected readonly level = signal('');
  protected readonly userId = signal('');
  protected readonly fromDate = signal('');
  protected readonly toDate = signal('');

  protected readonly page = signal(1);
  protected readonly pageSize = 25;
  protected readonly users = signal<User[]>([]);
  protected readonly expandedId = signal<number | null>(null);

  protected readonly cleanupDays = signal(30);
  protected readonly cleaningUp = signal(false);

  protected readonly levelOptions: readonly SystemLogLevel[] = [
    'Verbose',
    'Debug',
    'Information',
    'Warning',
    'Error',
    'Fatal',
  ];

  protected readonly items = computed(() => this.result()?.items ?? []);
  protected readonly totalPages = computed(() => this.result()?.totalPages ?? 0);
  protected readonly totalCount = computed(() => this.result()?.totalCount ?? 0);

  private searchTimer: ReturnType<typeof setTimeout> | null = null;
  private requestSequence = 0;

  ngOnInit(): void {
    this.load();
    this.loadUsers();
  }

  ngOnDestroy(): void {
    if (this.searchTimer) {
      clearTimeout(this.searchTimer);
    }
  }

  protected load(): void {
    if (!this.validateDateRange()) {
      return;
    }

    const sequence = ++this.requestSequence;
    this.loading.set(true);
    this.error.set(null);

    const filter: SystemLogFilter = {
      page: this.page(),
      pageSize: this.pageSize,
      level: this.level() || undefined,
      search: this.search().trim() || undefined,
      userId: this.userId() || undefined,
      fromUtc: this.fromDate() ? `${this.fromDate()}T00:00:00.000Z` : undefined,
      toUtc: this.toDate() ? `${this.toDate()}T23:59:59.999Z` : undefined,
    };

    this.logService.list(filter).subscribe({
      next: (result) => {
        if (sequence !== this.requestSequence) {
          return;
        }

        this.result.set(result);
        this.page.set(result.page);
        this.loading.set(false);
      },
      error: (err) => {
        if (sequence !== this.requestSequence) {
          return;
        }

        this.error.set(errorMessage(err, this.translate));
        this.loading.set(false);
      },
    });
  }

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
    this.page.set(1);

    if (this.searchTimer) {
      clearTimeout(this.searchTimer);
    }

    this.searchTimer = setTimeout(() => this.load(), 300);
  }

  protected onLevelChange(event: Event): void {
    this.level.set((event.target as HTMLSelectElement).value);
    this.page.set(1);
    this.load();
  }

  protected onUserChange(event: Event): void {
    this.userId.set((event.target as HTMLSelectElement).value);
    this.page.set(1);
    this.load();
  }

  protected onFromChange(event: Event): void {
    this.fromDate.set((event.target as HTMLInputElement).value);
    this.page.set(1);
    this.load();
  }

  protected onToChange(event: Event): void {
    this.toDate.set((event.target as HTMLInputElement).value);
    this.page.set(1);
    this.load();
  }

  protected resetFilters(): void {
    if (this.searchTimer) {
      clearTimeout(this.searchTimer);
    }

    this.search.set('');
    this.level.set('');
    this.userId.set('');
    this.fromDate.set('');
    this.toDate.set('');
    this.page.set(1);
    this.expandedId.set(null);
    this.load();
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }

    this.page.set(page);
    this.expandedId.set(null);
    this.load();
  }

  protected prevPage(): void {
    this.goToPage(this.page() - 1);
  }

  protected nextPage(): void {
    this.goToPage(this.page() + 1);
  }

  protected toggleDetails(id: number): void {
    this.expandedId.set(this.expandedId() === id ? null : id);
  }

  protected levelKey(level: SystemLogLevel): string {
    return `systemLogs.levels.${level.toLowerCase()}`;
  }

  protected levelClass(level: SystemLogLevel): string {
    return `level-${level.toLowerCase()}`;
  }

  protected statusClass(status?: number | null): string {
    if (status == null) {
      return 'status-muted';
    }

    if (status >= 500) {
      return 'status-danger';
    }

    if (status >= 400) {
      return 'status-warning';
    }

    if (status >= 300) {
      return 'status-info';
    }

    if (status >= 200) {
      return 'status-success';
    }

    return 'status-muted';
  }

  protected onCleanupDaysChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.cleanupDays.set(Number.isFinite(value) && value > 0 ? value : 30);
  }

  protected cleanup(): void {
    const days = this.cleanupDays();
    const message = this.translate.instant('systemLogs.cleanup.confirm', { days });

    this.toast
      .confirm(message, this.translate.instant('systemLogs.cleanup.title'))
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.cleaningUp.set(true);
        this.error.set(null);

        this.logService.cleanup(days).subscribe({
          next: (result: SystemLogCleanupResult) => {
            this.cleaningUp.set(false);
            this.toast.success(
              this.translate.instant('systemLogs.cleanup.success', {
                count: result.deletedCount,
                days: result.olderThanDays,
              }),
            );
            this.page.set(1);
            this.load();
          },
          error: (err) => {
            this.cleaningUp.set(false);
            this.error.set(errorMessage(err, this.translate));
          },
        });
      });
  }

  private loadUsers(): void {
    this.userService.list().subscribe({
      next: (users) => this.users.set(users),
      // The user filter is a convenience; log viewing must still work if the
      // user list cannot be loaded.
      error: () => undefined,
    });
  }

  private validateDateRange(): boolean {
    if (this.fromDate() && this.toDate() && this.fromDate() > this.toDate()) {
      this.error.set(this.translate.instant('errors.invalidDateRange'));
      this.loading.set(false);
      return false;
    }

    return true;
  }
}
