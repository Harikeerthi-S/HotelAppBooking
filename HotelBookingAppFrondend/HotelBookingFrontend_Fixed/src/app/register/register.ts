import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { APIService } from '../services/api.service';
import { RegisterModel } from '../models/register.model';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  private apiService = inject(APIService);
  private router     = inject(Router);
  private toastr     = inject(ToastrService);

  loading  = signal(false);
  showPass = signal(false);

  registerForm = new FormGroup({
    userName: new FormControl('', [Validators.required, Validators.minLength(2)]),
    email:    new FormControl('', [Validators.required, Validators.email]),
    phone:    new FormControl(''),
    password: new FormControl('', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[A-Z])(?=.*[0-9]).+$/)
    ])
  });

  get userName() { return this.registerForm.get('userName'); }
  get email()    { return this.registerForm.get('email'); }
  get password() { return this.registerForm.get('password'); }

  register(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }
    this.loading.set(true);

    const model    = new RegisterModel();
    model.userName = this.userName!.value!;
    model.email    = this.email!.value!;
    model.phone    = this.registerForm.get('phone')!.value ?? '';
    model.password = this.password!.value!;
    model.role     = 'user';   // always register as user

    this.apiService.apiRegister(model).subscribe({
      next: () => {
        this.loading.set(false);
        this.toastr.success('Account created! Please login.', 'Welcome to StayEase!');
        this.router.navigateByUrl('/login');
      },
      error: (error) => {
        this.loading.set(false);
        const msg = error?.error?.message || error?.error?.Message || 'Registration failed. Try again.';
        this.toastr.error(msg, 'Error');
      }
    });
  }
}
