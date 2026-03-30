import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AuditLog } from './audit-log';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { of, throwError } from 'rxjs';
import { AuditLogModel } from '../models/audit-log.model';
import { PagedResponse } from '../models/paged.model';

const mockLog = (id: number): AuditLogModel => ({
  auditLogId: id, userId: 1, userName: 'admin',
  action: 'Create', entityName: 'Hotel', entityId: id,
  changes: 'Created hotel', createdAt: new Date().toISOString()
});

const PAGE_SIZE = 10;

const mockPaged = (logs: AuditLogModel[], total = logs.length, pageNumber = 1): PagedResponse<AuditLogModel> => ({
  data: logs, pageNumber, pageSize: PAGE_SIZE,
  totalRecords: total, totalPages: Math.max(1, Math.ceil(total / PAGE_SIZE))
});

describe('AuditLog', () => {
  let fixture: ComponentFixture<AuditLog>;
  let component: AuditLog;
  let apiSpy: jasmine.SpyObj<APIService>;
  let toastSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    apiSpy   = jasmine.createSpyObj('APIService', ['apiGetAllAuditLogsPaged', 'apiFilterAuditLogsPaged', 'apiDeleteAuditLog']);
    toastSpy = jasmine.createSpyObj('ToastrService', ['success', 'error', 'warning']);

    apiSpy.apiGetAllAuditLogsPaged.and.returnValue(of(mockPaged([mockLog(1), mockLog(2)])));

    await TestBed.configureTestingModule({
      imports: [AuditLog],
      providers: [
        { provide: APIService,    useValue: apiSpy },
        { provide: ToastrService, useValue: toastSpy }
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(AuditLog);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load logs on init', () => {
    expect(apiSpy.apiGetAllAuditLogsPaged).toHaveBeenCalledWith({ pageNumber: 1, pageSize: 10 });
    expect(component.logs().length).toBe(2);
  });

  it('should set totalLogs from loaded data', () => {
    expect(component.totalLogs()).toBe(2);
  });

  it('isFiltered should be false by default', () => {
    expect(component.isFiltered()).toBeFalse();
  });

  it('isFiltered should be true when action is set', () => {
    component.filter.update(f => ({ ...f, action: 'Create' }));
    expect(component.isFiltered()).toBeTrue();
  });

  it('should call apiFilterAuditLogsPaged when filter is active', () => {
    apiSpy.apiFilterAuditLogsPaged.and.returnValue(of(mockPaged([mockLog(1)])));
    component.filter.update(f => ({ ...f, action: 'Create' }));
    component.applyFilter();
    expect(apiSpy.apiFilterAuditLogsPaged).toHaveBeenCalled();
  });

  it('clearFilter should reset filter and reload', () => {
    component.filter.update(f => ({ ...f, action: 'Delete' }));
    component.clearFilter();
    expect(component.filter().action).toBe('');
    expect(component.isFiltered()).toBeFalse();
    expect(apiSpy.apiGetAllAuditLogsPaged).toHaveBeenCalled();
  });

  it('should delete a log and reload', () => {
    apiSpy.apiDeleteAuditLog.and.returnValue(of({}));
    spyOn(window, 'confirm').and.returnValue(true);
    component.delete(mockLog(1));
    expect(apiSpy.apiDeleteAuditLog).toHaveBeenCalledWith(1);
    expect(toastSpy.success).toHaveBeenCalledWith('Audit log deleted.');
  });

  it('should not delete if confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    component.delete(mockLog(1));
    expect(apiSpy.apiDeleteAuditLog).not.toHaveBeenCalled();
  });

  it('should show error toast on load failure', () => {
    apiSpy.apiGetAllAuditLogsPaged.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(toastSpy.error).toHaveBeenCalledWith('Failed to load audit logs.', 'Error');
  });

  it('actionClass should return correct class', () => {
    expect(component.actionClass('Create Hotel')).toBe('action-create');
    expect(component.actionClass('Update Room')).toBe('action-update');
    expect(component.actionClass('Delete User')).toBe('action-delete');
    expect(component.actionClass('Login')).toBe('action-auth');
    expect(component.actionClass('Export')).toBe('action-default');
  });

  it('entityClass should return correct class', () => {
    expect(component.entityClass('Hotel')).toBe('entity-hotel');
    expect(component.entityClass('Booking')).toBe('entity-booking');
    expect(component.entityClass('Unknown')).toBe('entity-default');
  });

  it('setFilterField should update filter signal', () => {
    component.setFilterField('action', 'Login');
    expect(component.filter().action).toBe('Login');
  });

  it('setFilterField with empty string should set undefined', () => {
    component.setFilterField('action', '');
    expect(component.filter().action).toBeUndefined();
  });

  it('setFilterNum should parse number', () => {
    component.setFilterNum('userId', '7');
    expect(component.filter().userId).toBe(7);
  });

  it('setFilterNum with empty string should set undefined', () => {
    component.setFilterNum('userId', '');
    expect(component.filter().userId).toBeUndefined();
  });
});
