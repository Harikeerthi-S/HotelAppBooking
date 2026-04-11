import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
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
  private route        = inject(ActivatedRoute);
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

    // Capture returnUrl BEFORE the API call so it's available synchronously
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl')
                   || sessionStorage.getItem('hb_returnUrl')
                   || '';

    this.apiService.apiLogin(model).subscribe({
      next: (response) => {
        this.loading.set(false);

        this.tokenService.setToken(response.token);

        const userId   = this.tokenService.getUserIdFromToken();
        const userName = this.tokenService.getUserNameFromToken();
        const role     = this.tokenService.getRoleFromToken() ?? 'user';

        userLogin({ userId, userName, email: model.email, role });
        sessionStorage.removeItem('hb_returnUrl');

        this.toastr.success(`Welcome back, ${userName || 'User'}!`, 'Login Successful');

        // Redirect: returnUrl takes priority for regular users
        if (returnUrl && role === 'user') {
          // Use window.location for a hard redirect — bypasses any guard re-evaluation
          window.location.href = returnUrl;
        } else if (role === 'admin') {
          window.location.href = '/dashboard-admin';
        } else if (role === 'hotelmanager') {
          window.location.href = '/dashboard-manager';
        } else {
          window.location.href = returnUrl || '/dashboard-user';
        }
      },
      error: (error) => {
        this.loading.set(false);
        const msg = error?.error?.message || error?.error?.Message || 'Invalid email or password.';
        this.toastr.error(msg, 'Login Failed');
      }
    });
  }
}
