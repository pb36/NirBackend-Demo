using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace NirvedBackend.Entities;

public partial class NirvedContext : DbContext
{
    public NirvedContext(DbContextOptions<NirvedContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bank> Banks { get; set; }

    public virtual DbSet<Bill> Bills { get; set; }

    public virtual DbSet<Biller> Billers { get; set; }

    public virtual DbSet<BillerCategory> BillerCategories { get; set; }

    public virtual DbSet<BillerInfo> BillerInfos { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<CommissionPercentage> CommissionPercentages { get; set; }

    public virtual DbSet<Config> Configs { get; set; }

    public virtual DbSet<CreditRequest> CreditRequests { get; set; }

    public virtual DbSet<Ledger> Ledgers { get; set; }

    public virtual DbSet<Outstanding> Outstandings { get; set; }

    public virtual DbSet<OutstandingLedger> OutstandingLedgers { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<TopUpRequest> TopUpRequests { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserNotice> UserNotices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Bank>(entity =>
        {
            entity.HasKey(e => e.BankId).HasName("PRIMARY");

            entity.ToTable("bank");

            entity.HasIndex(e => e.AccountNumber, "account_number").IsUnique();

            entity.Property(e => e.BankId)
                .HasColumnType("int(11)")
                .HasColumnName("bank_id");
            entity.Property(e => e.AccountName)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("account_name");
            entity.Property(e => e.AccountNumber)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("account_number");
            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("address");
            entity.Property(e => e.BranchName)
                .IsRequired()
                .HasMaxLength(30)
                .HasColumnName("branch_name");
            entity.Property(e => e.IfscCode)
                .IsRequired()
                .HasMaxLength(11)
                .HasColumnName("ifsc_code");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Type)
                .HasColumnType("int(1)")
                .HasColumnName("type");
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.BillId).HasName("PRIMARY");

            entity.ToTable("bills");

            entity.HasIndex(e => e.BillerId, "FK_bills_biller");

            entity.HasIndex(e => e.CreatedBy, "FK_bills_user");

            entity.HasIndex(e => e.DisplayId, "display_id").IsUnique();

            entity.Property(e => e.BillId)
                .HasColumnType("int(11)")
                .HasColumnName("bill_id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 4)
                .HasColumnName("amount");
            entity.Property(e => e.BillerId)
                .HasColumnType("int(11)")
                .HasColumnName("biller_id");
            entity.Property(e => e.CommissionGiven).HasColumnName("commission_given");
            entity.Property(e => e.CreatedBy)
                .HasColumnType("int(6)")
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.CreatedOnDate).HasColumnName("created_on_date");
            entity.Property(e => e.CustomerName)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("customer_name");
            entity.Property(e => e.DisplayId)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("display_id");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.ExtraInfo)
                .HasMaxLength(200)
                .HasColumnName("extra_info");
            entity.Property(e => e.PaymentRef)
                .HasMaxLength(50)
                .HasColumnName("payment_ref");
            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(50)
                .HasColumnName("reference_number");
            entity.Property(e => e.Remark)
                .HasMaxLength(100)
                .HasColumnName("remark");
            entity.Property(e => e.ServiceNumber)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("service_number");
            entity.Property(e => e.Status)
                .HasColumnType("int(1)")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updated_on");
            entity.Property(e => e.UpdatedOnDate).HasColumnName("updated_on_date");

            entity.HasOne(d => d.Biller).WithMany(p => p.Bills)
                .HasForeignKey(d => d.BillerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bills_biller");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Bills)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bills_user");
        });

        modelBuilder.Entity<Biller>(entity =>
        {
            entity.HasKey(e => e.BillerId).HasName("PRIMARY");

            entity.ToTable("biller");

            entity.HasIndex(e => e.BillerCategoryId, "FK_biller_biller_category");

            entity.HasIndex(e => e.Code, "code").IsUnique();

            entity.HasIndex(e => e.IsActive, "is_active");

            entity.HasIndex(e => e.Name, "name").IsUnique();

            entity.Property(e => e.BillerId)
                .HasColumnType("int(11)")
                .HasColumnName("biller_id");
            entity.Property(e => e.BillerCategoryId)
                .HasColumnType("int(11)")
                .HasColumnName("biller_category_id");
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.HasOne(d => d.BillerCategory).WithMany(p => p.Billers)
                .HasForeignKey(d => d.BillerCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_biller_biller_category");
        });

        modelBuilder.Entity<BillerCategory>(entity =>
        {
            entity.HasKey(e => e.BillerCategoryId).HasName("PRIMARY");

            entity.ToTable("biller_category");

            entity.HasIndex(e => e.IsActive, "is_active");

            entity.HasIndex(e => e.Name, "name").IsUnique();

            entity.Property(e => e.BillerCategoryId)
                .HasColumnType("int(11)")
                .HasColumnName("biller_category_id");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<BillerInfo>(entity =>
        {
            entity.HasKey(e => e.BillerInfo1).HasName("PRIMARY");

            entity.ToTable("biller_info");

            entity.HasIndex(e => e.City, "FK_biller_info_city");

            entity.HasIndex(e => e.BillerId, "biller_id").IsUnique();

            entity.Property(e => e.BillerInfo1)
                .HasColumnType("int(11)")
                .HasColumnName("biller_info");
            entity.Property(e => e.BillerId)
                .HasColumnType("int(11)")
                .HasColumnName("biller_id");
            entity.Property(e => e.City)
                .HasColumnType("int(11)")
                .HasColumnName("city");
            entity.Property(e => e.Fetching).HasColumnName("fetching");
            entity.Property(e => e.FieldsData)
                .IsRequired()
                .HasMaxLength(10000)
                .HasColumnName("fields_data");

            entity.HasOne(d => d.Biller).WithOne(p => p.BillerInfo)
                .HasForeignKey<BillerInfo>(d => d.BillerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_biller_info_biller");

            entity.HasOne(d => d.CityNavigation).WithMany(p => p.BillerInfos)
                .HasForeignKey(d => d.City)
                .HasConstraintName("FK_biller_info_city");
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.CityId).HasName("PRIMARY");

            entity.ToTable("city");

            entity.HasIndex(e => e.StateId, "FK_city_id_state");

            entity.Property(e => e.CityId)
                .HasColumnType("int(11)")
                .HasColumnName("city_id");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.StateId)
                .HasColumnType("int(11)")
                .HasColumnName("state_id");

            entity.HasOne(d => d.State).WithMany(p => p.Cities)
                .HasForeignKey(d => d.StateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_city_id_state");
        });

        modelBuilder.Entity<CommissionPercentage>(entity =>
        {
            entity.HasKey(e => e.CommissionPercentageId).HasName("PRIMARY");

            entity.ToTable("commission_percentage");

            entity.HasIndex(e => e.BillerCategoryId, "FK_commission_percentage_biller_category");

            entity.HasIndex(e => e.UserId, "FK_commission_percentage_user");

            entity.Property(e => e.CommissionPercentageId)
                .HasColumnType("int(11)")
                .HasColumnName("commission_percentage_id");
            entity.Property(e => e.BillerCategoryId)
                .HasColumnType("int(11)")
                .HasColumnName("biller_category_id");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.Percentage)
                .HasPrecision(4, 2)
                .HasColumnName("percentage");
            entity.Property(e => e.PercentageJson)
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnName("percentage_json")
                .UseCollation("utf8mb4_bin");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updated_on");
            entity.Property(e => e.UserId)
                .HasColumnType("int(6)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.BillerCategory).WithMany(p => p.CommissionPercentages)
                .HasForeignKey(d => d.BillerCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_commission_percentage_biller_category");

            entity.HasOne(d => d.User).WithMany(p => p.CommissionPercentages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_commission_percentage_user");
        });

        modelBuilder.Entity<Config>(entity =>
        {
            entity.HasKey(e => e.ConfigId).HasName("PRIMARY");

            entity.ToTable("config");

            entity.HasIndex(e => e.Key, "key").IsUnique();

            entity.Property(e => e.ConfigId)
                .HasColumnType("int(11)")
                .HasColumnName("config_id");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.CreatedOnDate).HasColumnName("created_on_date");
            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("key");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updated_on");
            entity.Property(e => e.UpdatedOnDate).HasColumnName("updated_on_date");
            entity.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("value");
        });

        modelBuilder.Entity<CreditRequest>(entity =>
        {
            entity.HasKey(e => e.CreditRequestId).HasName("PRIMARY");

            entity.ToTable("credit_request");

            entity.HasIndex(e => e.UserId, "FK_credit_request_user");

            entity.Property(e => e.CreditRequestId)
                .HasColumnType("int(11)")
                .HasColumnName("credit_request_id");
            entity.Property(e => e.Amount)
                .HasPrecision(16, 4)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.CreatedOnDate).HasColumnName("created_on_date");
            entity.Property(e => e.Remark)
                .HasMaxLength(100)
                .HasColumnName("remark");
            entity.Property(e => e.Status)
                .HasColumnType("int(1)")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updated_on");
            entity.Property(e => e.UpdatedOnDate).HasColumnName("updated_on_date");
            entity.Property(e => e.UserId)
                .HasColumnType("int(6)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.CreditRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_credit_request_user");
        });

        modelBuilder.Entity<Ledger>(entity =>
        {
            entity.HasKey(e => e.LedgerId).HasName("PRIMARY");

            entity.ToTable("ledger");

            entity.HasIndex(e => e.BillId, "FK_ledger_bills");

            entity.HasIndex(e => e.CreditRequestId, "FK_ledger_credit_request");

            entity.HasIndex(e => e.TopUpRequestId, "FK_ledger_top_up_request");

            entity.HasIndex(e => e.UserId, "FK_ledger_user_2");

            entity.Property(e => e.LedgerId)
                .HasColumnType("int(11)")
                .HasColumnName("ledger_id");
            entity.Property(e => e.Amount)
                .HasPrecision(16, 4)
                .HasColumnName("amount");
            entity.Property(e => e.BillId)
                .HasColumnType("int(11)")
                .HasColumnName("bill_id");
            entity.Property(e => e.Closing)
                .HasPrecision(16, 4)
                .HasColumnName("closing");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.CreatedOnDate).HasColumnName("created_on_date");
            entity.Property(e => e.CreditRequestId)
                .HasColumnType("int(11)")
                .HasColumnName("credit_request_id");
            entity.Property(e => e.Opening)
                .HasPrecision(16, 4)
                .HasColumnName("opening");
            entity.Property(e => e.Remark)
                .HasMaxLength(100)
                .HasColumnName("remark");
            entity.Property(e => e.TopUpRequestId)
                .HasColumnType("int(11)")
                .HasColumnName("top_up_request_id");
            entity.Property(e => e.Type)
                .HasColumnType("int(1)")
                .HasColumnName("type");
            entity.Property(e => e.UserId)
                .HasColumnType("int(6)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Bill).WithMany(p => p.Ledgers)
                .HasForeignKey(d => d.BillId)
                .HasConstraintName("FK_ledger_bills");

            entity.HasOne(d => d.CreditRequest).WithMany(p => p.Ledgers)
                .HasForeignKey(d => d.CreditRequestId)
                .HasConstraintName("FK_ledger_credit_request");

            entity.HasOne(d => d.TopUpRequest).WithMany(p => p.Ledgers)
                .HasForeignKey(d => d.TopUpRequestId)
                .HasConstraintName("FK_ledger_top_up_request");

            entity.HasOne(d => d.User).WithMany(p => p.Ledgers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ledger_user");
        });

        modelBuilder.Entity<Outstanding>(entity =>
        {
            entity.HasKey(e => e.OutstandingId).HasName("PRIMARY");

            entity.ToTable("outstanding");

            entity.HasIndex(e => e.UserId, "user_id").IsUnique();

            entity.Property(e => e.OutstandingId)
                .HasColumnType("int(11)")
                .HasColumnName("outstanding_id");
            entity.Property(e => e.Amount)
                .HasPrecision(16, 4)
                .HasColumnName("amount");
            entity.Property(e => e.UserId)
                .HasColumnType("int(6)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Outstanding)
                .HasForeignKey<Outstanding>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_outstanding_user");
        });

        modelBuilder.Entity<OutstandingLedger>(entity =>
        {
            entity.HasKey(e => e.OutstandingLedgerId).HasName("PRIMARY");

            entity.ToTable("outstanding_ledger");

            entity.HasIndex(e => e.OutstandingId, "FK__outstanding");

            entity.Property(e => e.OutstandingLedgerId)
                .HasColumnType("int(11)")
                .HasColumnName("outstanding_ledger_id");
            entity.Property(e => e.Amount)
                .HasPrecision(16, 4)
                .HasColumnName("amount");
            entity.Property(e => e.Closing)
                .HasPrecision(16, 4)
                .HasColumnName("closing");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.Opening)
                .HasPrecision(16, 4)
                .HasColumnName("opening");
            entity.Property(e => e.OutstandingId)
                .HasColumnType("int(11)")
                .HasColumnName("outstanding_id");
            entity.Property(e => e.Remark)
                .HasMaxLength(100)
                .HasColumnName("remark");
            entity.Property(e => e.Type)
                .HasColumnType("int(1)")
                .HasColumnName("type");

            entity.HasOne(d => d.Outstanding).WithMany(p => p.OutstandingLedgers)
                .HasForeignKey(d => d.OutstandingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__outstanding");
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => e.StateId).HasName("PRIMARY");

            entity.ToTable("state");

            entity.HasIndex(e => e.Name, "name").IsUnique();

            entity.Property(e => e.StateId)
                .HasColumnType("int(11)")
                .HasColumnName("state_id");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TopUpRequest>(entity =>
        {
            entity.HasKey(e => e.TopUpRequestId).HasName("PRIMARY");

            entity.ToTable("top_up_request");

            entity.HasIndex(e => e.BankId, "FK__bank");

            entity.HasIndex(e => e.UserId, "FK__user");

            entity.Property(e => e.TopUpRequestId)
                .HasColumnType("int(11)")
                .HasColumnName("top_up_request_id");
            entity.Property(e => e.Amount)
                .HasPrecision(16, 4)
                .HasColumnName("amount");
            entity.Property(e => e.BankId)
                .HasColumnType("int(11)")
                .HasColumnName("bank_id");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.CreatedOnDate).HasColumnName("created_on_date");
            entity.Property(e => e.DepositDate).HasColumnName("deposit_date");
            entity.Property(e => e.ImageId)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("image_id");
            entity.Property(e => e.PaymentMode)
                .HasColumnType("int(1)")
                .HasColumnName("payment_mode");
            entity.Property(e => e.ReferenceNumber)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("reference_number");
            entity.Property(e => e.Remark)
                .HasMaxLength(100)
                .HasColumnName("remark");
            entity.Property(e => e.Status)
                .HasColumnType("int(1)")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updated_on");
            entity.Property(e => e.UpdatedOnDate).HasColumnName("updated_on_date");
            entity.Property(e => e.UserId)
                .HasColumnType("int(6)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Bank).WithMany(p => p.TopUpRequests)
                .HasForeignKey(d => d.BankId)
                .HasConstraintName("FK__bank");

            entity.HasOne(d => d.User).WithMany(p => p.TopUpRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("user");

            entity.HasIndex(e => e.CityId, "FK_user_city");

            entity.HasIndex(e => e.CreatedBy, "FK_user_user");

            entity.HasIndex(e => e.AdharCard, "adhar_card").IsUnique();

            entity.HasIndex(e => e.DisplayId, "display_id").IsUnique();

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.Mobile, "mobile").IsUnique();

            entity.HasIndex(e => e.PanCard, "pan_card").IsUnique();

            entity.HasIndex(e => e.Username, "username").IsUnique();

            entity.Property(e => e.UserId)
                .HasColumnType("int(6)")
                .HasColumnName("user_id");
            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.AdharCard)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("adhar_card");
            entity.Property(e => e.Balance)
                .HasPrecision(16, 4)
                .HasColumnName("balance");
            entity.Property(e => e.CityId)
                .HasColumnType("int(11)")
                .HasColumnName("city_id");
            entity.Property(e => e.CreatedBy)
                .HasColumnType("int(6)")
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.CreatedOnDate).HasColumnName("created_on_date");
            entity.Property(e => e.DisplayId)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("display_id");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasColumnName("email");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.LastLoginIp)
                .HasMaxLength(40)
                .HasColumnName("last_login_ip");
            entity.Property(e => e.LastLoginTime)
                .HasColumnType("datetime")
                .HasColumnName("last_login_time");
            entity.Property(e => e.Mobile)
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnName("mobile");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PanCard)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("pan_card");
            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("password");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updated_on");
            entity.Property(e => e.UpdatedOnDate).HasColumnName("updated_on_date");
            entity.Property(e => e.UserType)
                .HasColumnType("int(1)")
                .HasColumnName("user_type");
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasOne(d => d.City).WithMany(p => p.Users)
                .HasForeignKey(d => d.CityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_city");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InverseCreatedByNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_user_user");
        });

        modelBuilder.Entity<UserNotice>(entity =>
        {
            entity.HasKey(e => e.UserNoticeId).HasName("PRIMARY");

            entity.ToTable("user_notice");

            entity.HasIndex(e => e.EndDate, "end_time");

            entity.HasIndex(e => e.StartDate, "start_time");

            entity.Property(e => e.UserNoticeId)
                .HasColumnType("int(11)")
                .HasColumnName("user_notice_id");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnName("message");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updated_on");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
