import { Component } from '@angular/core';

@Component({
  selector: 'app-terms-of-service',
  templateUrl: './terms-of-service.component.html',
  styleUrls: ['./terms-of-service.component.css'],
  standalone: true
})
export class TermsOfServiceComponent {
  ngOnInit(): void {
    console.log('TermsOfServiceComponent loaded');
  }
}
