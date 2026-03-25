import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Review } from './review';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideToastr } from 'ngx-toastr';
import { provideAnimations } from '@angular/platform-browser/animations';

describe('Review Component', () => {
  let fixture:   ComponentFixture<Review>;
  let component: Review;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Review],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Review);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('default signals are correct', () => {
    expect(component.reviews()).toEqual([]);
    expect(component.newRating()).toBe(0);
    expect(component.newComment()).toBe('');
    expect(component.filterRating()).toBe(0);
    expect(component.filterHotelId()).toBe(0);
  });

  it('isStarFilled() returns true when star <= hovered or rating', () => {
    component.setRating(3);
    expect(component.isStarFilled(1)).toBeTrue();
    expect(component.isStarFilled(3)).toBeTrue();
    expect(component.isStarFilled(4)).toBeFalse();
  });

  it('hoverStar and clearHover work correctly', () => {
    component.hoverStar(4);
    expect(component.hoveredStar()).toBe(4);
    component.clearHover();
    expect(component.hoveredStar()).toBe(0);
  });

  it('ratingLabel returns correct string', () => {
    expect(component.ratingLabel(5)).toBe('Excellent');
    expect(component.ratingLabel(3)).toBe('Good');
    expect(component.ratingLabel(1)).toBe('Poor');
  });

  it('displayStars returns array of 5 booleans', () => {
    const stars = component.displayStars(3);
    expect(stars.length).toBe(5);
    expect(stars[0]).toBeTrue();
    expect(stars[2]).toBeTrue();
    expect(stars[3]).toBeFalse();
  });

  it('clearFilters resets filter signals', () => {
    component.filterRating.set(4);
    component.filterHotelId.set(7);
    component.clearFilters();
    expect(component.filterRating()).toBe(0);
    expect(component.filterHotelId()).toBe(0);
  });

  it('filtered() returns all reviews when no filter set', () => {
    (component as any).reviews.set([
      { reviewId: 1, hotelId: 1, userId: 1, rating: 5, comment: 'Great', createdAt: '', hotelName: 'H1' },
      { reviewId: 2, hotelId: 2, userId: 1, rating: 3, comment: 'Ok',    createdAt: '', hotelName: 'H2' },
    ]);
    expect(component.filtered().length).toBe(2);
  });

  it('filtered() filters by rating correctly', () => {
    (component as any).reviews.set([
      { reviewId: 1, hotelId: 1, userId: 1, rating: 5, comment: 'Great', createdAt: '', hotelName: 'H1' },
      { reviewId: 2, hotelId: 2, userId: 1, rating: 3, comment: 'Ok',    createdAt: '', hotelName: 'H2' },
    ]);
    component.filterRating.set(5);
    expect(component.filtered().length).toBe(1);
    expect(component.filtered()[0].reviewId).toBe(1);
  });

  it('avgRating computed is correct', () => {
    (component as any).reviews.set([
      { reviewId: 1, hotelId: 1, userId: 1, rating: 4, comment: '', createdAt: '', hotelName: 'H1' },
      { reviewId: 2, hotelId: 2, userId: 1, rating: 2, comment: '', createdAt: '', hotelName: 'H2' },
    ]);
    expect(component.avgRating()).toBe(3);
  });
});
