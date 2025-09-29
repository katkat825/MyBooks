import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface InvitationValidation {
  email: string;
  firstName: string;
  lastName: string;
}

export interface CompleteInvitationDto {
  token: string;
  password: string;
}

@Injectable({
  providedIn: 'root'
})
export class InviteService {
  private apiUrl = `${environment.authServiceUrl}/invitation`;

  constructor(private http: HttpClient) {}

  validate(token: string): Observable<InvitationValidation> {
    return this.http.post<InvitationValidation>(
      `${this.apiUrl}/validate`,
      JSON.stringify(token),  // ensure raw string
      { headers: { 'Content-Type': 'application/json' } }
    );
  }

  complete(dto: CompleteInvitationDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/complete`, dto);
  }

  resend(email: string): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/resend`,
      JSON.stringify(email),   // send raw string
      { headers: { 'Content-Type': 'application/json' } }
    );
  }
}
