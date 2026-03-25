import { Component, inject, signal, OnDestroy } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { userLogout, $userStatus } from '../dynamicCommunication/userObservable';
import { ToastrService } from 'ngx-toastr';

/**
 * Profile page — shows only Full Name and Email.
 * userId is decoded from the JWT and used internally across the app
 * but is intentionally NOT displayed here per product requirement.
 */
@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile implements OnDestroy {
  private toast  = inject(ToastrService);
  private router = inject(Router);

  // Only the two fields shown in the UI
  fullName = signal('');
  email    = signal('');
  role     = signal('');
  // userId stored but never bound to the template
  private userId = 0;

  private sub: Subscription;

  constructor() {
    this.sub = $userStatus.subscribe(u => {
      this.fullName.set(u.userName);
      this.email.set(u.email);
      this.role.set(u.role);
      this.userId = u.userId;   // used internally (e.g. API calls), never shown
    });
  }

  ngOnDestroy(): void { this.sub.unsubscribe(); }

  getInitial(): string {
    return this.fullName() ? this.fullName()[0].toUpperCase() : 'U';
  }

  getRoleBadge(): { label: string; cls: string } {
    const r = this.role();
    if (r === 'admin')        return { label: '🛡️ Administrator', cls: 'badge-admin' };
    if (r === 'hotelmanager') return { label: '🏨 Hotel Manager',  cls: 'badge-manager' };
    return                           { label: '🧳 Guest',          cls: 'badge-user' };
  }

  logout(): void {
    userLogout();
    this.toast.success('Logged out successfully.', 'Goodbye!');
    this.router.navigateByUrl('/login');
  }
}
