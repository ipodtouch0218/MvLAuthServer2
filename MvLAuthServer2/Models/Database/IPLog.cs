using MvLAuthServer2.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvLAuthServer2.Models.Database
{
    public class IPLog
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey(nameof(UserEntry))]
        public int UserEntryId { get; set; }
        [BindIpAddressAsInt]
        public uint IPAddress { get; set; }
        public long LastUsed { get; set; }
        public long TimesUsed { get; set; }

        public UserEntry UserEntry { get; set; }
    }
}