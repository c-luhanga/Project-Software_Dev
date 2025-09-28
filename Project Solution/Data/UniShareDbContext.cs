using System;
using Microsoft.EntityFrameworkCore;

namespace UniShare.Data
{
    public class UniShareDbContext : DbContext
    {
        public UniShareDbContext(DbContextOptions<UniShareDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemImage> ItemImages { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("UserID");
                entity.Property(e => e.FirebaseUid).HasColumnName("FirebaseUID");
                entity.Property(e => e.FirstName).HasColumnName("FirstName");
                entity.Property(e => e.LastName).HasColumnName("LastName");
                entity.Property(e => e.Email).HasColumnName("Email");
                entity.Property(e => e.PasswordHash).HasColumnName("PasswordHash");
                entity.Property(e => e.Phone).HasColumnName("Phone");
                entity.Property(e => e.House).HasColumnName("House");
                entity.Property(e => e.IsBanned).HasColumnName("IsBanned");
                entity.Property(e => e.IsAdmin).HasColumnName("IsAdmin");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
                entity.Property(e => e.LastSeen).HasColumnName("LastSeen");
                entity.Property(e => e.ProfileImageUrl).HasColumnName("ProfileImageURL");
            });

            // Item
            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("ItemID");
                entity.Property(e => e.Title).HasColumnName("Title");
                entity.Property(e => e.Description).HasColumnName("Description");
                entity.Property(e => e.Category).HasColumnName("Category");
                entity.Property(e => e.Price).HasColumnName("Price").HasColumnType("decimal(18,2)");
                entity.Property(e => e.Condition).HasColumnName("Condition");
                entity.Property(e => e.SellerId).HasColumnName("SellerID");
                entity.Property(e => e.PostedDate).HasColumnName("PostedDate");

                entity.HasOne(e => e.Seller)
                      .WithMany(u => u.Items)
                      .HasForeignKey(e => e.SellerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.SellerId);
                entity.HasIndex(e => e.Category);
            });

            // ItemImage
            modelBuilder.Entity<ItemImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("ImageID");
                entity.Property(e => e.ItemId).HasColumnName("ItemID");
                entity.Property(e => e.ImageUrl).HasColumnName("ImageURL");

                entity.HasOne(e => e.Item)
                      .WithMany(i => i.Images)
                      .HasForeignKey(e => e.ItemId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Conversation
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("ConversationID");
                entity.Property(e => e.LastMessage).HasColumnName("LastMessage");
                entity.Property(e => e.LastUpdated).HasColumnName("LastUpdated");
            });

            // ConversationParticipant
            modelBuilder.Entity<ConversationParticipant>(entity =>
            {
                entity.HasKey(e => new { e.ConversationId, e.UserId });
                entity.Property(e => e.ConversationId).HasColumnName("ConversationID");
                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.Participants)
                      .HasForeignKey(e => e.ConversationId);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Conversations)
                      .HasForeignKey(e => e.UserId);
            });

            // Message
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("MessageID");
                entity.Property(e => e.ConversationId).HasColumnName("ConversationID");
                entity.Property(e => e.SenderId).HasColumnName("SenderID");
                entity.Property(e => e.Content).HasColumnName("Content");
                entity.Property(e => e.Timestamp).HasColumnName("Timestamp");

                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.Messages)
                      .HasForeignKey(e => e.ConversationId);

                entity.HasOne(e => e.Sender)
                      .WithMany()
                      .HasForeignKey(e => e.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.ConversationId, e.Timestamp });
            });
        }
    }
}