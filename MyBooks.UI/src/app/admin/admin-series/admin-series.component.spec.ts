import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminSeriesComponent } from './admin-series.component';

describe('AdminSeriesComponent', () => {
  let component: AdminSeriesComponent;
  let fixture: ComponentFixture<AdminSeriesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminSeriesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminSeriesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
