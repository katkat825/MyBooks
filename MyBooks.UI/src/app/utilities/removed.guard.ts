import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, CanActivate, GuardResult, MaybeAsync, RouterStateSnapshot } from "@angular/router";

@Injectable({
    providedIn: 'root'
})

export class RemovedGuard implements CanActivate {
    canActivate(): boolean {
       return false; 
    }
}