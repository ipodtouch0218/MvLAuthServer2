using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MvLAuthServer2.Models.Database;

namespace MvLAuthServer2
{
    public class MvLDBContext(DbContextOptions<MvLDBContext> options) : DbContext(options)
    {
        public DbSet<Ban> Ban { get; set; }
        public DbSet<UserEntry> UserEntry { get; set; }
        public DbSet<NewsBoardPost> NewsBoardPost { get; set; }
        public DbSet<NicknameLog> NicknameLog { get; set; }
        public DbSet<IPLog> IPLog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEntry>()
                .HasMany(userEntry => userEntry.AllNicknames)
                .WithOne(nicknameLog => nicknameLog.UserEntry)
                .HasForeignKey(nicknameLog => nicknameLog.UserEntryId);

            modelBuilder.Entity<UserEntry>()
                .HasMany(userEntry => userEntry.AllIps)
                .WithOne(ipLog => ipLog.UserEntry)
                .HasForeignKey(ipLog => ipLog.UserEntryId);

            modelBuilder.Entity<UserEntry>()
                .Property(userEntry => userEntry.UserId)
                .HasConversion(new GuidToStringConverter());

            modelBuilder.Entity<Ban>()
                .Property(ban => ban.UserId)
                .HasConversion(new GuidToStringConverter());
        }
    }
}
