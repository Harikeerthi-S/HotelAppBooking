import { Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { $userStatus, userLogout } from '../dynamicCommunication/userObservable';
import { TokenService } from '../services/token.service';

@Component({
  selector: 'app-menu',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './menu.html',
  styleUrl: './menu.css'
})
export class Menu {
  userName = signal('');
  role = signal('');
  dropOpen = signal(false);

  constructor(private tokenService: TokenService, private router: Router) {
    $userStatus.subscribe({
      next: (user) => {
        this.userName.set(user.userName);
        this.role.set(user.role);
      }
    });
  }

  getInitial(): string {
    return this.userName() ? this.userName()[0].toUpperCase() : 'U';
  }

  toggleDrop(): void {
    this.dropOpen.update(v => !v);
  }

  logout(): void {
    userLogout();
    this.dropOpen.set(false);
    this.router.navigateByUrl('/login');
  }

  isLoggedIn(): boolean {
    return this.tokenService.isLoggedIn();
  }
}
