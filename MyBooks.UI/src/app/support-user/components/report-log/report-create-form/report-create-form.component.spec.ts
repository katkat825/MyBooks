import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReportCreateFormComponent } from './report-create-form.component';

describe('ReportFormComponent', () => {
  let component: ReportCreateFormComponent;
  let fixture: ComponentFixture<ReportCreateFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ReportCreateFormComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ReportCreateFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
