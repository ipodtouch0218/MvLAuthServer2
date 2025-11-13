using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvLAuthServer2.Models.Database
{
    public class NicknameLog
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [ForeignKey(nameof(UserEntry))]
        public int UserEntryId { get; set; }
        public string Nickname { get; set; }
        public int TimesUsed { get; set; }
        public long LastUsed { get; set; }

        public UserEntry UserEntry { get; set; }
    }
}