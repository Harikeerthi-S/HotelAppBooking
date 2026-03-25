using Microsoft.EntityFrameworkCore;
using HotelBookingApp.Models;

namespace HotelBookingApp.Context
{
    public class HotelBookingContext : DbContext
    {
        public HotelBookingContext(DbContextOptions<HotelBookingContext> options)
            : base(options) { }

        // ── DbSets ────────────────────────────────────────────────────────
        public DbSet<User>          Users          { get; set; }
        public DbSet<Hotel>         Hotels         { get; set; }
        public DbSet<Room>          Rooms          { get; set; }
        public DbSet<Amenity>       Amenities      { get; set; }
        public DbSet<HotelAmenity>  HotelAmenities { get; set; }
        public DbSet<Booking>       Bookings       { get; set; }
        public DbSet<BookingRoom>   BookingRooms   { get; set; }
        public DbSet<Payment>       Payments       { get; set; }
        public DbSet<Cancellation>  Cancellations  { get; set; }
        public DbSet<Review>        Reviews        { get; set; }
        public DbSet<Wishlist>      Wishlists      { get; set; }
        public DbSet<Notification>  Notifications  { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── USER ─────────────────────────────────────────────────────
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();

                e.HasMany(u => u.Bookings)
                 .WithOne(b => b.User)
                 .HasForeignKey(b => b.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasMany(u => u.Reviews)
                 .WithOne(r => r.User)
                 .HasForeignKey(r => r.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(u => u.Notifications)
                 .WithOne(n => n.User)
                 .HasForeignKey(n => n.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(u => u.Wishlists)
                 .WithOne(w => w.User)
                 .HasForeignKey(w => w.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── HOTEL ────────────────────────────────────────────────────
            modelBuilder.Entity<Hotel>(e =>
            {
                e.HasMany(h => h.Rooms)
                 .WithOne(r => r.Hotel)
                 .HasForeignKey(r => r.HotelId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(h => h.Bookings)
                 .WithOne(b => b.Hotel)
                 .HasForeignKey(b => b.HotelId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasMany(h => h.Reviews)
                 .WithOne(r => r.Hotel)
                 .HasForeignKey(r => r.HotelId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(h => h.HotelAmenities)
                 .WithOne(ha => ha.Hotel)
                 .HasForeignKey(ha => ha.HotelId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(h => h.Wishlists)
                 .WithOne(w => w.Hotel)
                 .HasForeignKey(w => w.HotelId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── ROOM ─────────────────────────────────────────────────────
            modelBuilder.Entity<Room>(e =>
            {
                e.Property(r => r.PricePerNight).HasPrecision(18, 2);
                e.Property(r => r.ImageUrl)
                      .HasMaxLength(255)
                      .IsRequired(false);

                e.HasMany(r => r.BookingRooms)
                 .WithOne(br => br.Room)
                 .HasForeignKey(br => br.RoomId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── BOOKING ──────────────────────────────────────────────────
            modelBuilder.Entity<Booking>(e =>
            {
                e.Property(b => b.TotalAmount).HasPrecision(18, 2);

                e.HasMany(b => b.BookingRooms)
                 .WithOne(br => br.Booking)
                 .HasForeignKey(br => br.BookingId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(b => b.Payments)
                 .WithOne(p => p.Booking)
                 .HasForeignKey(p => p.BookingId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(b => b.Cancellations)
                 .WithOne(c => c.Booking)
                 .HasForeignKey(c => c.BookingId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── BOOKING ROOM ─────────────────────────────────────────────
            modelBuilder.Entity<BookingRoom>(e =>
            {
                e.Property(br => br.PricePerNight).HasPrecision(18, 2);
            });

            // ── PAYMENT ──────────────────────────────────────────────────
            modelBuilder.Entity<Payment>(e =>
            {
                e.Property(p => p.Amount).HasPrecision(18, 2);
            });

            // ── CANCELLATION ─────────────────────────────────────────────
            modelBuilder.Entity<Cancellation>(e =>
            {
                e.Property(c => c.RefundAmount).HasPrecision(18, 2);
            });

            // ── HOTEL AMENITY ────────────────────────────────────────────
            modelBuilder.Entity<HotelAmenity>(e =>
            {
                e.HasOne(ha => ha.Amenity)
                 .WithMany(a => a.HotelAmenities)
                 .HasForeignKey(ha => ha.AmenityId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
