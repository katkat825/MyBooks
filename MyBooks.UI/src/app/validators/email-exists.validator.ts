import { AbstractControl, AsyncValidatorFn } from '@angular/forms';
import { map, catchError, of } from 'rxjs';
import { UserService } from '../services/user.service';

export function emailExistsValidator(userService: UserService, excludeUserId: number = 0): AsyncValidatorFn {
  return (control: AbstractControl) => {
    if (!control.value) return of(null);

    return userService.checkEmailExists(control.value, excludeUserId).pipe(
      map(res => (res.exists ? { emailTaken: true } : null)),
      catchError(() => of(null)) // fail-safe
    );
  };
}
