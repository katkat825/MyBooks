import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError, tap, map } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${environment.authServiceUrl}/users`;

  constructor(private http: HttpClient) { }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    });
  }

  getProfile(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/profile`, { headers: this.getAuthHeaders() }).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          console.error("❌ User is unauthorized. Logging out...");
          localStorage.removeItem('token'); // ❌ Remove invalid token
          window.location.href = '/login'; // 🚀 Redirect to login
        }
        return throwError(() => error);
      })
    );
  }

  getUsers(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl, { headers: this.getAuthHeaders() }).pipe(
      catchError(error => {
        console.error("Error fetching users:", error);
        return throwError(error);
      })
    );
  }

  getUserById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`, { headers: this.getAuthHeaders() }).pipe(
      catchError(error => {
        console.error("Error fetching user: ", error);
        return throwError(error);
      })
    );
  }  

  updateUser(id: number, updates: any): Observable<any> {
    return this.http.patch<any>(`${this.apiUrl}/${id}`, updates, { headers: this.getAuthHeaders() }).pipe(
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
    console.log("sending user payload: ", user);
    return this.http.post<any>(`${this.apiUrl}/register`, user, { headers: this.getAuthHeaders() }).pipe(
      catchError(error => {
        console.error("Error creating user:", error);
        return throwError(error);
      })
    );
  }

  deactivateUser(id: number): Observable<any> {
    return this.http.patch<any>(`${this.apiUrl}/deactivate/${id}`, {}, { headers: this.getAuthHeaders() }).pipe(
      tap(() => console.log("successfully deactivated user ID: ${id}")),
      catchError(error => {
        console.error("error deactivating user. ", error);
        return throwError(() => new Error("Failed to deactivate user."));
      })
    );
  }

  reactivateUser(id: number): Observable<any> {
    return this.http.patch<any>(`${this.apiUrl}/reactivate/${id}`, {}, { headers: this.getAuthHeaders() }).pipe(
      tap(() => console.log(`✅ Successfully reactivated user ID: ${id}`)),
      catchError(error => {
        console.error("❌ Error reactivating user:", error);
        return throwError(() => new Error("Failed to reactivate user."));
      })
    );
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
}
