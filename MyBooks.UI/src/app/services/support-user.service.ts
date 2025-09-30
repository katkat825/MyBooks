import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SupportUserService {
  private authBaseUrl = environment.authBaseUrl;
  private catalogApiUrl = environment.apiUrl;
  private fileApiUrl = environment.fileApiUrl;
  private tenantApiUrl = environment.tenantApiUrl;

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    });
  }

  // impersonation
  impersonate(userId: number): Observable<{ token: string; logId: number }> {
    const currentToken = localStorage.getItem('token');
    if(currentToken) {
      localStorage.setItem('originalToken', currentToken);
    }

    return this.http.post<{ token: string; logId: number }>(
      `${this.authBaseUrl}/api/Impersonation/${userId}`, {}, { headers: this.getAuthHeaders() }
    );
  }

  stopImpersonation(logId: number): Observable<void> {
    return this.http.post<void>(
      `${this.authBaseUrl}/api/Impersonation/stop/${logId}`, {}, { headers: this.getAuthHeaders() }
    );
  }

  impersonateAccount(tenantId: number): Observable<{ token: string; logId: number }> {
    const currentToken = localStorage.getItem('token');
    if(currentToken) {
      localStorage.setItem('originalToken', currentToken);
    }

    return this.http.post<{ token: string; logId: number }>(
      `${this.authBaseUrl}/api/Impersonation/tenant/${tenantId}`, {}, { headers: this.getAuthHeaders() }
    );
  }

  // books
  getAllBooks(): Observable<any[]> {
    return this.http.get<any[]>(`${this.catalogApiUrl}/support/book`, { headers: this.getAuthHeaders() });
  }

  getBookById(id: number): Observable<any> {
    return this.http.get<any>(`${this.catalogApiUrl}/support/book/${id}`, { headers: this.getAuthHeaders() });
  }

  toggleBookRestricted(id: number, restricted: boolean): Observable<void> {
    return this.http.patch<void>(
      `${this.catalogApiUrl}/support/book/${id}/restricted?restricted=${restricted}`, {}, { headers: this.getAuthHeaders() }
    );
  }

  updateBookFileLink(bookId: number, fileId: number): Observable<void> {
    return this.http.patch<void>(
      `${this.catalogApiUrl}/support/book/${bookId}/file`,
      { bookId, fileId },
      { headers: this.getAuthHeaders() }
    );
  }

  // files
  getAllFilesForBook(bookId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.fileApiUrl}/support/file/book/${bookId}`, { headers: this.getAuthHeaders() });
  }

  activateFile(fileId: number): Observable<void> {
    return this.http.patch<void>(
      `${this.fileApiUrl}/support/file/${fileId}/activate`, {}, { headers: this.getAuthHeaders() }
    );
  }

  // tenants
  getAllTenants(): Observable<any> {
    return this.http.get<any[]>(`${this.tenantApiUrl}/tenant`, { headers: this.getAuthHeaders() });
  }

  getTenantById(id: number): Observable<any> {
    return this.http.get<any>(`${this.tenantApiUrl}/tenant/${id}`, { headers: this.getAuthHeaders() });
  }

  createTenant(dto: any): Observable<any> {
    return this.http.post<any>(`${this.tenantApiUrl}/tenant`, dto, { headers: this.getAuthHeaders() });
  }

  updateTenant(id: number, dto: any): Observable<void> {
    return this.http.put<void>(`${this.tenantApiUrl}/tenant/${id}`, dto, { headers: this.getAuthHeaders() });
  }

  deactivateTenant(id: number): Observable<void> {
    return this.http.patch<void>(`${this.tenantApiUrl}/tenant/${id}/deactivate`, {}, { headers: this.getAuthHeaders() });
  }

  activateTenant(id: number): Observable<void> {
    return this.http.patch<void>(`${this.tenantApiUrl}/tenant/${id}/activate`, {}, { headers: this.getAuthHeaders() });
  }

  // users
  getAllUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.authBaseUrl}/api/users/all-users`, {
      headers: this.getAuthHeaders(),
    });
  }
}
