import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddGoogleDriveFolderComponent } from './add-google-drive-folder.component';

describe('AddGoogleDriveFolderComponent', () => {
  let component: AddGoogleDriveFolderComponent;
  let fixture: ComponentFixture<AddGoogleDriveFolderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AddGoogleDriveFolderComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AddGoogleDriveFolderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
