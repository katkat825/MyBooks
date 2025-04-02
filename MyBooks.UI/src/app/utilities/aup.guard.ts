import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { UserService } from '../services/user.service';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AupGuard implements CanActivate {
  constructor(private userService: UserService, private router: Router) { }

  canActivate(): Observable<boolean> {
    return this.userService.getProfile().pipe(
      map(profile => {
        if (profile.acceptedAup) {
          return true;
        } else {
          this.router.navigate(['/aup']);
          return false;
        }
      }),
      catchError(error => {
        // Redirect to login if there is an error fetching the profile
        this.router.navigate(['/login']);
        return of(false);
      })
    );
  }
}
