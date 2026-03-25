import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Room } from './room';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';
import { RoomModel } from '../models/room.model';
import { HotelModel } from '../models/hotel.model';

/* ── Fake data matching backend DTOs exactly ──────────────────────────────
   RoomModel   uses imageUrl  (NOT imagePath)
   HotelModel  uses imagePath (NOT imageUrl)
   ─────────────────────────────────────────────────────────────────────── */
const fakeHotels: HotelModel[] = [
  {
    hotelId: 1, hotelName: 'Grand Palace', imagePath: '',
    location: 'Mumbai', address: 'Marine Drive',
    totalRooms: 50, starRating: 5, contactNumber: '9999999999'
  },
  {
    hotelId: 2, hotelName: 'Sea View', imagePath: '',
    location: 'Goa', address: 'Beach Road',
    totalRooms: 30, starRating: 4, contactNumber: '8888888888'
  }
];

const fakeRooms: RoomModel[] = [
  {
    roomId: 1, hotelId: 1, roomNumber: 101, roomType: 'Deluxe',
    pricePerNight: 3000, capacity: 2, isAvailable: true,
    imageUrl: 'https://example.com/room1.jpg'
  },
  {
    roomId: 2, hotelId: 1, roomNumber: 102, roomType: 'Suite',
    pricePerNight: 6000, capacity: 4, isAvailable: true,
    imageUrl: ''
  },
  {
    roomId: 3, hotelId: 2, roomNumber: 201, roomType: 'Standard',
    pricePerNight: 1200, capacity: 2, isAvailable: false,
    imageUrl: ''
  }
];

