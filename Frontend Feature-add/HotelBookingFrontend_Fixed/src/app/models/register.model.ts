export class RegisterModel {
  userName: string = '';
  email: string = '';
  password: string = '';
  phone?: string;   // optional — backend Phone is nullable string?
  role: string = 'user';
}
