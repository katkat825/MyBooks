import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, filter, map, take } from 'rxjs/operators';
import { UserService } from '../services/user.service';

@Injectable({
  providedIn: 'root'
})
export class GlobalReviewerGuard implements CanActivate {

    constructor(private router: Router, private userService: UserService) {}

    canActivate(): Observable<boolean | UrlTree> {
        const hasToken = !!localStorage.getItem('token');
        if (!hasToken) {
            return of(this.router.createUrlTree(['/login']));
        }    

        return this.userService.user$.pipe(
            filter((u): u is any => !!u),
            take(1),
            map(user => {
                const role = user.role ?? user.Role ?? '';
                const allowed = role === 'SuperAdmin' || role === 'GlobalReviewer';
                console.log('GlobalReviewerGuard allowed?', allowed, 'role:', role);
                return allowed ? true : this.router.createUrlTree(['/']);
            }),
            catchError(() => of(this.router.createUrlTree(['/login'])))
        );
    }
}