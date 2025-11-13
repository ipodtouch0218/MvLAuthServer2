using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvLAuthServer2.Models.Database
{
    public class UserEntry
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string HashedToken { get; set; }
        public string? NicknameColor { get; set; }
        public long FirstPlayed { get; set; }
        public long LastSeen { get; set; }
        public int TimesConnected { get; set; }


        public List<NicknameLog> AllNicknames { get; set; }
        public List<IPLog> AllIps { get; set; }
    }
}