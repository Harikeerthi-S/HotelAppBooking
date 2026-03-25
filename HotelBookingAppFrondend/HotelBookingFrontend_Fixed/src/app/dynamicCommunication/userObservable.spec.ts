import { userLogin, userLogout, $userStatus, changeUserStatus } from './userObservable';
import { UserState } from './userObservable';

describe('userObservable', () => {
  const testUser: UserState = {
    userId: 1,
    userName: 'TestUser',
    email: 'test@hotel.com',
    role: 'user'
  };

  beforeEach(() => sessionStorage.clear());
  afterEach(() => sessionStorage.clear());

  it('$userStatus should be an observable', () => {
    expect($userStatus).toBeTruthy();
    expect(typeof $userStatus.subscribe).toBe('function');
  });

  it('userLogin should emit user state to subscribers', () => {
    let received: UserState | null = null;
    $userStatus.subscribe(u => received = u);
    userLogin(testUser);
    expect(received).not.toBeNull();
    expect(received!.userName).toBe('TestUser');
    expect(received!.role).toBe('user');
  });

  it('userLogin should store user in sessionStorage', () => {
    userLogin(testUser);
    const stored = JSON.parse(sessionStorage.getItem('hotel_user') ?? '{}');
    expect(stored.userId).toBe(1);
    expect(stored.userName).toBe('TestUser');
    expect(stored.email).toBe('test@hotel.com');
  });

  it('userLogout should emit empty user state', () => {
    userLogin(testUser);
    let received: UserState | null = null;
    $userStatus.subscribe(u => received = u);
    userLogout();
    expect(received!.userName).toBe('');
    expect(received!.userId).toBe(0);
  });

  it('userLogout should remove token from sessionStorage', () => {
    sessionStorage.setItem('token', 'some-token');
    userLogout();
    expect(sessionStorage.getItem('token')).toBeNull();
  });

  it('userLogout should remove hotel_user from sessionStorage', () => {
    userLogin(testUser);
    userLogout();
    expect(sessionStorage.getItem('hotel_user')).toBeNull();
  });

  it('changeUserStatus should be a BehaviorSubject', () => {
    expect(typeof changeUserStatus.next).toBe('function');
    expect(typeof changeUserStatus.getValue).toBe('function');
  });

  it('multiple subscribers should all receive the same value', () => {
    const received1: UserState[] = [];
    const received2: UserState[] = [];
    $userStatus.subscribe(u => received1.push(u));
    $userStatus.subscribe(u => received2.push(u));
    userLogin(testUser);
    const last1 = received1[received1.length - 1];
    const last2 = received2[received2.length - 1];
    expect(last1.userName).toBe(last2.userName);
  });

  it('admin user login should store role correctly', () => {
    const adminUser: UserState = { ...testUser, role: 'admin', userName: 'AdminUser' };
    userLogin(adminUser);
    const stored = JSON.parse(sessionStorage.getItem('hotel_user') ?? '{}');
    expect(stored.role).toBe('admin');
  });

  it('hotelmanager user login should store role correctly', () => {
    const managerUser: UserState = { ...testUser, role: 'hotelmanager', userName: 'Manager' };
    userLogin(managerUser);
    const stored = JSON.parse(sessionStorage.getItem('hotel_user') ?? '{}');
    expect(stored.role).toBe('hotelmanager');
  });
});
