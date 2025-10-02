import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReportUpdateFormComponent } from './report-update-form.component';

describe('ReportUpdateFormComponent', () => {
  let component: ReportUpdateFormComponent;
  let fixture: ComponentFixture<ReportUpdateFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ReportUpdateFormComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ReportUpdateFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
