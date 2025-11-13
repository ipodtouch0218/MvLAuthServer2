using MvLAuthServer2.Helpers;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvLAuthServer2.Models.Database
{
    public class Ban
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public Guid? UserId { get; set; }
        [BindIpAddressAsInt]
        public uint? IpRangeMin { get; set; }
        [BindIpAddressAsInt]
        public uint? IpRangeMax { get; set; }
        public string? Message { get; set; }
        public long? Expiration { get; set; }
        public long Created { get; set; }

        public string IpRangeString()
        {
            if (IpRangeMin == null || IpRangeMax == null)
            {
                return null;
            }
            
            if (IpRangeMin == IpRangeMax)
            {
                return Utils.NumberToIp(IpRangeMin.Value);
            }
            else
            {
                return Utils.NumberToIp(IpRangeMin.Value) + " - " + Utils.NumberToIp(IpRangeMax.Value);
            }
        }
    }
}