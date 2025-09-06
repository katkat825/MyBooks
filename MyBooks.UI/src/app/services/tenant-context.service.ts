import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { BehaviorSubject, Observable } from "rxjs";

export interface TenantContext {
    id: number;
    name: string;
    subdomain: string;
    isActive: boolean;
    allowExternalIntegrations: boolean;
    allowStorage: boolean;
    maxStorageMb: number;
    maxUsers: number;
}

@Injectable({
  providedIn: "root"
})

export class TenantContextService {
  private tenantContext$ = new BehaviorSubject<TenantContext | null>(null);

  constructor(private http: HttpClient) {}

  getSubdomain(): string | null {
    const host = window.location.host;
    const parts = host.split(".");

    /*if (parts.length < 3) {
      return null;
    }*/

    const subdomain = parts[0];
    /*if(subdomain.toLowerCase() === 'www') {
        return null;
    }*/

    return subdomain;
  }

  loadTenantContext(): Observable<TenantContext> {
    const subdomain = this.getSubdomain();
    if (!subdomain) {
      throw new Error("Subdomain not found");
    }

    const request$ = this.http.get<TenantContext>(`/api/tenant/by-subdomain/${subdomain}`);

    request$.subscribe({
      next: (context) => this.tenantContext$.next(context),
      error: () => this.tenantContext$.error(null)
    });

    return request$;
  }

  getTenantContext(): Observable<TenantContext | null> {
    return this.tenantContext$.asObservable();
  }

  currentTenant(): TenantContext | null {
    return this.tenantContext$.value;
  }

  checkSubdomainAvailability(subdomain:string) {
    return this.http.get<{available: boolean}>(`/api/tenant/check-subdomain/${subdomain}`);
  }
}