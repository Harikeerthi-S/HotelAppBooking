import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { APIService } from '../services/api.service';
import { TokenService } from '../services/token.service';
import { userLogin } from '../dynamicCommunication/userObservable';
import { LoginModel } from '../models/login.model';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private apiService   = inject(APIService);
  private tokenService = inject(TokenService);
  private router       = inject(Router);
  private toastr       = inject(ToastrService);

  loading  = signal(false);
  showPass = signal(false);

  loginForm = new FormGroup({
    email:    new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(6)])
  });

  get email()    { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }

  login(): void {
    if (this.loginForm.invalid) { this.loginForm.markAllAsTouched(); return; }
    this.loading.set(true);

    const model    = new LoginModel();
    model.email    = this.email!.value!;
    model.password = this.password!.value!;

    this.apiService.apiLogin(model).subscribe({
      next: (response) => {
        this.loading.set(false);

        // FIX: AuthController returns only { token } (TokenResponseDto)
        // Decode userId, userName, role from the JWT using TokenService methods
        this.tokenService.setToken(response.token);  // stored in localStorage via 'hb_token'

        const userId   = this.tokenService.getUserIdFromToken();   // ClaimTypes.NameIdentifier
        const userName = this.tokenService.getUserNameFromToken(); // ClaimTypes.Name
        const role     = this.tokenService.getRoleFromToken() ?? 'user'; // ClaimTypes.Role

        // Store full user state in localStorage via Observable
        userLogin({ userId, userName, email: model.email, role });

        this.toastr.success(`Welcome back, ${userName || 'User'}!`, 'Login Successful');

        if (role === 'admin')             this.router.navigateByUrl('/dashboard-admin');
        else if (role === 'hotelmanager') this.router.navigateByUrl('/dashboard-manager');
        else                              this.router.navigateByUrl('/dashboard-user');
      },
      error: (error) => {
        this.loading.set(false);
        const msg = error?.error?.message || error?.error?.Message || 'Invalid email or password.';
        this.toastr.error(msg, 'Login Failed');
      }
    });
  }
}
