import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { UserService } from '../services/user.service';
import { Observable, of } from 'rxjs';
import { map, catchError, filter, take } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class TermsGuard implements CanActivate {
  constructor(private userService: UserService, private router: Router) { }

  canActivate(): Observable<boolean | UrlTree> {
    const hasToken = !!localStorage.getItem('token');
    if(!hasToken) return of(this.router.createUrlTree(['/login']));

    return this.userService.ensureProfile$().pipe(
      filter((u): u is any => u !== null),
      take(1),
      map(user => {
        const accepted = user.acceptedAup ?? user.AcceptedAup ?? false;
        return accepted ? true : this.router.createUrlTree(['/terms']);
      })
    );
  }
}
