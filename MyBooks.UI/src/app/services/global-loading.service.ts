import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class GlobalLoadingService {
  private _isVisible = new BehaviorSubject<boolean>(false);
  private _message = new BehaviorSubject<string>('Please wait...');

  // public observables
  isVisible$ = this._isVisible.asObservable();
  message$ = this._message.asObservable();

  show(message: string = 'Please wait...') {
    this._message.next(message);
    this._isVisible.next(true);
  }

  hide() {
    this._isVisible.next(false);
  }
}
