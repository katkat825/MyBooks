import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-acceptable-use-policy',
  templateUrl: './acceptable-use-policy.component.html',
  styleUrls: ['./acceptable-use-policy.component.css']
})
export class AcceptableUsePolicyComponent {
  constructor(private userService: UserService, private router: Router) { }

  acceptPolicy() {
    this.userService.acceptAup().subscribe({
      next: () => {
        window.location.href = '/';
      },
      error: (error) => console.error('Failed to accept AUP:', error)
    });
  }
}
