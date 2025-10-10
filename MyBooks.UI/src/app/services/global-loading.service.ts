import { Injectable } from '@angular/core';
import { BehaviorSubject, interval, Subscription } from 'rxjs';
import { LOADING_MESSAGES, LOGIN_MESSAGES, BULK_IMPORT_MESSAGES, BOOK_VIEWER_MESSAGES } from '../components/shared/loading-messages';

export enum LoadingContext {
  Default,
  Login,
  BulkImport,
  BookViewer
}

@Injectable({
  providedIn: 'root'
})

export class GlobalLoadingService {
  private _isVisible = new BehaviorSubject<boolean>(false);
  private _message = new BehaviorSubject<string>('Please wait...');
  private _funMessage = new BehaviorSubject<string>('');

  isVisible$ = this._isVisible.asObservable();
  message$ = this._message.asObservable();
  funMessage$ = this._funMessage.asObservable();

  private rotationSub?: Subscription;
  private currentPool: string[] = LOADING_MESSAGES;

  private buildPool(context: LoadingContext): string[] {
    const multiplier = 3;
    let extras: string[] = [];

    switch (context) {
      case LoadingContext.Login:
        extras = LOGIN_MESSAGES;
        break;
      case LoadingContext.BulkImport:
        extras = BULK_IMPORT_MESSAGES;
        break;
      case LoadingContext.BookViewer:
        extras = BOOK_VIEWER_MESSAGES;
        break;
      default:
        extras = [];
    }

    const weightedExtras = extras.flatMap(msg => Array(multiplier).fill(msg));
    return [...LOADING_MESSAGES, ...weightedExtras];
  }

  private getRandomMessage(exclude?: string): string {
    let options = this.currentPool;
    if (exclude) {
      options = options.filter(msg => msg !== exclude);
    }
    const idx = Math.floor(Math.random() * options.length);
    return options[idx];
  }

  show(message: string = 'Please wait...', context: LoadingContext = LoadingContext.Default) {
    this.currentPool = this.buildPool(context);
    this._message.next(message);
    this._isVisible.next(true);
    this._funMessage.next(this.getRandomMessage());
    this.rotationSub = interval(6000).subscribe(() => {
      this._funMessage.next(this.getRandomMessage(this._funMessage.value));
    });
  }

  hide() {
    this._isVisible.next(false);
    this._funMessage.next('');
    if (this.rotationSub) {
      this.rotationSub.unsubscribe();
      this.rotationSub = undefined;
    }
  }
}
