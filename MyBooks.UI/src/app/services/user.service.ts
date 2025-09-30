import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError, tap, map, BehaviorSubject, shareReplay, EMPTY, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { Router } from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private jwtHelper = new JwtHelperService();
  private usersApiUrl = `${environment.authServiceUrl}/users`;
  private accountApiUrl = `${environment.authServiceUrl}/account`;
  private userSubject = new BehaviorSubject<any>(null);
  user$ = this.userSubject.asObservable();

  canAccessAdmin$ = this.user$.pipe(
    map(u => !!u && (u.role === 'Admin' || u.role === 'Editor' || u.role === 'SuperAdmin' || u.role === 'Owner' || u.role === 'Support')),
    shareReplay({ bufferSize: 1, refCount: true })
  );

  canAccessOwner$ = this.user$.pipe(
    map(u => !!u && (u.role === 'SuperAdmin' || u.role === 'Owner' || u.role === 'Support')),
    shareReplay({ bufferSize: 1, refCount: true})
  );

  canAccessSupport$ = this.user$.pipe(
    map(u => !!u && u.role === 'SuperAdmin'),
    shareReplay({ bufferSize: 1, refCount: true })
  );

  constructor(private http: HttpClient, private router: Router) { }

  get isImpersonating(): boolean {
    const token = localStorage.getItem('token');
    if (!token) return false;

    const decoded = this.jwtHelper.decodeToken(token);
    return decoded?.IsImpersonating === 'true';
  }

  public getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    });
  }

  getProfile(): Observable<any | null> {
    return this.user$;
  }

  loadProfile(): void {
    const token = localStorage.getItem('token');
    if (!token) {
      this.userSubject.next(null);
      return;
    }

    this.http.get<any>(`${this.accountApiUrl}/profile`, { headers: this.getAuthHeaders() })
      .pipe(
        catchError((error: HttpErrorResponse) => {
          if (error.status === 401) {
            // Keep SPA flow: clear and route
            localStorage.removeItem('token');
            this.userSubject.next(null);
            this.router.navigate(['/login']);
            return EMPTY;
          }
          console.error('Profile load error:', error);
          // Don’t blow up the app; just emit null
          this.userSubject.next(null);
          return EMPTY;
        })
      )
      .subscribe(user => this.userSubject.next(user));
  }

  getUsers(includeSupport = false): Observable<any[]> {
    return this.http.get<any[]>(this.usersApiUrl, { headers: this.getAuthHeaders() }).pipe(
      map(users => includeSupport ? users : users.filter(u => !this.isSupportAccount(u))),
      catchError(error => {
        console.error("Error fetching users:", error);
        return throwError(() => error);
      })
    );
  }

  private isSupportAccount(u: any): boolean {
    if (!u) return false;
    // Hide by role…
    if (u.role === 'SuperAdmin' || u.role === 'Support') return true;
    // …and/or by service email convention (adjust if you picked a different prefix)
    const email = String(u.email || '').toLowerCase();
    return email.startsWith('svc+') && email.endsWith('@mybookcatalog.com');
  }

  getUserById(id: number): Observable<any> {
    return this.http.get<any>(`${this.usersApiUrl}/${id}`, { headers: this.getAuthHeaders() }).pipe(
      catchError(error => {
        console.error("Error fetching user: ", error);
        return throwError(error);
      })
    );
  }  

  updateUser(id: number, updates: any): Observable<any> {
    return this.http.patch<any>(`${this.usersApiUrl}/${id}`, updates, { headers: this.getAuthHeaders() }).pipe(
      tap(response => {
        console.log("api response (patch_: ", response);
      }),
      catchError(error => {
        console.error("Error updating user:", error);
        return throwError(error);
      })
    );
  }

  createUser(user: any): Observable<any> {
    user.password = crypto.randomUUID();
    
    return this.http.post<any>(`${this.usersApiUrl}/register`, user, { headers: this.getAuthHeaders() }).pipe(
      catchError(error => {
        console.error("Error creating user:", error);
        return throwError(error);
      })
    );
  }

  deactivateUser(id: number): Observable<any> {
    return this.http.patch<any>(`${this.usersApiUrl}/deactivate/${id}`, {}, { headers: this.getAuthHeaders() }).pipe(
      tap(() => console.log("successfully deactivated user ID: ${id}")),
      catchError(error => {
        console.error("error deactivating user. ", error);
        return throwError(() => new Error("Failed to deactivate user."));
      })
    );
  }

  reactivateUser(id: number): Observable<any> {
    return this.http.patch<any>(`${this.usersApiUrl}/reactivate/${id}`, {}, { headers: this.getAuthHeaders() }).pipe(
      tap(() => console.log(`✅ Successfully reactivated user ID: ${id}`)),
      catchError(error => {
        console.error("❌ Error reactivating user:", error);
        return throwError(() => new Error("Failed to reactivate user."));
      })
    );
  }

  // soft-delete only
  deleteUser(id: number): Observable<any> {
    return this.http.patch<any>(`${this.usersApiUrl}/delete/${id}`, {}, { headers: this.getAuthHeaders() }).pipe(
      tap(() => console.log(`✅ Successfully deleted user ID: ${id}`)),
      catchError(error => {
        console.error("❌ Error deleting user:", error);
        return throwError(() => new Error("Failed to delete user."));
      })
    );
  }

  updateProfile(dto: any): Observable<any> {
    return this.http.patch<any>(`${environment.authServiceUrl}/account/profile`, dto, { headers: this.getAuthHeaders() }).pipe(
      tap(response => console.log("Profile updated:", response)),
      catchError(error => {
        console.error("Error updating profile:", error);
        return throwError(error);
      })
    );
  }

  clearSession(): void {
    localStorage.removeItem('token');
    this.userSubject.next(null);
  }

  getAgeCategories(): Observable<any[]> {
    return this.http.get<any>(`${environment.apiUrl}/books/agecategories`, { headers: this.getAuthHeaders() }).pipe(
      map(response => Array.isArray(response) ? response : response?.$values || []),
      catchError(error => {
        console.error("Error fetching age categories:", error);
        return throwError(error);
      })
    );
  }

  acceptAup(): Observable<any> {
    const payload = {
      AcceptedAup: true
    };
    return this.updateProfile(payload).pipe(
      tap(response => console.log("AUP accepted:", response)),
      catchError(error => {
        console.error("Error updating AUP:", error);
        return throwError(error);
      })
    );
  }

  ensureProfile$(): Observable<any | null> {
    const hasToken = !!localStorage.getItem('token');
    if (!hasToken) {
      this.userSubject.next(null);
      return of(null);
    }
    if (this.userSubject.value === null) {
      this.loadProfile();
    }
    return this.user$; 
  }
}
