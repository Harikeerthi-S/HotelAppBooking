export class UserAmenityPreferenceModel {
  preferenceId: number = 0;
  userId: number = 0;
  userName: string = '';
  amenityId: number = 0;
  amenityName: string = '';
  amenityIcon?: string;
  createdAt: string = '';
  /** Pending | Approved | Rejected */
  status: string = 'Pending';
}

export class CreateUserAmenityPreferenceModel {
  userId: number = 0;
  amenityId: number = 0;
}
