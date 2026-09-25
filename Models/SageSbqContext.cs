using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace StowellCoAPI.Models;

public partial class SageSbqContext : DbContext
{
    public SageSbqContext()
    {
    }

    public SageSbqContext(DbContextOptions<SageSbqContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountingQueue> AccountingQueues { get; set; }

    public virtual DbSet<AppLog> AppLogs { get; set; }

    public virtual DbSet<Bid> Bids { get; set; }

    public virtual DbSet<BidAmount> BidAmounts { get; set; }

    public virtual DbSet<BidEmployee> BidEmployees { get; set; }

    public virtual DbSet<BidPhase> BidPhases { get; set; }

    public virtual DbSet<BudgetPayment> BudgetPayments { get; set; }

    public virtual DbSet<BudgetPaymentLog> BudgetPaymentLogs { get; set; }

    public virtual DbSet<BudgetTransaction> BudgetTransactions { get; set; }

    public virtual DbSet<BudgetTransactionDraft> BudgetTransactionDrafts { get; set; }

    public virtual DbSet<BudgetTransactionLog> BudgetTransactionLogs { get; set; }

    public virtual DbSet<ContractorInfo> ContractorInfos { get; set; }

    public virtual DbSet<CostCodeList> CostCodeLists { get; set; }

    public virtual DbSet<DateSequence> DateSequences { get; set; }

    public virtual DbSet<Division> Divisions { get; set; }

    public virtual DbSet<ErrorLog> ErrorLogs { get; set; }

    public virtual DbSet<GetNextFileNumber> GetNextFileNumbers { get; set; }

    public virtual DbSet<GetNextJobId> GetNextJobIds { get; set; }

    public virtual DbSet<GetProjectDetail> GetProjectDetails { get; set; }

    public virtual DbSet<GetUserDetail> GetUserDetails { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<McostCode> McostCodes { get; set; }

    public virtual DbSet<McostCodes1> McostCodes1s { get; set; }

    public virtual DbSet<PreferredCcode> PreferredCcodes { get; set; }

    public virtual DbSet<ProcedureLog> ProcedureLogs { get; set; }

    public virtual DbSet<ProjectInfo> ProjectInfos { get; set; }

    public virtual DbSet<ProjectLocation> ProjectLocations { get; set; }

    public virtual DbSet<ProjectManagement> ProjectManagements { get; set; }

    public virtual DbSet<ProjectManagementLog> ProjectManagementLogs { get; set; }

    public virtual DbSet<ProjectManagementOld> ProjectManagementOlds { get; set; }

    public virtual DbSet<ProjectManagementUser> ProjectManagementUsers { get; set; }

    public virtual DbSet<ProjectResource> ProjectResources { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SageBudgetTransaction> SageBudgetTransactions { get; set; }

    public virtual DbSet<SageBudgetTransactionDraft> SageBudgetTransactionDrafts { get; set; }

    public virtual DbSet<SageJob> SageJobs { get; set; }

    public virtual DbSet<SecurityGroupDivisonMapping> SecurityGroupDivisonMappings { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<StowellUser> StowellUsers { get; set; }

    public virtual DbSet<StowellUsers1> StowellUsers1s { get; set; }

    public virtual DbSet<Subcnt> Subcnts { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<VwAccountingQueue> VwAccountingQueues { get; set; }

    public virtual DbSet<VwBudgetPayment> VwBudgetPayments { get; set; }

    public virtual DbSet<VwCashFlowStatus> VwCashFlowStatuses { get; set; }

    public virtual DbSet<VwClient> VwClients { get; set; }

    public virtual DbSet<VwClosedBid> VwClosedBids { get; set; }

    public virtual DbSet<VwGetBidderName> VwGetBidderNames { get; set; }

    public virtual DbSet<VwInvoiceretention> VwInvoiceretentions { get; set; }

    public virtual DbSet<VwInvoiceretentionByEmail> VwInvoiceretentionByEmails { get; set; }

    public virtual DbSet<VwJobStatus> VwJobStatuses { get; set; }

    public virtual DbSet<VwLocalAccountingQueue> VwLocalAccountingQueues { get; set; }

    public virtual DbSet<VwMonth> VwMonths { get; set; }

    public virtual DbSet<VwOpenBid> VwOpenBids { get; set; }

    public virtual DbSet<VwPayment> VwPayments { get; set; }

    public virtual DbSet<VwPendingBid> VwPendingBids { get; set; }

    public virtual DbSet<VwProcessedBid> VwProcessedBids { get; set; }

    public virtual DbSet<VwTotalBudget> VwTotalBudgets { get; set; }

    public virtual DbSet<Vwbdglin> Vwbdglins { get; set; }

    public virtual DbSet<Vwcostcode> Vwcostcodes { get; set; }

    public virtual DbSet<VwjobNumber> VwjobNumbers { get; set; }

    public virtual DbSet<VwjobType> VwjobTypes { get; set; }

    public virtual DbSet<Vwjobcstdescrpt> Vwjobcstdescrpts { get; set; }

    public virtual DbSet<Vwphase> Vwphases { get; set; }

    public virtual DbSet<VwphasesList> VwphasesLists { get; set; }

    public virtual DbSet<Vwsession> Vwsessions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-C86LJD9\\SQLEXPRESS02;Database=SageSBQ;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<AccountingQueue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_AccountingQueue_ID");

            entity.ToTable("AccountingQueue");

            entity.HasIndex(e => e.JobId, "KEY_AccountingQueue_JobID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.Submitter)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AppLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AppLog__5E54864857FEA60F");

            entity.ToTable("AppLog");

            entity.Property(e => e.LogLevel)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LogMessage)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.LogTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Module)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Bids_ID");

            entity.HasIndex(e => e.JobId, "KEY_Bids_JobID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BidDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.BidStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Bidder)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ContractAmount).HasColumnType("decimal(19, 2)");
            entity.Property(e => e.ContractNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Department)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Division)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsAccountingQueue)
                .HasDefaultValue(false)
                .HasColumnName("isAccountingQueue");
            entity.Property(e => e.IsNew).HasColumnName("isNew");
            entity.Property(e => e.IsPushtoSage)
                .HasDefaultValue(false)
                .HasColumnName("isPushtoSage");
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.JobName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.JobType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastModifiedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Phase)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShortName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Submitter)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TotalBudget).HasColumnType("decimal(19, 2)");
            entity.Property(e => e.TotalPaid).HasColumnType("decimal(19, 2)");
        });

        modelBuilder.Entity<BidAmount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BidAmount_ID");

            entity.ToTable("BidAmount");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BidAmount1)
                .HasColumnType("money")
                .HasColumnName("BidAmount");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
        });

        modelBuilder.Entity<BidEmployee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BidEmployee_ID");

            entity.ToTable("BidEmployee");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Employee)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BidPhase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BidPhase_ID");

            entity.ToTable("BidPhase");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.Phase)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BudgetPayment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BudgetPayment_ID");

            entity.ToTable("BudgetPayment");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(19, 2)");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.JobId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BudgetPaymentLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK_BudgetPaymentLog_LogID");

            entity.ToTable("BudgetPaymentLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(19, 2)");
            entity.Property(e => e.BudgetPaymentId).HasColumnName("BudgetPaymentID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.JobId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.LogDateTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LogUser)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Operation)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BudgetTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BudgetTracking");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ApprovedBudget)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(19, 2)");
            entity.Property(e => e.Budget).HasColumnType("decimal(19, 2)");
            entity.Property(e => e.BudgetApprovedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.BudgetSubmittedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CostCodeDescription)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsNewBudget).HasDefaultValue(false);
            entity.Property(e => e.IsRejected).HasDefaultValue(false);
            entity.Property(e => e.Isapproved)
                .HasDefaultValue(false)
                .HasColumnName("ISApproved");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.PhaseName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PhaseNumber).HasDefaultValue((short)0);
            entity.Property(e => e.RejectedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SubmittedDate).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<BudgetTransactionDraft>(entity =>
        {
            entity.HasKey(e => e.DraftId).HasName("PK__BudgetTr__3E93D63B20380045");

            entity.Property(e => e.DraftId).HasColumnName("DraftID");
            entity.Property(e => e.Budget).HasColumnType("decimal(19, 2)");
            entity.Property(e => e.BudgetSubmittedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CostCodeDescription)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsSave).HasDefaultValue(true);
            entity.Property(e => e.Issubmitted)
                .HasDefaultValue(false)
                .HasColumnName("ISSubmitted");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.PhaseName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SavedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SubmittedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BudgetTransactionLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__BudgetTr__5E5499A8920C0446");

            entity.ToTable("BudgetTransactionLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ApprovedBudget).HasColumnType("decimal(19, 2)");
            entity.Property(e => e.Budget).HasColumnType("decimal(19, 2)");
            entity.Property(e => e.BudgetApprovedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.BudgetSubmittedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CostCodeDescription)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsRejected).HasDefaultValue(false);
            entity.Property(e => e.Isapproved).HasColumnName("ISApproved");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.LogDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LogUser)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.PhaseName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RejectedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");

            entity.HasOne(d => d.Transaction).WithMany(p => p.BudgetTransactionLogs)
                .HasForeignKey(d => d.TransactionId)
                .HasConstraintName("FK_BudgetTransactionLog_BudgetTransactions");
        });

        modelBuilder.Entity<ContractorInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ContractorInfo_ID");

            entity.ToTable("ContractorInfo");

            entity.HasIndex(e => e.JobId, "KEY_ContractorInfo_JobID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Address)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Address2)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Client)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.JobId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.State)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Zip)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("zip");
        });

        modelBuilder.Entity<CostCodeList>(entity =>
        {
            entity.ToTable("CostCodeList");

            entity.HasIndex(e => e.ShortDescription, "IX_CostCodeList_ShortDescription").HasFilter("([ShortDescription] IS NOT NULL)");

            entity.HasIndex(e => e.CostCodeDescription, "UK_CostCodeList_CostCodeDescription").IsUnique();

            entity.HasIndex(e => e.PreferredCode, "UK_CostCodeList_PreferredCode").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CostCodeDescription)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.CreatedDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ModifiedBy).HasMaxLength(50);
            entity.Property(e => e.ModifiedDate).HasPrecision(3);
            entity.Property(e => e.ShortDescription)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DateSequence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_DataSequence_ID");

            entity.ToTable("DateSequence");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Division>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.Division1)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Division");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.Recnum).HasColumnName("recnum");
        });

        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(e => e.ErrorLogId).HasName("PK__ErrorLog__D65247E236EEDE96");

            entity.ToTable("ErrorLog");

            entity.Property(e => e.ErrorLogId).HasColumnName("ErrorLogID");
            entity.Property(e => e.DateOccurred)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.Property(e => e.ErrorProcedure).HasMaxLength(128);
        });

        modelBuilder.Entity<GetNextFileNumber>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("GetNextFileNumber");

            entity.Property(e => e.FileNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<GetNextJobId>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("GetNextJobID");

            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
        });

        modelBuilder.Entity<GetProjectDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("GetProjectDetails");

            entity.Property(e => e.Apmemail)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("APMEmail");
            entity.Property(e => e.Division)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Jobnme)
                .HasMaxLength(75)
                .HasColumnName("jobnme");
            entity.Property(e => e.Pmemail)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PMEmail");
            entity.Property(e => e.Recnum).HasColumnName("recnum");
            entity.Property(e => e.Status)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<GetUserDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("GetUserDetails");

            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RoleName).HasMaxLength(50);
            entity.Property(e => e.Roleid).HasColumnName("roleid");
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Jobs_ID");

            entity.HasIndex(e => e.JobId, "KEY_Jobs_JobID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Address2)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ClientContract)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ClientId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ClientID");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ContactEmail)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContractorPm)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ContractorPM");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedByEmail)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Division)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EstimatedId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EstimatedID");
            entity.Property(e => e.FolderLocation)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.GeneralContractor)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.JobAddress)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.JobAddress2)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.JobCity)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.JobEmail)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.JobId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.JobName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.JobPhone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.JobSite)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.JobState)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.JobType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedByEmail)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NtofilingStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("NTOFilingStatus");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Retainagepercent)
                .HasDefaultValue(0.0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.State)
                .HasMaxLength(2)
                .IsUnicode(false);
        });

        modelBuilder.Entity<McostCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_MainPhase_ID");

            entity.ToTable("MCostCodes");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Column10)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Column11)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Column5)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Column6)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Column7)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Column8)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Column9)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CostCode).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Equipment)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GcOther)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GC (Other)");
            entity.Property(e => e.Labor)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PhaseA)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PhaseB)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PhaseC)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Subcontract)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<McostCodes1>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_MCostCodes");

            entity.ToTable("MCostCodes1");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CostCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PreferredCcode>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PreferredCCode");

            entity.Property(e => e.CostCode).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.Description)
                .HasMaxLength(8000)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ProcedureLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Procedur__5E5499A8F05A29BF");

            entity.ToTable("ProcedureLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.ClientIp)
                .HasMaxLength(50)
                .HasColumnName("ClientIP");
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.Property(e => e.ExecutionTime).HasColumnType("datetime");
            entity.Property(e => e.Parameters).HasMaxLength(2000);
            entity.Property(e => e.ProcedureName).HasMaxLength(128);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UserName).HasMaxLength(128);
        });

        modelBuilder.Entity<ProjectInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProjectInfo_ID");

            entity.ToTable("ProjectInfo");

            entity.HasIndex(e => e.JobId, "KEY_ProjectInfo_JobID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Bonded)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CertifiedPayroll)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsuranceType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceSubmittal)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.JobId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.RetainagePerc).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TaxExcempt)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ProjectLocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProjectLocation_ID");

            entity.ToTable("ProjectLocation");

            entity.HasIndex(e => e.JobId, "KEY_ProjectLocation_JobID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Address)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Address2)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.JobId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.Jobsite)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.SalesTaxDistrict)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.State)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ProjectManagement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProjectManagement_ID2");

            entity.ToTable("ProjectManagement");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Apmemail)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("APMEmail");
            entity.Property(e => e.Branch)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Division)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Pmemail)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PMEmail");
        });

        modelBuilder.Entity<ProjectManagementLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProjectManagementLog_ID");

            entity.ToTable("ProjectManagementLog");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Apmemail)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("APMEmail");
            entity.Property(e => e.Command)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ModifiedUser)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Pmemail)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PMEmail");
        });

        modelBuilder.Entity<ProjectManagementOld>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProjectManagement_ID");

            entity.ToTable("ProjectManagementOld");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ProjectManagementUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProjectM__3214EC27763A9BF0");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DateAdded)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmailId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EmailID");
            entity.Property(e => e.IsDisabled)
                .HasDefaultValue(false)
                .HasColumnName("isDisabled");
        });

        modelBuilder.Entity<ProjectResource>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProjectResources_ID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Branch)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.JobId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A64F52119");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.PermissionType).HasMaxLength(50);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<SageBudgetTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BudgetTracking1");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Budget).HasColumnType("decimal(19, 2)");
            entity.Property(e => e.CostCodeDescription)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.PhaseName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PhaseNumber).HasDefaultValue((short)0);
            entity.Property(e => e.Saveddate).HasColumnName("saveddate");
            entity.Property(e => e.Submittedby)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("submittedby");
        });

        modelBuilder.Entity<SageBudgetTransactionDraft>(entity =>
        {
            entity.HasKey(e => e.DraftId).HasName("PK__SageBudg__3E93D63B56509B6D");

            entity.Property(e => e.DraftId).HasColumnName("DraftID");
            entity.Property(e => e.Budget).HasColumnType("decimal(19, 2)");
            entity.Property(e => e.BudgetSubmittedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CostCodeDescription)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsSave).HasDefaultValue(true);
            entity.Property(e => e.Issubmitted)
                .HasDefaultValue(false)
                .HasColumnName("ISSubmitted");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.PhaseName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SavedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SubmittedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SageJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SageJobs_ID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.JobNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");
        });

        modelBuilder.Entity<SecurityGroupDivisonMapping>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("SecurityGroupDivisonMapping");

            entity.Property(e => e.Division)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.SecurityGroup)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Settings_ID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FileNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<StowellUser>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Licenses)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<StowellUsers1>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("StowellUsers1");

            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Licenses)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Subcnt>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("subcnt");

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.Billed)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("billed");
            entity.Property(e => e.Change)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("change");
            entity.Property(e => e.Cntrct)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("cntrct");
            entity.Property(e => e.Cstcde)
                .HasColumnType("decimal(15, 3)")
                .HasColumnName("cstcde");
            entity.Property(e => e.Csttyp).HasColumnName("csttyp");
            entity.Property(e => e.Dscrpt)
                .HasMaxLength(50)
                .HasColumnName("dscrpt");
            entity.Property(e => e.Gstsbj)
                .HasMaxLength(1)
                .HasColumnName("gstsbj");
            entity.Property(e => e.Hstsbj)
                .HasMaxLength(1)
                .HasColumnName("hstsbj");
            entity.Property(e => e.Idnum).HasColumnName("_idnum");
            entity.Property(e => e.Linref)
                .HasMaxLength(32)
                .HasColumnName("linref");
            entity.Property(e => e.Ntetxt).HasColumnName("ntetxt");
            entity.Property(e => e.Pstsbj)
                .HasMaxLength(1)
                .HasColumnName("pstsbj");
            entity.Property(e => e.Remain)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("remain");
            entity.Property(e => e.Usrdf1)
                .HasMaxLength(50)
                .HasColumnName("usrdf1");
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("SystemLog");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.CostCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.LogDate).HasColumnType("datetime");
            entity.Property(e => e.ProcedureName).HasMaxLength(128);
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwAccountingQueue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwAccountingQueue");

            entity.Property(e => e.Address)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BidStatus)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.JobName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.JobType)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.Submitter)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("submitter");
        });

        modelBuilder.Entity<VwBudgetPayment>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwBudgetPayments");

            entity.Property(e => e.AmountPaid).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.Balance).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.Recnum).HasColumnName("recnum");
            entity.Property(e => e.Totalbudget)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("totalbudget");
        });

        modelBuilder.Entity<VwCashFlowStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwCashFlowStatus");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Status)
                .HasMaxLength(8)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwClient>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwClients");

            entity.Property(e => e.Address1)
                .HasMaxLength(50)
                .HasColumnName("address1");
            entity.Property(e => e.Address2)
                .HasMaxLength(50)
                .HasColumnName("address2");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasColumnName("city");
            entity.Property(e => e.Clientname)
                .HasMaxLength(102)
                .HasColumnName("clientname");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Shtnme)
                .HasMaxLength(30)
                .HasColumnName("shtnme");
            entity.Property(e => e.State)
                .HasMaxLength(2)
                .HasColumnName("state");
            entity.Property(e => e.Zip)
                .HasMaxLength(10)
                .HasColumnName("zip");
        });

        modelBuilder.Entity<VwClosedBid>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwClosedBids");

            entity.Property(e => e.Address)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BidStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.Submitter)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("submitter");
        });

        modelBuilder.Entity<VwGetBidderName>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwGetBidderName");

            entity.Property(e => e.Bidder)
                .HasMaxLength(101)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwInvoiceretention>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VW_Invoiceretention");

            entity.Property(e => e.FileNumber).HasColumnName("file_number");
            entity.Property(e => e.TotalRetain)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_retain($)");
        });

        modelBuilder.Entity<VwInvoiceretentionByEmail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VW_Invoiceretention_ByEmail");

            entity.Property(e => e.EmailId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EmailID");
            entity.Property(e => e.FileNumber).HasColumnName("file_number");
            entity.Property(e => e.TotalRetain)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_retain($)");
        });

        modelBuilder.Entity<VwJobStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwJobStatus");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Status)
                .HasMaxLength(8)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwLocalAccountingQueue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwLocalAccountingQueue");

            entity.Property(e => e.Address)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BidStatus)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.JobName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.JobType)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.Submitter)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("submitter");
        });

        modelBuilder.Entity<VwMonth>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwMonth");

            entity.Property(e => e.MonthYear).HasMaxLength(4000);
        });

        modelBuilder.Entity<VwOpenBid>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwOpenBids");

            entity.Property(e => e.Address)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BidStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.Submitter)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("submitter");
        });

        modelBuilder.Entity<VwPayment>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwPayments");

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.Aplcrd)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("aplcrd");
            entity.Property(e => e.Chkdte).HasColumnName("chkdte");
            entity.Property(e => e.Chknum)
                .HasMaxLength(20)
                .HasColumnName("chknum");
            entity.Property(e => e.Dscrpt)
                .HasMaxLength(12)
                .HasColumnName("dscrpt");
            entity.Property(e => e.Dsctkn)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("dsctkn");
            entity.Property(e => e.Idnum).HasColumnName("_idnum");
            entity.Property(e => e.Invnum)
                .HasMaxLength(20)
                .HasColumnName("invnum");
            entity.Property(e => e.Jobnum).HasColumnName("jobnum");
        });

        modelBuilder.Entity<VwPendingBid>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwPendingBids");

            entity.Property(e => e.Address)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BidStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.Submitter)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("submitter");
        });

        modelBuilder.Entity<VwProcessedBid>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwProcessedBids");

            entity.Property(e => e.Address)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BidStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.JobId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.Submitter)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("submitter");
        });

        modelBuilder.Entity<VwTotalBudget>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwTotalBudget");

            entity.Property(e => e.Recnum).HasColumnName("recnum");
            entity.Property(e => e.TotalBudget).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<Vwbdglin>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwbdglin");

            entity.Property(e => e.Cstcde)
                .HasColumnType("decimal(15, 3)")
                .HasColumnName("cstcde");
            entity.Property(e => e.Recnum).HasColumnName("recnum");
            entity.Property(e => e.Ttlbdg)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("ttlbdg");
        });

        modelBuilder.Entity<Vwcostcode>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwcostcode");

            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DESCription");
            entity.Property(e => e.Id).HasColumnName("id");
        });

        modelBuilder.Entity<VwjobNumber>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VWJobNumber");

            entity.Property(e => e.Fulljob)
                .HasMaxLength(88)
                .HasColumnName("fulljob");
            entity.Property(e => e.Jobnme)
                .HasMaxLength(75)
                .HasColumnName("jobnme");
            entity.Property(e => e.Recnum).HasColumnName("recnum");
        });

        modelBuilder.Entity<VwjobType>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VWJobType");

            entity.Property(e => e.Recnum).HasColumnName("recnum");
            entity.Property(e => e.Typnme)
                .HasMaxLength(50)
                .HasColumnName("typnme");
        });

        modelBuilder.Entity<Vwjobcstdescrpt>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VWJobcstdescrpt");

            entity.Property(e => e.Cstcde)
                .HasColumnType("decimal(15, 3)")
                .HasColumnName("cstcde");
            entity.Property(e => e.Dscrpt)
                .HasMaxLength(50)
                .HasColumnName("dscrpt");
            entity.Property(e => e.Jobnum).HasColumnName("jobnum");
            entity.Property(e => e.RowId).HasColumnName("RowID");
        });

        modelBuilder.Entity<Vwphase>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwphases");

            entity.Property(e => e.PhasesName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Phsnum).HasColumnName("phsnum");
            entity.Property(e => e.Recnum).HasColumnName("recnum");
        });

        modelBuilder.Entity<VwphasesList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VWPhasesList");

            entity.Property(e => e.Phsnme)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("phsnme");
            entity.Property(e => e.Phsnum).HasColumnName("phsnum");
        });

        modelBuilder.Entity<Vwsession>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VWSessions");

            entity.Property(e => e.DatabaseName)
                .HasMaxLength(128)
                .HasColumnName("database_name");
            entity.Property(e => e.HostName)
                .HasMaxLength(128)
                .HasColumnName("host_name");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.LastRequestStartTime)
                .HasColumnType("datetime")
                .HasColumnName("last_request_start_time");
            entity.Property(e => e.LoginName)
                .HasMaxLength(128)
                .HasColumnName("login_name");
            entity.Property(e => e.ProgramName)
                .HasMaxLength(128)
                .HasColumnName("program_name");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
