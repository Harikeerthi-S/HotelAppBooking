import { BehaviorSubject } from 'rxjs';

export interface UserState {
  userId:   number;
  userName: string;
  email:    string;
  role:     string;
}

const emptyUser: UserState = { userId: 0, userName: '', email: '', role: '' };

// FIX: localStorage persists across tab refresh
const USER_KEY = 'hb_user';

export const changeUserStatus = new BehaviorSubject<UserState>(loadUserFromStorage());
export const $userStatus       = changeUserStatus.asObservable();

export function userLogin(user: UserState): void {
  localStorage.setItem(USER_KEY, JSON.stringify(user));
  changeUserStatus.next(user);
}

export function userLogout(): void {
  localStorage.removeItem('hb_token');
  localStorage.removeItem(USER_KEY);
  changeUserStatus.next(emptyUser);
}

export function getCurrentUser(): UserState {
  return changeUserStatus.getValue();
}

function loadUserFromStorage(): UserState {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : emptyUser;
  } catch { return emptyUser; }
}
