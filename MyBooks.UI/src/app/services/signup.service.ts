import { Injectable } from '@angular/core';
import { HttpClient, HttpParamsOptions } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SignupRequest {
  subdomain: string;
  Email: string;
  Password: string;
  firstName: string;
  lastName: string;
}

export interface SignupResponse {
  tenantId: number;
  ownerUserId: number;
  portalUrl: string;
}

@Injectable({
  providedIn: 'root'
})

export class SignupService {
  private apiUrl = `${environment.tenantApiUrl}/signup`;

  constructor(private http: HttpClient) {}

  createTenant(request: SignupRequest): Observable<SignupResponse> {
    return this.http.post<SignupResponse>(this.apiUrl, request);
  }  

  checkSubdomainAvailability(subdomain:string) {
    return this.http.get<{available: boolean}>(`${environment.tenantApiUrl}/tenant/check-subdomain/${subdomain}`);
  }
}