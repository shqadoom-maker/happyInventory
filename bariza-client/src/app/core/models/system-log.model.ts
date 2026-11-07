export type SystemLogLevel =
  | 'Verbose'
  | 'Debug'
  | 'Information'
  | 'Warning'
  | 'Error'
  | 'Fatal';

export interface SystemLog {
  id: number;
  timestampUtc: string;
  level: SystemLogLevel;
  message: string;
  messageTemplate?: string | null;
  exception?: string | null;
  sourceContext?: string | null;
  userId?: string | null;
  userName?: string | null;
  requestPath?: string | null;
  requestMethod?: string | null;
  statusCode?: number | null;
  traceId?: string | null;
  properties?: string | null;
}

export interface SystemLogFilter {
  page?: number;
  pageSize?: number;
  level?: string;
  search?: string;
  fromUtc?: string;
  toUtc?: string;
  userId?: string;
}

export interface SystemLogCleanupResult {
  deletedCount: number;
  olderThanDays: number;
  cutoffUtc: string;
}
