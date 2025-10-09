import { Component } from '@angular/core';

@Component({
  selector: 'app-privacy-policy',
  templateUrl: './privacy-policy.component.html',
  styleUrls: ['./privacy-policy.component.css'],
  standalone: true
})
export class PrivacyPolicyComponent {
  ngOnInit(): void {
    console.log('PrivacyPolicyComponent loaded');
  }
}
