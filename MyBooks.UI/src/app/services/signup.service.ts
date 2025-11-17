import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SignupRequest {
  billingPlanId?: number; 
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface SignupResponse {
  tenantId: number;
  ownerUserId: number;
}

@Injectable({
  providedIn: 'root'
})

export class SignupService {
  private signupUrl = `${environment.tenantBaseUrl}/signup`;

  constructor(private http: HttpClient) {}

  public getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    });
  }

  createTenant(request: SignupRequest): Observable<SignupResponse> {
    return this.http.post<SignupResponse>(this.signupUrl, request, {headers: this.getAuthHeaders() });
  }
}
