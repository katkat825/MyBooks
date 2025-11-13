import { Component } from '@angular/core';
import { UserService } from '../../services/user.service';
import { GlobalLoadingService } from '../../services/global-loading.service';

@Component({
  selector: 'app-terms-of-service',
  templateUrl: './terms-of-service.component.html',
  styleUrls: ['./terms-of-service.component.css'],
  standalone: true
})
export class TermsOfServiceComponent {
  constructor(private userService: UserService, private globalLoading: GlobalLoadingService) {}

  ngOnInit(): void {
    this.globalLoading.hide();
  }

  acceptPolicy() {
    this.userService.acceptTerms().subscribe({
      next: () => {
        window.location.href = '/';
      },
      error: (error) => console.error('Failed to accept terms: ', error)
    });
  }
}
