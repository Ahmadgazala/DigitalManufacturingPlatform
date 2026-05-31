using DMP.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DMP.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Manufacturer>         Manufacturers         => Set<Manufacturer>();
    public DbSet<Machine>              Machines              => Set<Machine>();
    public DbSet<WorkshopPhoto>        WorkshopPhotos        => Set<WorkshopPhoto>();
    public DbSet<Review>               Reviews               => Set<Review>();
    public DbSet<Supplier>             Suppliers             => Set<Supplier>();
    public DbSet<ManufacturingRequest> ManufacturingRequests => Set<ManufacturingRequest>();
    public DbSet<RequestFile>          RequestFiles          => Set<RequestFile>();
    public DbSet<Quotation>            Quotations            => Set<Quotation>();
    public DbSet<GroupBuyingCampaign>  GroupBuyingCampaigns  => Set<GroupBuyingCampaign>();
    public DbSet<CampaignParticipant>  CampaignParticipants  => Set<CampaignParticipant>();
    public DbSet<Message>              Messages              => Set<Message>();
    public DbSet<Notification>         Notifications         => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Manufacturer>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Machine>()
            .HasOne(m => m.Manufacturer)
            .WithMany(m => m.Machines)
            .HasForeignKey(m => m.ManufacturerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkshopPhoto>()
            .HasOne(p => p.Manufacturer)
            .WithMany(m => m.WorkshopPhotos)
            .HasForeignKey(p => p.ManufacturerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
            .HasOne(r => r.Manufacturer)
            .WithMany(m => m.Reviews)
            .HasForeignKey(r => r.ManufacturerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
            .HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ManufacturingRequest>()
            .HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ManufacturingRequest>()
            .HasOne(r => r.Manufacturer)
            .WithMany()
            .HasForeignKey(r => r.ManufacturerId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.Entity<Quotation>()
            .HasOne(q => q.Request)
            .WithMany(r => r.Quotations)
            .HasForeignKey(q => q.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Quotation>()
            .HasOne(q => q.Manufacturer)
            .WithMany()
            .HasForeignKey(q => q.ManufacturerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<CampaignParticipant>()
            .HasOne(p => p.Campaign)
            .WithMany(c => c.Participants)
            .HasForeignKey(p => p.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CampaignParticipant>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
