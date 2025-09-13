import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GoogleDriveComponent } from './google-drive.component';

describe('GoogleDriveComponent', () => {
  let component: GoogleDriveComponent;
  let fixture: ComponentFixture<GoogleDriveComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GoogleDriveComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GoogleDriveComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
