export class AuditLogModel {
  auditLogId: number = 0;
  userId?: number;
  userName: string = '';
  action: string = '';
  entityName: string = '';
  entityId?: number;
  changes: string = '';
  createdAt: string = '';
}

export class CreateAuditLogModel {
  userId?: number;
  action: string = '';
  entityName: string = '';
  entityId?: number;
  changes?: string;
}
