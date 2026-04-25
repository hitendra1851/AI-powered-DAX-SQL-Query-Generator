-- QueryMind Sample Schema: Healthcare Star Schema
-- Compatible with: SQL Server, Azure SQL, Microsoft Fabric
-- Use case: Wound care analytics, SNF/HHA billing queries

CREATE TABLE [dbo].[DimPatient] (
    [PatientId]         INT             NOT NULL PRIMARY KEY,
    [MRN]               VARCHAR(20)     NOT NULL,
    [FirstName]         VARCHAR(100)    NOT NULL,
    [LastName]          VARCHAR(100)    NOT NULL,
    [DateOfBirth]       DATE            NOT NULL,
    [Gender]            VARCHAR(10)     NULL,
    [InsuranceType]     VARCHAR(50)     NULL,   -- Medicare, Medicaid, Commercial, Self-Pay
    [PrimaryDiagnosis]  VARCHAR(10)     NULL,   -- ICD-10 code
    [FacilityId]        INT             NOT NULL,
    [AdmissionDate]     DATE            NULL,
    [DischargeDate]     DATE            NULL,
    [IsActive]          BIT             NOT NULL DEFAULT 1
);

CREATE TABLE [dbo].[DimFacility] (
    [FacilityId]        INT             NOT NULL PRIMARY KEY,
    [FacilityName]      VARCHAR(200)    NOT NULL,
    [FacilityType]      VARCHAR(50)     NOT NULL,   -- SNF, HHA, Outpatient
    [State]             CHAR(2)         NOT NULL,
    [Region]            VARCHAR(50)     NOT NULL,
    [TaxId]             VARCHAR(20)     NULL,
    [CCN]               VARCHAR(10)     NULL,       -- CMS Certification Number
    [IsActive]          BIT             NOT NULL DEFAULT 1
);

CREATE TABLE [dbo].[DimDate] (
    [DateId]            INT             NOT NULL PRIMARY KEY,
    [FullDate]          DATE            NOT NULL,
    [Year]              INT             NOT NULL,
    [Quarter]           INT             NOT NULL,
    [Month]             INT             NOT NULL,
    [MonthName]         VARCHAR(20)     NOT NULL,
    [WeekOfYear]        INT             NOT NULL,
    [DayOfWeek]         INT             NOT NULL,
    [DayName]           VARCHAR(20)     NOT NULL,
    [IsWeekend]         BIT             NOT NULL,
    [IsFederalHoliday]  BIT             NOT NULL DEFAULT 0,
    [FiscalYear]        INT             NOT NULL,
    [FiscalQuarter]     INT             NOT NULL
);

CREATE TABLE [dbo].[DimWoundType] (
    [WoundTypeId]       INT             NOT NULL PRIMARY KEY,
    [WoundTypeCode]     VARCHAR(20)     NOT NULL,
    [WoundTypeName]     VARCHAR(200)    NOT NULL,
    [Category]          VARCHAR(100)    NOT NULL,   -- Pressure Injury, Diabetic Foot, Venous, Arterial
    [ICD10Code]         VARCHAR(10)     NULL,
    [SeverityLevel]     INT             NULL        -- 1-4 for pressure injuries
);

CREATE TABLE [dbo].[DimClinician] (
    [ClinicianId]       INT             NOT NULL PRIMARY KEY,
    [NPI]               VARCHAR(10)     NULL,
    [FirstName]         VARCHAR(100)    NOT NULL,
    [LastName]          VARCHAR(100)    NOT NULL,
    [Credentials]       VARCHAR(50)     NULL,       -- RN, MD, DPM, WOCN
    [Specialty]         VARCHAR(100)    NULL,
    [FacilityId]        INT             NOT NULL,
    FOREIGN KEY ([FacilityId]) REFERENCES [dbo].[DimFacility]([FacilityId])
);

CREATE TABLE [dbo].[FactWoundVisit] (
    [VisitId]               BIGINT          NOT NULL PRIMARY KEY,
    [PatientId]             INT             NOT NULL,
    [FacilityId]            INT             NOT NULL,
    [ClinicianId]           INT             NOT NULL,
    [WoundTypeId]           INT             NOT NULL,
    [VisitDateId]           INT             NOT NULL,
    [VisitType]             VARCHAR(50)     NOT NULL,   -- Initial, Follow-up, Discharge
    [WoundArea_cm2]         DECIMAL(10,2)   NULL,
    [WoundDepth_cm]         DECIMAL(10,2)   NULL,
    [HealingProgress_pct]   DECIMAL(5,2)    NULL,
    [IsHealed]              BIT             NOT NULL DEFAULT 0,
    [DaysToHeal]            INT             NULL,
    [ProcedureCount]        INT             NOT NULL DEFAULT 0,
    [SupplyCost]            DECIMAL(18,2)   NULL,
    [LaborCost]             DECIMAL(18,2)   NULL,
    [TotalCost]             DECIMAL(18,2)   NULL,
    [BilledAmount]          DECIMAL(18,2)   NULL,
    [ReimbursedAmount]      DECIMAL(18,2)   NULL,
    [HCPCS_Code]            VARCHAR(10)     NULL,
    FOREIGN KEY ([PatientId])   REFERENCES [dbo].[DimPatient]([PatientId]),
    FOREIGN KEY ([FacilityId])  REFERENCES [dbo].[DimFacility]([FacilityId]),
    FOREIGN KEY ([ClinicianId]) REFERENCES [dbo].[DimClinician]([ClinicianId]),
    FOREIGN KEY ([WoundTypeId]) REFERENCES [dbo].[DimWoundType]([WoundTypeId]),
    FOREIGN KEY ([VisitDateId]) REFERENCES [dbo].[DimDate]([DateId])
);

CREATE TABLE [dbo].[FactBilling] (
    [BillingId]             BIGINT          NOT NULL PRIMARY KEY,
    [VisitId]               BIGINT          NOT NULL,
    [PatientId]             INT             NOT NULL,
    [FacilityId]            INT             NOT NULL,
    [BillingDateId]         INT             NOT NULL,
    [ClaimNumber]           VARCHAR(50)     NULL,
    [InsuranceType]         VARCHAR(50)     NOT NULL,
    [DiagnosisCode]         VARCHAR(10)     NOT NULL,
    [ProcedureCode]         VARCHAR(10)     NOT NULL,
    [BilledAmount]          DECIMAL(18,2)   NOT NULL,
    [AllowedAmount]         DECIMAL(18,2)   NULL,
    [PaidAmount]            DECIMAL(18,2)   NULL,
    [DeniedAmount]          DECIMAL(18,2)   NULL,
    [ClaimStatus]           VARCHAR(50)     NOT NULL,   -- Submitted, Paid, Denied, Appealed
    [DaysToPay]             INT             NULL,
    FOREIGN KEY ([VisitId])         REFERENCES [dbo].[FactWoundVisit]([VisitId]),
    FOREIGN KEY ([PatientId])       REFERENCES [dbo].[DimPatient]([PatientId]),
    FOREIGN KEY ([FacilityId])      REFERENCES [dbo].[DimFacility]([FacilityId]),
    FOREIGN KEY ([BillingDateId])   REFERENCES [dbo].[DimDate]([DateId])
);
