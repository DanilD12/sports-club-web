using Microsoft.EntityFrameworkCore;
using Stalika.Web.Entities;

namespace Stalika.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<BookingInfoRow> BookingInfoRows => Set<BookingInfoRow>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<Gym> Gyms => Set<Gym>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentRental> EquipmentRentals => Set<EquipmentRental>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Gym>(entity =>
        {
            entity.ToTable("gyms");
            entity.HasKey(e => e.GymNumber);

            entity.Property(e => e.GymNumber).HasColumnName("gym_number");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.TableCount).HasColumnName("table_count");
            entity.Property(e => e.OpeningTime).HasColumnName("opening_time");
            entity.Property(e => e.ClosingTime).HasColumnName("closing_time");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleName).HasColumnName("role_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId);
        });

        modelBuilder.Entity<Trainer>(entity =>
        {
            entity.ToTable("trainers");
            entity.HasKey(e => e.TrainerId);

            entity.Property(e => e.TrainerId).HasColumnName("trainer_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.HourlyRate).HasColumnName("hourly_rate");
            entity.Property(e => e.Qualification).HasColumnName("qualification");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<Table>(entity =>
        {
            entity.ToTable("tables");
            entity.HasKey(e => e.TableNumber);

            entity.Property(e => e.TableNumber).HasColumnName("table_number");
            entity.Property(e => e.GymNumber).HasColumnName("gym_number");
            entity.Property(e => e.PricePerHour).HasColumnName("price_per_hour");

            entity.HasOne(e => e.Gym)
                .WithMany(g => g.Tables)
                .HasForeignKey(e => e.GymNumber);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");
            entity.HasKey(e => e.BookingNumber);

            entity.Property(e => e.BookingNumber)
                .HasColumnName("booking_number")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TableNumber).HasColumnName("table_number");

            entity.Property(e => e.StartTime)
                .HasColumnName("start_time")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.TotalPrice).HasColumnName("total_price");

            entity.Property(e => e.BookingDate)
                .HasColumnName("booking_date")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.CoachId).HasColumnName("coach_id");

            entity.Property(e => e.EndTime)
                .HasColumnName("end_time")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(e => e.UserId);

            entity.HasOne(e => e.Table)
                .WithMany(t => t.Bookings)
                .HasForeignKey(e => e.TableNumber);

            entity.HasOne(e => e.Trainer)
                .WithMany(t => t.Bookings)
                .HasForeignKey(e => e.CoachId)
                .HasPrincipalKey(t => t.TrainerId);
        });

        modelBuilder.Entity<BookingInfoRow>(entity =>
        {
            entity.ToView("booking_info");
            entity.HasNoKey();

            entity.Property(e => e.BookingNumber).HasColumnName("booking_number");

            entity.Property(e => e.BookingDate)
                .HasColumnName("booking_date")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.StartTime)
                .HasColumnName("start_time")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.EndTime)
                .HasColumnName("end_time")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.TableNumber).HasColumnName("table_number");
            entity.Property(e => e.ClientName).HasColumnName("client_name");
            entity.Property(e => e.TrainerName).HasColumnName("trainer_name");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price");
            entity.Property(e => e.EquipmentName).HasColumnName("equipment_name");
            entity.Property(e => e.EquipmentQuantity).HasColumnName("equipment_quantity");
            entity.Property(e => e.EquipmentAmount).HasColumnName("equipment_amount");
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.ToTable("equipment");
            entity.HasKey(e => e.EquipmentName);

            entity.Property(e => e.EquipmentName).HasColumnName("equipment_name");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.PricePerHour).HasColumnName("price_per_hour");
        });

        modelBuilder.Entity<EquipmentRental>(entity =>
        {
            entity.ToTable("equipment_rental");
            entity.HasKey(e => e.RentalNumber);

            entity.Property(e => e.RentalNumber)
                .HasColumnName("rental_number")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EquipmentName).HasColumnName("equipment_name");
            entity.Property(e => e.BookingNumber).HasColumnName("booking_number");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Amount).HasColumnName("amount");

            entity.HasOne(e => e.Booking)
                .WithMany(b => b.EquipmentRentals)
                .HasForeignKey(e => e.BookingNumber);

            entity.HasOne(e => e.Equipment)
                .WithMany(eq => eq.Rentals)
                .HasForeignKey(e => e.EquipmentName);
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.ToTable("tournaments");
            entity.HasKey(e => e.TournamentId);

            entity.Property(e => e.TournamentId)
                .HasColumnName("tournament_id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.TournamentName).HasColumnName("tournament_name");
            entity.Property(e => e.Organizer).HasColumnName("organizer");
            entity.Property(e => e.ParticipantCount).HasColumnName("participant_count");

            entity.Property(e => e.Date)
                .HasColumnName("date")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.MaxParticipants).HasColumnName("max_participants");
        });

        modelBuilder.Entity<TournamentParticipant>(entity =>
        {
            entity.ToTable("tournament_participants");

            entity.HasKey(e => new { e.UserId, e.TournamentId });

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Place).HasColumnName("place");
            entity.Property(e => e.TournamentId).HasColumnName("tournament_id");

            entity.HasOne(e => e.User)
                .WithMany(u => u.TournamentParticipants)
                .HasForeignKey(e => e.UserId);

            entity.HasOne(e => e.Tournament)
                .WithMany(t => t.Participants)
                .HasForeignKey(e => e.TournamentId);
        });
    }
}