describe('Room', () => {
  let component: Room;
  let fixture:   ComponentFixture<Room>;
  let httpMock:  HttpTestingController;

  function flushInit(): void {
    /* ngOnInit fires: loadHotels() then loadRooms() */
    httpMock
      .expectOne(r => r.url.includes('hotel/paged') && r.method === 'POST')
      .flush({ data: fakeHotels, totalRecords: 2, totalPages: 1, pageNumber: 1, pageSize: 100 });

    httpMock
      .expectOne(r => r.url.endsWith('/room') && r.method === 'GET' && !r.params.has('hotelId'))
      .flush(fakeRooms);
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports:   [Room],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Room);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    flushInit();
    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  /* ── Creation ──────────────────────────────────────────────────────── */
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  /* ── Data loading ──────────────────────────────────────────────────── */
  it('should load 3 rooms on init', () => {
    expect(component.rooms().length).toBe(3);
  });

  it('should load 2 hotels for the dropdown', () => {
    expect(component.hotels().length).toBe(2);
  });

  it('loading should be false after data loaded', () => {
    expect(component.loading()).toBeFalse();
  });

  /* ── Computed counts ───────────────────────────────────────────────── */
  it('availableCount should be 2', () => {
    expect(component.availableCount).toBe(2);
  });

  it('inactiveCount should be 1', () => {
    expect(component.inactiveCount).toBe(1);
  });

  /* ── Default form state ────────────────────────────────────────────── */
  it('editRoomId defaults to null', () => {
    expect(component.editRoomId()).toBeNull();
  });

  it('saving defaults to false', () => {
    expect(component.saving()).toBeFalse();
  });

  it('filterHotelId defaults to 0 (All Hotels)', () => {
    expect(component.filterHotelId()).toBe(0);
  });

  it('form defaults: hotelId 0, roomType Standard, imageUrl empty', () => {
    const f = component.rf();
    expect(f.hotelId).toBe(0);
    expect(f.roomType).toBe('Standard');
    expect(f.pricePerNight).toBe(1000);
    expect(f.imageUrl).toBe('');       // room uses imageUrl
  });

  /* ── roomTypes list ────────────────────────────────────────────────── */
  it('should have 5 room types including Deluxe and Suite', () => {
    expect(component.roomTypes.length).toBe(5);
    expect(component.roomTypes).toContain('Deluxe');
    expect(component.roomTypes).toContain('Suite');
    expect(component.roomTypes).toContain('Standard');
  });

  /* ── editRoom ──────────────────────────────────────────────────────── */
  it('editRoom should populate form with room values including imageUrl', () => {
    component.editRoom(fakeRooms[0]);
    expect(component.editRoomId()).toBe(1);
    expect(component.rf().hotelId).toBe(1);
    expect(component.rf().roomType).toBe('Deluxe');
    expect(component.rf().pricePerNight).toBe(3000);
    expect(component.rf().capacity).toBe(2);
    expect(component.rf().imageUrl).toBe('https://example.com/room1.jpg');
  });

  it('editRoom with empty imageUrl should set imageUrl to empty string', () => {
    component.editRoom(fakeRooms[1]);
    expect(component.rf().imageUrl).toBe('');
  });

  /* ── resetRoom ─────────────────────────────────────────────────────── */
  it('resetRoom should clear editRoomId and reset form to defaults', () => {
    component.editRoom(fakeRooms[0]);
    component.resetRoom();
    expect(component.editRoomId()).toBeNull();
    expect(component.rf().hotelId).toBe(0);
    expect(component.rf().roomType).toBe('Standard');
    expect(component.rf().imageUrl).toBe('');
  });

  /* ── getHotelName ──────────────────────────────────────────────────── */
  it('getHotelName returns correct name for known hotelId', () => {
    expect(component.getHotelName(1)).toBe('Grand Palace');
    expect(component.getHotelName(2)).toBe('Sea View');
  });

  it('getHotelName returns fallback for unknown hotelId', () => {
    expect(component.getHotelName(999)).toBe('Hotel #999');
  });

  /* ── getRoomIcon ───────────────────────────────────────────────────── */
  it('getRoomIcon returns 🏰 for suite', () => {
    expect(component.getRoomIcon('suite')).toBe('🏰');
  });

  it('getRoomIcon returns 💎 for deluxe', () => {
    expect(component.getRoomIcon('deluxe')).toBe('💎');
  });

  it('getRoomIcon returns default 🛏️ for unknown type', () => {
    expect(component.getRoomIcon('unknown')).toBe('🛏️');
  });

  /* ── getStatusClass ────────────────────────────────────────────────── */
  it('getStatusClass returns badge-confirmed for available room', () => {
    expect(component.getStatusClass(true)).toBe('badge-confirmed');
  });

  it('getStatusClass returns badge-cancelled for inactive room', () => {
    expect(component.getStatusClass(false)).toBe('badge-cancelled');
  });

  /* ── saveRoom validation ───────────────────────────────────────────── */
  it('saveRoom should NOT call API when hotelId is 0', () => {
    component.rf.set({ hotelId: 0, roomNumber: 101, roomType: 'Standard', pricePerNight: 1000, capacity: 2, imageUrl: '' });
    component.saveRoom();
    httpMock.expectNone(r => r.url.endsWith('/room') && r.method === 'POST');
  });

  it('saveRoom should NOT call API when pricePerNight is 0', () => {
    component.rf.set({ hotelId: 1, roomNumber: 101, roomType: 'Standard', pricePerNight: 0, capacity: 2, imageUrl: '' });
    component.saveRoom();
    httpMock.expectNone(r => r.url.endsWith('/room') && r.method === 'POST');
  });

  /* ── saveRoom CREATE — POST /api/room ──────────────────────────────── */
  it('saveRoom CREATE calls POST /api/room with correct imageUrl in body', () => {
    component.rf.set({
      hotelId: 1, roomNumber: 103, roomType: 'Deluxe',
      pricePerNight: 3500, capacity: 2,
      imageUrl: 'https://img.com/r.jpg'
    });
    component.saveRoom();

    const req = httpMock.expectOne(r => r.url.endsWith('/room') && r.method === 'POST');
    expect(req.request.body.hotelId).toBe(1);
    expect(req.request.body.roomNumber).toBe(103);
    expect(req.request.body.imageUrl).toBe('https://img.com/r.jpg');
    expect(req.request.body['imagePath']).toBeUndefined(); // must NOT send imagePath

    req.flush({
      roomId: 10, hotelId: 1, roomNumber: 103, roomType: 'Deluxe',
      pricePerNight: 3500, capacity: 2, isAvailable: true,
      imageUrl: 'https://img.com/r.jpg'
    });

    expect(component.rooms().length).toBe(4);
    expect(component.editRoomId()).toBeNull();
  });

  /* ── saveRoom UPDATE — PUT /api/room/{roomId} ──────────────────────── */
  it('saveRoom UPDATE calls PUT /api/room/1 and updates list', () => {
    component.editRoom(fakeRooms[0]);
    component.rf.update(f => ({ ...f, pricePerNight: 3200 }));
    component.saveRoom();

    const req = httpMock.expectOne(r => r.url.includes('/room/1') && r.method === 'PUT');
    expect(req.request.body.pricePerNight).toBe(3200);
    req.flush({ ...fakeRooms[0], pricePerNight: 3200 });

    expect(component.rooms().find(r => r.roomId === 1)?.pricePerNight).toBe(3200);
    expect(component.editRoomId()).toBeNull();
  });

  /* ── deactivateRoom — DELETE /api/room/{roomId} ───────────────────── */
  it('deactivateRoom calls DELETE /api/room/1 and marks room inactive', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    component.deactivateRoom(fakeRooms[0]);

    const req = httpMock.expectOne(r => r.url.includes('/room/1') && r.method === 'DELETE');
    req.flush({});

    expect(component.rooms().find(r => r.roomId === 1)?.isAvailable).toBeFalse();
  });

  it('deactivateRoom should NOT call API when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    component.deactivateRoom(fakeRooms[0]);
    httpMock.expectNone(r => r.method === 'DELETE');
  });

  /* ── applyHotelFilter — GET /api/room?hotelId={id} ───────────────── */
  it('applyHotelFilter calls GET /api/room?hotelId=1 when filter set to 1', () => {
    component.filterHotelId.set(1);
    component.applyHotelFilter();

    const req = httpMock.expectOne(r =>
      r.url.endsWith('/room') && r.method === 'GET' && r.params.get('hotelId') === '1'
    );
    req.flush([fakeRooms[0], fakeRooms[1]]);
    expect(component.rooms().length).toBe(2);
  });

  it('loadRooms with filterHotelId=0 calls GET /api/room without hotelId param', () => {
    component.filterHotelId.set(0);
    component.loadRooms();

    const req = httpMock.expectOne(r =>
      r.url.endsWith('/room') && r.method === 'GET' && !r.params.has('hotelId')
    );
    req.flush(fakeRooms);
    expect(component.rooms().length).toBe(3);
  });

  /* ── Rendering ─────────────────────────────────────────────────────── */
  it('should render 3 room cards', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.room-card').length).toBe(3);
  });

  it('should render hotel filter dropdown with All Hotels option', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const options  = compiled.querySelectorAll('select option');
    const allOpt   = Array.from(options).find(o => o.textContent?.includes('All Hotels'));
    expect(allOpt).toBeTruthy();
  });
});
