import { Injectable } from '@angular/core';
import { HttpClient, HttpParamsOptions } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SignupRequest {
  tenantName: string;
  subdomain: string;
  ownerEmail: string;
  ownerPassword: string;
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
  private apiUrl = `#{environment.tenantApiUrl}/signup`;

  constructor(private http: HttpClient) {}

  createTenant(request: SignupRequest): Observable<SignupResponse> {
    return this.http.post<SignupResponse>(this.apiUrl, request);
  }
}