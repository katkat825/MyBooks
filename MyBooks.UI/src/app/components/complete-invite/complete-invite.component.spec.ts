import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CompleteInviteComponent } from './complete-invite.component';

describe('CompleteInviteComponent', () => {
  let component: CompleteInviteComponent;
  let fixture: ComponentFixture<CompleteInviteComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CompleteInviteComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CompleteInviteComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
