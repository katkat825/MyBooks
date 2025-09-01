import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReportAbuseComponent } from './report-abuse.component';

describe('ReportAbuseComponent', () => {
  let component: ReportAbuseComponent;
  let fixture: ComponentFixture<ReportAbuseComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ReportAbuseComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ReportAbuseComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
