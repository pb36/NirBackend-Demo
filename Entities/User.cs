using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string DisplayId { get; set; }

    public string Username { get; set; }

    public string Email { get; set; }

    public string Mobile { get; set; }

    public string AdharCard { get; set; }

    public string PanCard { get; set; }

    public string Name { get; set; }

    public string Address { get; set; }

    public int CityId { get; set; }

    public string Password { get; set; }

    public bool IsActive { get; set; }

    public int UserType { get; set; }

    public decimal Balance { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateOnly CreatedOnDate { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public DateOnly? UpdatedOnDate { get; set; }

    public string LastLoginIp { get; set; }

    public DateTime? LastLoginTime { get; set; }

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual City City { get; set; }

    public virtual ICollection<CommissionPercentage> CommissionPercentages { get; set; } = new List<CommissionPercentage>();

    public virtual User CreatedByNavigation { get; set; }

    public virtual ICollection<CreditRequest> CreditRequests { get; set; } = new List<CreditRequest>();

    public virtual ICollection<User> InverseCreatedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<Ledger> Ledgers { get; set; } = new List<Ledger>();

    public virtual Outstanding Outstanding { get; set; }

    public virtual ICollection<TopUpRequest> TopUpRequests { get; set; } = new List<TopUpRequest>();
}
