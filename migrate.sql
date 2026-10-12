IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Descriptions] nvarchar(max) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [Salt] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [Pin] nvarchar(max) NULL,
        [Password] nvarchar(max) NULL,
        [UserType] int NOT NULL,
        [OTP] nvarchar(max) NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [DateModified] datetimeoffset NULL,
        [ModifiedBy] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [LastLoginDate] datetimeoffset NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [Businesses] (
        [BusinessId] bigint NOT NULL IDENTITY,
        [BusinessCode] nvarchar(max) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [BusinessName] nvarchar(max) NOT NULL,
        [ContactEmail] nvarchar(max) NOT NULL,
        [SupportEmail] nvarchar(max) NOT NULL,
        [DisputeEmail] nvarchar(max) NOT NULL,
        [BusinessEmail] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [Website] nvarchar(max) NOT NULL,
        [BVN] nvarchar(max) NOT NULL,
        [NIN] nvarchar(max) NOT NULL,
        [BusinessStatus] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [KycCategory] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Businesses] PRIMARY KEY ([BusinessId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [FeeSplitConfigurations] (
        [Id] bigint NOT NULL IDENTITY,
        [BusinessId] bigint NOT NULL,
        [PlatformPercentage] decimal(18,2) NOT NULL,
        [BusinessPercentage] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_FeeSplitConfigurations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        [RoleId1] nvarchar(450) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId1] FOREIGN KEY ([RoleId1]) REFERENCES [AspNetRoles] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        [RoleId1] nvarchar(450) NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId1] FOREIGN KEY ([RoleId1]) REFERENCES [AspNetRoles] ([Id]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [BusinessWorkers] (
        [BusinessWorkerId] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [BusinessId] bigint NOT NULL,
        [IsOwner] bit NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_BusinessWorkers] PRIMARY KEY ([BusinessWorkerId]),
        CONSTRAINT [FK_BusinessWorkers_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([BusinessId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE TABLE [SubAccounts] (
        [SubAccountId] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [BankAccount] nvarchar(max) NULL,
        [BankName] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [SubAccountName] nvarchar(max) NULL,
        [WalletCode] nvarchar(max) NULL,
        [BusinessId] bigint NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [IsMainAccount] bit NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [Bvn] nvarchar(max) NULL,
        [DOB] nvarchar(max) NULL,
        [NIN] nvarchar(max) NULL,
        CONSTRAINT [PK_SubAccounts] PRIMARY KEY ([SubAccountId]),
        CONSTRAINT [FK_SubAccounts_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([BusinessId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId1] ON [AspNetRoleClaims] ([RoleId1]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId1] ON [AspNetUserRoles] ([RoleId1]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE INDEX [IX_BusinessWorkers_BusinessId] ON [BusinessWorkers] ([BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    CREATE INDEX [IX_SubAccounts_BusinessId] ON [SubAccounts] ([BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250123193815_initial db creation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250123193815_initial db creation', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250127125319_updated db'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'UserType');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [UserType] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250127125319_updated db'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250127125319_updated db', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250127134705_updated db user table'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [PinEncrypt] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250127134705_updated db user table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250127134705_updated db user table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203112812_updated users table'
)
BEGIN
    ALTER TABLE [Businesses] ADD [BusinessLogo] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203112812_updated users table'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Name] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203112812_updated users table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250203112812_updated users table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203142003_updated role table'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [BusinessId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203142003_updated role table'
)
BEGIN
    ALTER TABLE [AspNetRoles] ADD [BusinessId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203142003_updated role table'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_BusinessId] ON [AspNetUsers] ([BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203142003_updated role table'
)
BEGIN
    CREATE INDEX [IX_AspNetRoles_BusinessId] ON [AspNetRoles] ([BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203142003_updated role table'
)
BEGIN
    ALTER TABLE [AspNetRoles] ADD CONSTRAINT [FK_AspNetRoles_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203142003_updated role table'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203142003_updated role table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250203142003_updated role table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203152608_added wallet provisioning tables'
)
BEGIN
    CREATE TABLE [SterlingBankWallets] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [AgrovestoUserId] nvarchar(max) NOT NULL,
        [AccountNumber] nvarchar(max) NOT NULL,
        [LedgerAccountNumber] nvarchar(max) NOT NULL,
        [WalletId] nvarchar(max) NOT NULL,
        [InternalAccountId] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_SterlingBankWallets] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203152608_added wallet provisioning tables'
)
BEGIN
    CREATE TABLE [WalletProviders] (
        [ProviderId] bigint NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WalletProviders] PRIMARY KEY ([ProviderId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203152608_added wallet provisioning tables'
)
BEGIN
    CREATE TABLE [WalletProviderParameters] (
        [ParameterId] bigint NOT NULL IDENTITY,
        [Value] nvarchar(max) NOT NULL,
        [Key] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [WalletProviderProviderId] bigint NOT NULL,
        [Env] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WalletProviderParameters] PRIMARY KEY ([ParameterId]),
        CONSTRAINT [FK_WalletProviderParameters_WalletProviders_WalletProviderProviderId] FOREIGN KEY ([WalletProviderProviderId]) REFERENCES [WalletProviders] ([ProviderId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203152608_added wallet provisioning tables'
)
BEGIN
    CREATE INDEX [IX_WalletProviderParameters_WalletProviderProviderId] ON [WalletProviderParameters] ([WalletProviderProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250203152608_added wallet provisioning tables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250203152608_added wallet provisioning tables', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    ALTER TABLE [SubAccounts] ADD [FirstName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    ALTER TABLE [SubAccounts] ADD [LastName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    ALTER TABLE [SterlingBankWallets] ADD [IsABusinessAccount] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    ALTER TABLE [SterlingBankWallets] ADD [OrganizationId] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    ALTER TABLE [SterlingBankWallets] ADD [OwnerId] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Businesses]') AND [c].[name] = N'Website');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Businesses] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Businesses] ALTER COLUMN [Website] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Businesses]') AND [c].[name] = N'SupportEmail');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Businesses] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Businesses] ALTER COLUMN [SupportEmail] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Businesses]') AND [c].[name] = N'NIN');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Businesses] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Businesses] ALTER COLUMN [NIN] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Businesses]') AND [c].[name] = N'DisputeEmail');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Businesses] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Businesses] ALTER COLUMN [DisputeEmail] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Businesses]') AND [c].[name] = N'BVN');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Businesses] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Businesses] ALTER COLUMN [BVN] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250208160407_updated subaccount table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250208160407_updated subaccount table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250211131636_paas'
)
BEGIN
    ALTER TABLE [SubAccounts] ADD [SubAccountType] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250211131636_paas'
)
BEGIN
    CREATE TABLE [Agents] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NOT NULL,
        [State] nvarchar(max) NOT NULL,
        [Lga] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [OnBoardedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Agents] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250211131636_paas'
)
BEGIN
    CREATE TABLE [Cooperatives] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NOT NULL,
        [State] nvarchar(max) NOT NULL,
        [Lga] nvarchar(max) NOT NULL,
        [ContactPerson] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [OnBoardedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Cooperatives] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250211131636_paas'
)
BEGIN
    CREATE TABLE [Farmers] (
        [Id] bigint NOT NULL IDENTITY,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NOT NULL,
        [State] nvarchar(max) NOT NULL,
        [Lga] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NULL,
        [UserId] nvarchar(max) NOT NULL,
        [OnBoardedOn] datetimeoffset NOT NULL,
        [Gender] nvarchar(max) NULL,
        [DateOfBirth] nvarchar(max) NULL,
        [OnboardedByUserId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Farmers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250211131636_paas'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250211131636_paas', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250211140840_added functionality for paas'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250211140840_added functionality for paas', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250212182515_updated subaccount table__'
)
BEGIN
    ALTER TABLE [Cooperatives] ADD [IsAssignedToAgentId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250212182515_updated subaccount table__'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250212182515_updated subaccount table__', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250212182930_added nullable to account'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'WalletId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [WalletId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250212182930_added nullable to account'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'OwnerId');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [OwnerId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250212182930_added nullable to account'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'OrganizationId');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [OrganizationId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250212182930_added nullable to account'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'LedgerAccountNumber');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [LedgerAccountNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250212182930_added nullable to account'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'InternalAccountId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [InternalAccountId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250212182930_added nullable to account'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'AccountNumber');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [AccountNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250212182930_added nullable to account'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250212182930_added nullable to account', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250213080934_updated business page'
)
BEGIN
    ALTER TABLE [Businesses] ADD [BusinessCodeSignage] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250213080934_updated business page'
)
BEGIN
    ALTER TABLE [Businesses] ADD [BusinessCodeSignageSalt] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250213080934_updated business page'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250213080934_updated business page', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250213135801_updated business pageagain'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250213135801_updated business pageagain', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214163420_added more tables'
)
BEGIN
    ALTER TABLE [Cooperatives] ADD [HasbeenAssignedAgent] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214163420_added more tables'
)
BEGIN
    CREATE TABLE [Bundles] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [TotalBundlePackagePacked] bigint NOT NULL,
        [AvailableBundlePackagePacked] bigint NOT NULL,
        [TotalBundlePackageRequested] bigint NOT NULL,
        [TotalBundlePackageDisbursed] bigint NOT NULL,
        [BundlePrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Bundles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214163420_added more tables'
)
BEGIN
    CREATE TABLE [FarmPlotDetails] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerId] bigint NOT NULL,
        [FarmerName] nvarchar(max) NOT NULL,
        [Latitude1] float NOT NULL,
        [Longitude1] float NOT NULL,
        [Latitude2] float NOT NULL,
        [Longitude2] float NOT NULL,
        [Latitude3] float NOT NULL,
        [Longitude3] float NOT NULL,
        [Latitude4] float NOT NULL,
        [Longitude4] float NOT NULL,
        [FarmLocation] nvarchar(max) NOT NULL,
        [FarmState] nvarchar(max) NOT NULL,
        [FarmLga] nvarchar(max) NOT NULL,
        [CoordinateTakenBy] nvarchar(max) NOT NULL,
        [CoordinateTakenByUserId] bigint NOT NULL,
        CONSTRAINT [PK_FarmPlotDetails] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214163420_added more tables'
)
BEGIN
    CREATE TABLE [WarehouseInputInventories] (
        [Id] bigint NOT NULL IDENTITY,
        [InputName] nvarchar(max) NOT NULL,
        [InputDescription] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Brand] nvarchar(max) NOT NULL,
        [Price] nvarchar(max) NOT NULL,
        [Size] nvarchar(max) NOT NULL,
        [Unit] bigint NOT NULL,
        [UnitStockedUp] bigint NOT NULL,
        [UnitDepleted] bigint NOT NULL,
        [UnitBalance] bigint NOT NULL,
        [IsAvailable] bit NOT NULL,
        [InventoryStockedOn] datetimeoffset NOT NULL,
        [ModifiedOn] datetimeoffset NOT NULL,
        [ManagedByUserId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WarehouseInputInventories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214163420_added more tables'
)
BEGIN
    CREATE TABLE [Warehouses] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [State] nvarchar(max) NOT NULL,
        [Lga] nvarchar(max) NOT NULL,
        [ManagedByUserId] bigint NOT NULL,
        [HasbeenAssignedManager] bit NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214163420_added more tables'
)
BEGIN
    CREATE TABLE [WarehouseUsers] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NOT NULL,
        [State] nvarchar(max) NOT NULL,
        [Lga] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WarehouseUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214163420_added more tables'
)
BEGIN
    CREATE TABLE [BundleItems] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Unit] bigint NOT NULL,
        [WarehouseInputInventoryId] bigint NOT NULL,
        [BundleId] bigint NOT NULL,
        CONSTRAINT [PK_BundleItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BundleItems_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [Bundles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214163420_added more tables'
)
BEGIN
    CREATE INDEX [IX_BundleItems_BundleId] ON [BundleItems] ([BundleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214163420_added more tables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250214163420_added more tables', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214172955_updated farm plot details'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [AreaInDegress] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214172955_updated farm plot details'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [AreaInHectare] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214172955_updated farm plot details'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [AreaInSqMetre] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214172955_updated farm plot details'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [BelongToBusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250214172955_updated farm plot details'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250214172955_updated farm plot details', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250217123421_updated farm plot'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmPlotDetails]') AND [c].[name] = N'CoordinateTakenByUserId');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [FarmPlotDetails] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [FarmPlotDetails] ALTER COLUMN [CoordinateTakenByUserId] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250217123421_updated farm plot'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250217123421_updated farm plot', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218091936_modified warehouse'
)
BEGIN
    ALTER TABLE [Warehouses] ADD [Capacity] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218091936_modified warehouse'
)
BEGIN
    ALTER TABLE [Warehouses] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218091936_modified warehouse'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250218091936_modified warehouse', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218100600_updated warehouse users'
)
BEGIN
    ALTER TABLE [WarehouseUsers] ADD [UserId] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218100600_updated warehouse users'
)
BEGIN
    ALTER TABLE [WarehouseUsers] ADD [WarehouseId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218100600_updated warehouse users'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250218100600_updated warehouse users', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218113103_modified inventory and bundle'
)
BEGIN
    EXEC sp_rename N'[WarehouseInputInventories].[Unit]', N'WarehouseId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218113103_modified inventory and bundle'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WarehouseInputInventories]') AND [c].[name] = N'Price');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [WarehouseInputInventories] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [WarehouseInputInventories] ALTER COLUMN [Price] decimal(18,2) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218113103_modified inventory and bundle'
)
BEGIN
    ALTER TABLE [WarehouseInputInventories] ADD [BelongToBusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218113103_modified inventory and bundle'
)
BEGIN
    ALTER TABLE [WarehouseInputInventories] ADD [UnitAdded] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218113103_modified inventory and bundle'
)
BEGIN
    ALTER TABLE [Bundles] ADD [BelongToBusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218113103_modified inventory and bundle'
)
BEGIN
    ALTER TABLE [Bundles] ADD [CreatedBy] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218113103_modified inventory and bundle'
)
BEGIN
    ALTER TABLE [Bundles] ADD [CreatedOn] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250218113103_modified inventory and bundle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250218113103_modified inventory and bundle', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220122647_added financing tracker'
)
BEGIN
    ALTER TABLE [Bundles] ADD [BundleEquityPlusCommission] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220122647_added financing tracker'
)
BEGIN
    ALTER TABLE [Bundles] ADD [CommissionPrice] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220122647_added financing tracker'
)
BEGIN
    ALTER TABLE [Bundles] ADD [ShouldAddCommission] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220122647_added financing tracker'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250220122647_added financing tracker', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220123656_added bundle subscription'
)
BEGIN
    CREATE TABLE [BundleSubscriptionInterests] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerUserId] nvarchar(max) NOT NULL,
        [FarmerName] nvarchar(max) NOT NULL,
        [InterestedBundleId] bigint NOT NULL,
        [InterestedBundleName] nvarchar(max) NOT NULL,
        [QuantityOfInterest] bigint NOT NULL,
        [ExpectedEquityPricePerBundle] decimal(18,2) NOT NULL,
        [ExpectedTotalEquityPrice] decimal(18,2) NOT NULL,
        [TotalEquityPaid] decimal(18,2) NOT NULL,
        [EquityPaymentBalanceDeficit] decimal(18,2) NOT NULL,
        [TotalQuantityPaidFor] bigint NOT NULL,
        [PaymentStatus] nvarchar(max) NOT NULL,
        [PaymentDate] datetimeoffset NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [EquityPlusCommission] decimal(18,2) NOT NULL,
        [IsCommissionPaidOut] bit NOT NULL,
        [ShouldAddCommission] bit NOT NULL,
        [CommissionPrice] decimal(18,2) NOT NULL,
        [CommissionPercentage] float NOT NULL,
        [OfficerInChargeUserId] nvarchar(max) NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_BundleSubscriptionInterests] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220123656_added bundle subscription'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250220123656_added bundle subscription', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220144910_updated dbconfig'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250220144910_updated dbconfig', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250224175223_updated bundle interest'
)
BEGIN
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BundleSubscriptionInterests]') AND [c].[name] = N'PaymentDate');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [BundleSubscriptionInterests] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [BundleSubscriptionInterests] ALTER COLUMN [PaymentDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250224175223_updated bundle interest'
)
BEGIN
    ALTER TABLE [BundleSubscriptionInterests] ADD [CooperativeId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250224175223_updated bundle interest'
)
BEGIN
    ALTER TABLE [BundleSubscriptionInterests] ADD [Location] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250224175223_updated bundle interest'
)
BEGIN
    ALTER TABLE [BundleSubscriptionInterests] ADD [Reference] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250224175223_updated bundle interest'
)
BEGIN
    ALTER TABLE [Bundles] ADD [BundleEquityPrice] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250224175223_updated bundle interest'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250224175223_updated bundle interest', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250225043701_created optional property on bundle interest'
)
BEGIN
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BundleSubscriptionInterests]') AND [c].[name] = N'Reference');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [BundleSubscriptionInterests] DROP CONSTRAINT [' + @var15 + '];');
    ALTER TABLE [BundleSubscriptionInterests] ALTER COLUMN [Reference] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250225043701_created optional property on bundle interest'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250225043701_created optional property on bundle interest', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250226052959_updated user table'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [BusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250226052959_updated user table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250226052959_updated user table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250226053040_updated user tablemodified'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'BusinessCode');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [BusinessCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250226053040_updated user tablemodified'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250226053040_updated user tablemodified', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250227050613_updated cooperative table'
)
BEGIN
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cooperatives]') AND [c].[name] = N'Email');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Cooperatives] DROP CONSTRAINT [' + @var17 + '];');
    ALTER TABLE [Cooperatives] ALTER COLUMN [Email] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250227050613_updated cooperative table'
)
BEGIN
    DECLARE @var18 sysname;
    SELECT @var18 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agents]') AND [c].[name] = N'Email');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Agents] DROP CONSTRAINT [' + @var18 + '];');
    ALTER TABLE [Agents] ALTER COLUMN [Email] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250227050613_updated cooperative table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250227050613_updated cooperative table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250227063025_updated farmers table'
)
BEGIN
    ALTER TABLE [Farmers] ADD [BelongToCooperativeId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250227063025_updated farmers table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250227063025_updated farmers table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250228100040_updated input table'
)
BEGIN
    ALTER TABLE [WarehouseInputInventories] ADD [DeliveredTo] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250228100040_updated input table'
)
BEGIN
    ALTER TABLE [WarehouseInputInventories] ADD [InputComingFrom] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250228100040_updated input table'
)
BEGIN
    ALTER TABLE [WarehouseInputInventories] ADD [ReceivedBy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250228100040_updated input table'
)
BEGIN
    ALTER TABLE [WarehouseInputInventories] ADD [TransitStatus] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250228100040_updated input table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250228100040_updated input table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250228144304_updated input iventory'
)
BEGIN
    DECLARE @var19 sysname;
    SELECT @var19 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WarehouseInputInventories]') AND [c].[name] = N'ModifiedOn');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [WarehouseInputInventories] DROP CONSTRAINT [' + @var19 + '];');
    ALTER TABLE [WarehouseInputInventories] ALTER COLUMN [ModifiedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250228144304_updated input iventory'
)
BEGIN
    DECLARE @var20 sysname;
    SELECT @var20 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WarehouseInputInventories]') AND [c].[name] = N'InventoryStockedOn');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [WarehouseInputInventories] DROP CONSTRAINT [' + @var20 + '];');
    ALTER TABLE [WarehouseInputInventories] ALTER COLUMN [InventoryStockedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250228144304_updated input iventory'
)
BEGIN
    ALTER TABLE [WarehouseInputInventories] ADD [Reference] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250228144304_updated input iventory'
)
BEGIN
    ALTER TABLE [WarehouseInputInventories] ADD [Shipmentdate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250228144304_updated input iventory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250228144304_updated input iventory', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250306111142_added farmer image table'
)
BEGIN
    CREATE TABLE [FarmerImages] (
        [Id] bigint NOT NULL IDENTITY,
        [ImageName] nvarchar(max) NOT NULL,
        [FarmerUserId] nvarchar(max) NOT NULL,
        [IMageKey] nvarchar(max) NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_FarmerImages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250306111142_added farmer image table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250306111142_added farmer image table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250308062422_added warehouseId to bundle'
)
BEGIN
    ALTER TABLE [Bundles] ADD [WarehouseId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250308062422_added warehouseId to bundle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250308062422_added warehouseId to bundle', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250310124546_updated farm plot details with date'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [CreatedOn] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250310124546_updated farm plot details with date'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250310124546_updated farm plot details with date', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250310151139_added bundle distribution'
)
BEGIN
    CREATE TABLE [BundleSubscriptionDisbursements] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerUserId] nvarchar(max) NOT NULL,
        [FarmerName] nvarchar(max) NOT NULL,
        [InterestedBundleId] bigint NOT NULL,
        [InterestedBundleName] nvarchar(max) NOT NULL,
        [TotalQuantityPaidFor] bigint NOT NULL,
        [TotalQuantityReceived] bigint NOT NULL,
        [DisbursedOn] datetimeoffset NOT NULL,
        [Reference] nvarchar(max) NULL,
        [OfficerInChargeUserId] nvarchar(max) NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NOT NULL,
        [CooperativeId] bigint NOT NULL,
        [DisbursedBy] nvarchar(max) NOT NULL,
        [PaymentStatus] nvarchar(max) NOT NULL,
        [InputAlreadyCollected] bit NOT NULL,
        [InputCollectedByPictureOf] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_BundleSubscriptionDisbursements] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250310151139_added bundle distribution'
)
BEGIN
    CREATE TABLE [FarmActivityTypes] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_FarmActivityTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250310151139_added bundle distribution'
)
BEGIN
    CREATE TABLE [FarmVisitationActivities] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmActivityId] bigint NOT NULL,
        [FarmerUserId] nvarchar(max) NOT NULL,
        [FarmerName] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Recommendation] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [OfficerInChargeId] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_FarmVisitationActivities] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250310151139_added bundle distribution'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250310151139_added bundle distribution', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250311191234_updated farm log table'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [BelongToBusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250311191234_updated farm log table'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [CooperativeId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250311191234_updated farm log table'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [OfficerName] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250311191234_updated farm log table'
)
BEGIN
    ALTER TABLE [FarmActivityTypes] ADD [BelongToBusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250311191234_updated farm log table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250311191234_updated farm log table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250319170125_added update to role table'
)
BEGIN
    ALTER TABLE [AspNetRoles] ADD [IsAvailableToBusiness] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250319170125_added update to role table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250319170125_added update to role table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250325092611_added InputDeliveryTracker'
)
BEGIN
    CREATE TABLE [InputDeliveryTrackers] (
        [Id] bigint NOT NULL IDENTITY,
        [WarehouseId] bigint NOT NULL,
        [WarehouseInputInventoryId] bigint NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [InputName] nvarchar(max) NOT NULL,
        [QuantityLoaded] bigint NOT NULL,
        [QuantityDelivered] bigint NOT NULL,
        [QuantityReceived] bigint NOT NULL,
        [DeliveredTo] nvarchar(max) NOT NULL,
        [DeliveredBy] nvarchar(max) NOT NULL,
        [Reference] nvarchar(max) NULL,
        [DeliveryContact] nvarchar(max) NOT NULL,
        [PickupLocation] nvarchar(max) NOT NULL,
        [DeliveryLocation] nvarchar(max) NOT NULL,
        [LogisticType] nvarchar(max) NOT NULL,
        [LogisticNumber] nvarchar(max) NOT NULL,
        [PickUpDate] datetimeoffset NOT NULL,
        [DeliveryDate] datetimeoffset NULL,
        CONSTRAINT [PK_InputDeliveryTrackers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250325092611_added InputDeliveryTracker'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250325092611_added InputDeliveryTracker', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250325103208_added InputDeliveryTracker updated'
)
BEGIN
    ALTER TABLE [InputDeliveryTrackers] ADD [BusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250325103208_added InputDeliveryTracker updated'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250325103208_added InputDeliveryTracker updated', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250405045607_added FarmerSalesTracker'
)
BEGIN
    DECLARE @var21 sysname;
    SELECT @var21 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'ManagedByUserId');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var21 + '];');
    ALTER TABLE [Warehouses] ALTER COLUMN [ManagedByUserId] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250405045607_added FarmerSalesTracker'
)
BEGIN
    DECLARE @var22 sysname;
    SELECT @var22 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cooperatives]') AND [c].[name] = N'IsAssignedToAgentId');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Cooperatives] DROP CONSTRAINT [' + @var22 + '];');
    ALTER TABLE [Cooperatives] ALTER COLUMN [IsAssignedToAgentId] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250405045607_added FarmerSalesTracker'
)
BEGIN
    CREATE TABLE [FarmerSalesTracker] (
        [Id] bigint NOT NULL IDENTITY,
        [CollectionOfficerId] nvarchar(max) NOT NULL,
        [CollectionDate] datetimeoffset NOT NULL,
        [MarketName] nvarchar(max) NOT NULL,
        [MarketLocation] nvarchar(max) NOT NULL,
        [MarketState] nvarchar(max) NULL,
        [MarketLga] nvarchar(max) NULL,
        [FarmerUserId] nvarchar(max) NOT NULL,
        [CropSold] nvarchar(max) NOT NULL,
        [QuantityBroughtToMarket] bigint NOT NULL,
        [QuantitySoldToday] bigint NOT NULL,
        [UnitType] nvarchar(max) NOT NULL,
        [CropPricePerUnitSold] decimal(18,2) NOT NULL,
        [TotalPriceSold] decimal(18,2) NOT NULL,
        [IntendedPriceToSellPerUnit] decimal(18,2) NOT NULL,
        [IsCropTransportedToMarket] bit NOT NULL,
        [MeansOfTransport] nvarchar(max) NULL,
        [TransportCost] decimal(18,2) NOT NULL,
        [IsCropPackaged] bit NOT NULL,
        [PackagingType] nvarchar(max) NULL,
        [CostOfPackagingMaterial] decimal(18,2) NOT NULL,
        [OtherCost] decimal(18,2) NOT NULL,
        [ModeOfPayment] nvarchar(max) NOT NULL,
        [TotalTimeSpentInMarket] bigint NOT NULL,
        [SalesType] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_FarmerSalesTracker] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250405045607_added FarmerSalesTracker'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250405045607_added FarmerSalesTracker', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250409115427_added market sales logger'
)
BEGIN
    ALTER TABLE [SterlingBankWallets] ADD [CafeOneCustomerId] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250409115427_added market sales logger'
)
BEGIN
    ALTER TABLE [FarmerSalesTracker] ADD [BelongToBusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250409115427_added market sales logger'
)
BEGIN
    ALTER TABLE [FarmerSalesTracker] ADD [TransactionActualDate] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250409115427_added market sales logger'
)
BEGIN
    ALTER TABLE [FarmerSalesTracker] ADD [supervisorId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250409115427_added market sales logger'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250409115427_added market sales logger', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250409124319_added market day'
)
BEGIN
    ALTER TABLE [FarmerSalesTracker] ADD [MarketDay] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250409124319_added market day'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250409124319_added market day', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250410091022_added supervisor to agents'
)
BEGIN
    ALTER TABLE [Agents] ADD [SupervisorUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250410091022_added supervisor to agents'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250410091022_added supervisor to agents', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250415044335_added trader role'
)
BEGIN
    CREATE TABLE [Traders] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NOT NULL,
        [State] nvarchar(max) NOT NULL,
        [Lga] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [OnBoardedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Traders] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250415044335_added trader role'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250415044335_added trader role', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250415150853_extended market record'
)
BEGIN
    DECLARE @var23 sysname;
    SELECT @var23 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmerSalesTracker]') AND [c].[name] = N'QuantitySoldToday');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [FarmerSalesTracker] DROP CONSTRAINT [' + @var23 + '];');
    ALTER TABLE [FarmerSalesTracker] ALTER COLUMN [QuantitySoldToday] float NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250415150853_extended market record'
)
BEGIN
    DECLARE @var24 sysname;
    SELECT @var24 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmerSalesTracker]') AND [c].[name] = N'QuantityBroughtToMarket');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [FarmerSalesTracker] DROP CONSTRAINT [' + @var24 + '];');
    ALTER TABLE [FarmerSalesTracker] ALTER COLUMN [QuantityBroughtToMarket] float NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250415150853_extended market record'
)
BEGIN
    ALTER TABLE [FarmerSalesTracker] ADD [LowestUnitQuntity] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250415150853_extended market record'
)
BEGIN
    ALTER TABLE [FarmerSalesTracker] ADD [PriceOfLowestUnit] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250415150853_extended market record'
)
BEGIN
    ALTER TABLE [FarmerSalesTracker] ADD [SoldCropUnitType] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250415150853_extended market record'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250415150853_extended market record', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250422151058_updated farm visitation log'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [FarmerLabourCost] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250422151058_updated farm visitation log'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [FarmerTransportCost] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250422151058_updated farm visitation log'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [InputCost] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250422151058_updated farm visitation log'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [InputQuantity] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250422151058_updated farm visitation log'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [InputType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250422151058_updated farm visitation log'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [OtherCost] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250422151058_updated farm visitation log'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [TotalCost] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250422151058_updated farm visitation log'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250422151058_updated farm visitation log', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250429171142_updated sterling wallet creation'
)
BEGIN
    EXEC sp_rename N'[SterlingBankWallets].[CafeOneCustomerId]', N'customerTypeId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250429171142_updated sterling wallet creation'
)
BEGIN
    DECLARE @var25 sysname;
    SELECT @var25 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'AccountNumber');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var25 + '];');
    EXEC(N'UPDATE [SterlingBankWallets] SET [AccountNumber] = N'''' WHERE [AccountNumber] IS NULL');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [AccountNumber] nvarchar(max) NOT NULL;
    ALTER TABLE [SterlingBankWallets] ADD DEFAULT N'' FOR [AccountNumber];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250429171142_updated sterling wallet creation'
)
BEGIN
    ALTER TABLE [SterlingBankWallets] ADD [AccountName] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250429171142_updated sterling wallet creation'
)
BEGIN
    ALTER TABLE [SterlingBankWallets] ADD [BankCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250429171142_updated sterling wallet creation'
)
BEGIN
    ALTER TABLE [SterlingBankWallets] ADD [BankName] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250429171142_updated sterling wallet creation'
)
BEGIN
    ALTER TABLE [SterlingBankWallets] ADD [CustomerId] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250429171142_updated sterling wallet creation'
)
BEGIN
    ALTER TABLE [SterlingBankWallets] ADD [currencyId] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250429171142_updated sterling wallet creation'
)
BEGIN
    ALTER TABLE [SterlingBankWallets] ADD [walletClassificationId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250429171142_updated sterling wallet creation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250429171142_updated sterling wallet creation', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250503071936_added flutterwave service'
)
BEGIN
    CREATE TABLE [PaymentProviders] (
        [ProviderId] bigint NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PaymentProviders] PRIMARY KEY ([ProviderId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250503071936_added flutterwave service'
)
BEGIN
    CREATE TABLE [PaymentProviderParameters] (
        [ParameterId] bigint NOT NULL IDENTITY,
        [Key] nvarchar(max) NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [PaymentProviderProviderId] bigint NOT NULL,
        [Env] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PaymentProviderParameters] PRIMARY KEY ([ParameterId]),
        CONSTRAINT [FK_PaymentProviderParameters_PaymentProviders_PaymentProviderProviderId] FOREIGN KEY ([PaymentProviderProviderId]) REFERENCES [PaymentProviders] ([ProviderId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250503071936_added flutterwave service'
)
BEGIN
    CREATE INDEX [IX_PaymentProviderParameters_PaymentProviderProviderId] ON [PaymentProviderParameters] ([PaymentProviderProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250503071936_added flutterwave service'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250503071936_added flutterwave service', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250503100512_updated payment provider'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250503100512_updated payment provider', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250505214249_added bundle other costs'
)
BEGIN
    CREATE TABLE [OtherBundlePaymentCosts] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [BundleId] bigint NOT NULL,
        CONSTRAINT [PK_OtherBundlePaymentCosts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OtherBundlePaymentCosts_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [Bundles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250505214249_added bundle other costs'
)
BEGIN
    CREATE INDEX [IX_OtherBundlePaymentCosts_BundleId] ON [OtherBundlePaymentCosts] ([BundleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250505214249_added bundle other costs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250505214249_added bundle other costs', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250506155420_added shipment date to delivery tracker'
)
BEGIN
    ALTER TABLE [InputDeliveryTrackers] ADD [Shipmentdate] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250506155420_added shipment date to delivery tracker'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250506155420_added shipment date to delivery tracker', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250509093746_updated input subscription'
)
BEGIN
    ALTER TABLE [BundleSubscriptionInterests] ADD [SubscriptionStatus] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250509093746_updated input subscription'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250509093746_updated input subscription', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250509142503_added maturity date'
)
BEGIN
    ALTER TABLE [BundleSubscriptionInterests] ADD [MaturityDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250509142503_added maturity date'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250509142503_added maturity date', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250509143222_added maturity date to bundle'
)
BEGIN
    ALTER TABLE [Bundles] ADD [MaturityDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250509143222_added maturity date to bundle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250509143222_added maturity date to bundle', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250512130645_updated null column'
)
BEGIN
    DECLARE @var26 sysname;
    SELECT @var26 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'customerTypeId');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var26 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [customerTypeId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250512130645_updated null column'
)
BEGIN
    DECLARE @var27 sysname;
    SELECT @var27 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'currencyId');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var27 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [currencyId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250512130645_updated null column'
)
BEGIN
    DECLARE @var28 sysname;
    SELECT @var28 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'BankName');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var28 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [BankName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250512130645_updated null column'
)
BEGIN
    DECLARE @var29 sysname;
    SELECT @var29 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'BankCode');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var29 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [BankCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250512130645_updated null column'
)
BEGIN
    DECLARE @var30 sysname;
    SELECT @var30 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'AccountNumber');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var30 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [AccountNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250512130645_updated null column'
)
BEGIN
    DECLARE @var31 sysname;
    SELECT @var31 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SterlingBankWallets]') AND [c].[name] = N'AccountName');
    IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [SterlingBankWallets] DROP CONSTRAINT [' + @var31 + '];');
    ALTER TABLE [SterlingBankWallets] ALTER COLUMN [AccountName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250512130645_updated null column'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250512130645_updated null column', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250512183008_updated warehouse table'
)
BEGIN
    DECLARE @var32 sysname;
    SELECT @var32 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'ManagedByUserId');
    IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var32 + '];');
    ALTER TABLE [Warehouses] ALTER COLUMN [ManagedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250512183008_updated warehouse table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250512183008_updated warehouse table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250514054036_updated subaccount table names'
)
BEGIN
    ALTER TABLE [SubAccounts] ADD [MiddleName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250514054036_updated subaccount table names'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250514054036_updated subaccount table names', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250515131716_ added payout'
)
BEGIN
    CREATE TABLE [Payouts] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [SourceAccountName] nvarchar(max) NULL,
        [SourceAccountNumber] nvarchar(max) NULL,
        [SourceBankCode] nvarchar(max) NULL,
        [DestinationAccountNumber] nvarchar(max) NULL,
        [DestinationAccountName] nvarchar(max) NULL,
        [DestinationBank] nvarchar(max) NULL,
        [Reference] nvarchar(max) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [IsSuccessfulTransaction] bit NOT NULL,
        [CreatedOn] datetimeoffset NULL,
        CONSTRAINT [PK_Payouts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250515131716_ added payout'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250515131716_ added payout', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250523154230_added farmer identifier'
)
BEGIN
    ALTER TABLE [BundleSubscriptionInterests] ADD [FarmerIdentifier] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250523154230_added farmer identifier'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250523154230_added farmer identifier', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250523162340_added isdeitted'
)
BEGIN
    ALTER TABLE [BundleSubscriptionInterests] ADD [IsDebitted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250523162340_added isdeitted'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250523162340_added isdeitted', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250528083435_added url for business configuration'
)
BEGIN
    ALTER TABLE [Businesses] ADD [PropagateUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250528083435_added url for business configuration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250528083435_added url for business configuration', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [WarehouseUsers] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [Warehouses] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [WarehouseInputInventories] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [Traders] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [SubAccounts] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [InputDeliveryTrackers] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [FarmVisitationActivities] ADD [SupportingImageUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [FarmerSalesTracker] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [Farmers] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [FarmerImages] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [FarmActivityTypes] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [Cooperatives] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [BusinessWorkers] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [BundleSubscriptionInterests] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [BundleSubscriptionDisbursements] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [Bundles] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    ALTER TABLE [Agents] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184251_created organization entitties'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250622184251_created organization entitties', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623083040_added state'
)
BEGIN
    CREATE TABLE [BusinessOrganizations] (
        [OrganizaionId] bigint NOT NULL IDENTITY,
        [BusinessCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [OrganizationName] nvarchar(max) NOT NULL,
        [ContacPerson] nvarchar(max) NULL,
        [Contact] nvarchar(max) NULL,
        [ContactEmail] nvarchar(max) NULL,
        [OrganizationEmail] nvarchar(max) NULL,
        [Location] nvarchar(max) NULL,
        CONSTRAINT [PK_BusinessOrganizations] PRIMARY KEY ([OrganizaionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623083040_added state'
)
BEGIN
    CREATE TABLE [States] (
        [Id] bigint NOT NULL IDENTITY,
        [State] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_States] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623083040_added state'
)
BEGIN
    CREATE TABLE [LGAs] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [StatesId] bigint NULL,
        CONSTRAINT [PK_LGAs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LGAs_States_StatesId] FOREIGN KEY ([StatesId]) REFERENCES [States] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623083040_added state'
)
BEGIN
    CREATE TABLE [Wards] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [LGAId] bigint NULL,
        CONSTRAINT [PK_Wards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Wards_LGAs_LGAId] FOREIGN KEY ([LGAId]) REFERENCES [LGAs] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623083040_added state'
)
BEGIN
    CREATE INDEX [IX_LGAs_StatesId] ON [LGAs] ([StatesId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623083040_added state'
)
BEGIN
    CREATE INDEX [IX_Wards_LGAId] ON [Wards] ([LGAId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623083040_added state'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250623083040_added state', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    ALTER TABLE [LGAs] DROP CONSTRAINT [FK_LGAs_States_StatesId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    ALTER TABLE [Wards] DROP CONSTRAINT [FK_Wards_LGAs_LGAId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    EXEC sp_rename N'[Wards].[LGAId]', N'LgaId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    EXEC sp_rename N'[Wards].[Id]', N'WardId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    EXEC sp_rename N'[Wards].[IX_Wards_LGAId]', N'IX_Wards_LgaId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    EXEC sp_rename N'[States].[Id]', N'StateId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    EXEC sp_rename N'[LGAs].[StatesId]', N'StatesStateId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    EXEC sp_rename N'[LGAs].[Id]', N'LgaId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    EXEC sp_rename N'[LGAs].[IX_LGAs_StatesId]', N'IX_LGAs_StatesStateId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    ALTER TABLE [LGAs] ADD CONSTRAINT [FK_LGAs_States_StatesStateId] FOREIGN KEY ([StatesStateId]) REFERENCES [States] ([StateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    ALTER TABLE [Wards] ADD CONSTRAINT [FK_Wards_LGAs_LgaId] FOREIGN KEY ([LgaId]) REFERENCES [LGAs] ([LgaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623092927_added state and lgas'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250623092927_added state and lgas', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623102029_updated lgas column name'
)
BEGIN
    ALTER TABLE [LGAs] DROP CONSTRAINT [FK_LGAs_States_StatesStateId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623102029_updated lgas column name'
)
BEGIN
    ALTER TABLE [Wards] DROP CONSTRAINT [FK_Wards_LGAs_LgaId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623102029_updated lgas column name'
)
BEGIN
    DROP INDEX [IX_Wards_LgaId] ON [Wards];
    DECLARE @var33 sysname;
    SELECT @var33 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Wards]') AND [c].[name] = N'LgaId');
    IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [Wards] DROP CONSTRAINT [' + @var33 + '];');
    EXEC(N'UPDATE [Wards] SET [LgaId] = CAST(0 AS bigint) WHERE [LgaId] IS NULL');
    ALTER TABLE [Wards] ALTER COLUMN [LgaId] bigint NOT NULL;
    ALTER TABLE [Wards] ADD DEFAULT CAST(0 AS bigint) FOR [LgaId];
    CREATE INDEX [IX_Wards_LgaId] ON [Wards] ([LgaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623102029_updated lgas column name'
)
BEGIN
    DROP INDEX [IX_LGAs_StatesStateId] ON [LGAs];
    DECLARE @var34 sysname;
    SELECT @var34 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LGAs]') AND [c].[name] = N'StatesStateId');
    IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [LGAs] DROP CONSTRAINT [' + @var34 + '];');
    EXEC(N'UPDATE [LGAs] SET [StatesStateId] = CAST(0 AS bigint) WHERE [StatesStateId] IS NULL');
    ALTER TABLE [LGAs] ALTER COLUMN [StatesStateId] bigint NOT NULL;
    ALTER TABLE [LGAs] ADD DEFAULT CAST(0 AS bigint) FOR [StatesStateId];
    CREATE INDEX [IX_LGAs_StatesStateId] ON [LGAs] ([StatesStateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623102029_updated lgas column name'
)
BEGIN
    ALTER TABLE [LGAs] ADD CONSTRAINT [FK_LGAs_States_StatesStateId] FOREIGN KEY ([StatesStateId]) REFERENCES [States] ([StateId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623102029_updated lgas column name'
)
BEGIN
    ALTER TABLE [Wards] ADD CONSTRAINT [FK_Wards_LGAs_LgaId] FOREIGN KEY ([LgaId]) REFERENCES [LGAs] ([LgaId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623102029_updated lgas column name'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250623102029_updated lgas column name', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623113622_updated farmer column'
)
BEGIN
    ALTER TABLE [Farmers] ADD [Ward] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623113622_updated farmer column'
)
BEGIN
    ALTER TABLE [Cooperatives] ADD [Ward] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623113622_updated farmer column'
)
BEGIN
    ALTER TABLE [Agents] ADD [Ward] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623113622_updated farmer column'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250623113622_updated farmer column', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250702130705_credit scoring scaffold'
)
BEGIN
    CREATE TABLE [CreditScoreWeights] (
        [Id] bigint NOT NULL IDENTITY,
        [BusinessCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        [RepaymentWeight] bigint NOT NULL,
        [SubcriptionWeight] bigint NOT NULL,
        [TimelinessWeight] bigint NOT NULL,
        CONSTRAINT [PK_CreditScoreWeights] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250702130705_credit scoring scaffold'
)
BEGIN
    CREATE TABLE [FarmerCreditScores] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerId] bigint NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        [Score] bigint NOT NULL,
        [RiskLevel] nvarchar(max) NOT NULL,
        [LastUpdated] datetimeoffset NOT NULL,
        CONSTRAINT [PK_FarmerCreditScores] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250702130705_credit scoring scaffold'
)
BEGIN
    CREATE TABLE [Repayments] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerUserId] nvarchar(max) NOT NULL,
        [InterestedBundleId] bigint NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [CompletedOn] datetimeoffset NULL,
        [PaymentDueDate] datetimeoffset NOT NULL,
        [AmountToRepay] decimal(18,2) NOT NULL,
        [AmountPaid] decimal(18,2) NOT NULL,
        [IsComplete] bit NOT NULL,
        CONSTRAINT [PK_Repayments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250702130705_credit scoring scaffold'
)
BEGIN
    CREATE TABLE [RepaymentBreakdown] (
        [Id] bigint NOT NULL IDENTITY,
        [RepaymentId] bigint NOT NULL,
        [RepaymentMode] nvarchar(max) NOT NULL,
        [AmountOrEquivalent] decimal(18,2) NOT NULL,
        [PaidOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_RepaymentBreakdown] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RepaymentBreakdown_Repayments_RepaymentId] FOREIGN KEY ([RepaymentId]) REFERENCES [Repayments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250702130705_credit scoring scaffold'
)
BEGIN
    CREATE INDEX [IX_RepaymentBreakdown_RepaymentId] ON [RepaymentBreakdown] ([RepaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250702130705_credit scoring scaffold'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250702130705_credit scoring scaffold', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713124724_configured farmer with multiple organization'
)
BEGIN
    EXEC sp_rename N'[AspNetUsers].[OrganizationCode]', N'NIN', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713124724_configured farmer with multiple organization'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [BVN] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713124724_configured farmer with multiple organization'
)
BEGIN
    CREATE TABLE [UserOrganizations] (
        [UserId] nvarchar(450) NOT NULL,
        [OrganizationCode] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserOrganizations] PRIMARY KEY ([UserId], [OrganizationCode]),
        CONSTRAINT [FK_UserOrganizations_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713124724_configured farmer with multiple organization'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250713124724_configured farmer with multiple organization', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [CoordinateOverlapWithFarmerIds] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [FarmUnflaggedByName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [FarmUnflaggedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [FarmWard] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [FlaggedReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [IsFarmFlaggedBySystem] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [UnflagFarm] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    ALTER TABLE [FarmPlotDetails] ADD [UnflaggedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    CREATE TABLE [FarmFlagAuditTrail] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmPlotDetailsId] bigint NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [PerformedByUserId] nvarchar(max) NOT NULL,
        [PerformedByName] nvarchar(max) NOT NULL,
        [PerformedOn] datetimeoffset NOT NULL,
        [Remarks] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_FarmFlagAuditTrail] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FarmFlagAuditTrail_FarmPlotDetails_FarmPlotDetailsId] FOREIGN KEY ([FarmPlotDetailsId]) REFERENCES [FarmPlotDetails] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    CREATE INDEX [IX_FarmFlagAuditTrail_FarmPlotDetailsId] ON [FarmFlagAuditTrail] ([FarmPlotDetailsId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250713155453_farm mapping updated with flags'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250713155453_farm mapping updated with flags', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250714153648_added value chain'
)
BEGIN
    CREATE TABLE [FarmerCategorys] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerCategory] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_FarmerCategorys] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250714153648_added value chain'
)
BEGIN
    CREATE TABLE [ValueChainTypes] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [FarmerCategorysId] bigint NOT NULL,
        CONSTRAINT [PK_ValueChainTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ValueChainTypes_FarmerCategorys_FarmerCategorysId] FOREIGN KEY ([FarmerCategorysId]) REFERENCES [FarmerCategorys] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250714153648_added value chain'
)
BEGIN
    CREATE TABLE [FarmerValueChainItems] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [ValueChainTypeId] bigint NOT NULL,
        CONSTRAINT [PK_FarmerValueChainItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FarmerValueChainItems_ValueChainTypes_ValueChainTypeId] FOREIGN KEY ([ValueChainTypeId]) REFERENCES [ValueChainTypes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250714153648_added value chain'
)
BEGIN
    CREATE INDEX [IX_FarmerValueChainItems_ValueChainTypeId] ON [FarmerValueChainItems] ([ValueChainTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250714153648_added value chain'
)
BEGIN
    CREATE INDEX [IX_ValueChainTypes_FarmerCategorysId] ON [ValueChainTypes] ([FarmerCategorysId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250714153648_added value chain'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250714153648_added value chain', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721092535_updated farmer table to include categorization'
)
BEGIN
    ALTER TABLE [FarmerValueChainItems] DROP CONSTRAINT [FK_FarmerValueChainItems_ValueChainTypes_ValueChainTypeId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721092535_updated farmer table to include categorization'
)
BEGIN
    DROP TABLE [ValueChainTypes];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721092535_updated farmer table to include categorization'
)
BEGIN
    DROP TABLE [FarmerCategorys];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721092535_updated farmer table to include categorization'
)
BEGIN
    EXEC sp_rename N'[FarmerValueChainItems].[ValueChainTypeId]', N'FarmerId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721092535_updated farmer table to include categorization'
)
BEGIN
    EXEC sp_rename N'[FarmerValueChainItems].[Name]', N'ValueChainType', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721092535_updated farmer table to include categorization'
)
BEGIN
    EXEC sp_rename N'[FarmerValueChainItems].[IX_FarmerValueChainItems_ValueChainTypeId]', N'IX_FarmerValueChainItems_FarmerId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721092535_updated farmer table to include categorization'
)
BEGIN
    ALTER TABLE [FarmerValueChainItems] ADD [ItemName] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721092535_updated farmer table to include categorization'
)
BEGIN
    ALTER TABLE [Farmers] ADD [FarmerCategory] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721092535_updated farmer table to include categorization'
)
BEGIN
    ALTER TABLE [FarmerValueChainItems] ADD CONSTRAINT [FK_FarmerValueChainItems_Farmers_FarmerId] FOREIGN KEY ([FarmerId]) REFERENCES [Farmers] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721092535_updated farmer table to include categorization'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250721092535_updated farmer table to include categorization', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721104614_updated farmer table to include education level'
)
BEGIN
    ALTER TABLE [Farmers] ADD [HighestLevelOfEducation] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721104614_updated farmer table to include education level'
)
BEGIN
    ALTER TABLE [Farmers] ADD [YearsInAgric] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721104614_updated farmer table to include education level'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250721104614_updated farmer table to include education level', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721104712_updated farmer table to include education level_'
)
BEGIN
    DECLARE @var35 sysname;
    SELECT @var35 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'YearsInAgric');
    IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var35 + '];');
    EXEC(N'UPDATE [Farmers] SET [YearsInAgric] = CAST(0 AS bigint) WHERE [YearsInAgric] IS NULL');
    ALTER TABLE [Farmers] ALTER COLUMN [YearsInAgric] bigint NOT NULL;
    ALTER TABLE [Farmers] ADD DEFAULT CAST(0 AS bigint) FOR [YearsInAgric];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250721104712_updated farmer table to include education level_'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250721104712_updated farmer table to include education level_', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902130126_added M and E table'
)
BEGIN
    CREATE TABLE [MandEActivityTypes] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(450) NOT NULL,
        [Description] nvarchar(max) NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        CONSTRAINT [PK_MandEActivityTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902130126_added M and E table'
)
BEGIN
    CREATE TABLE [MandEFarmActivities] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerUserId] nvarchar(max) NOT NULL,
        [FarmerName] nvarchar(max) NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        [FarmPlotId] bigint NOT NULL,
        [MandEActivityTypeId] bigint NOT NULL,
        [MandESubActivityId] bigint NOT NULL,
        [ActivityDate] datetimeoffset NOT NULL,
        [DataJson] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [PerformedByUserId] nvarchar(max) NOT NULL,
        [AttachmentUrl] nvarchar(max) NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_MandEFarmActivities] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902130126_added M and E table'
)
BEGIN
    CREATE TABLE [MandEFieldDefinitions] (
        [Id] bigint NOT NULL IDENTITY,
        [MandESubActivityId] bigint NOT NULL,
        [Label] nvarchar(max) NOT NULL,
        [Key] nvarchar(450) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [OptionsJson] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [Order] bigint NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        CONSTRAINT [PK_MandEFieldDefinitions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902130126_added M and E table'
)
BEGIN
    CREATE TABLE [MandESubActivities] (
        [Id] bigint NOT NULL IDENTITY,
        [MandEActivityTypeId] bigint NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Description] nvarchar(max) NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        CONSTRAINT [PK_MandESubActivities] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902130126_added M and E table'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MandEActivityTypes_Name] ON [MandEActivityTypes] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902130126_added M and E table'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MandEFieldDefinitions_MandESubActivityId_Key] ON [MandEFieldDefinitions] ([MandESubActivityId], [Key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902130126_added M and E table'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MandESubActivities_MandEActivityTypeId_Name] ON [MandESubActivities] ([MandEActivityTypeId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902130126_added M and E table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250902130126_added M and E table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002151654_added organization configuration and wallet setup'
)
BEGIN
    CREATE TABLE [OrganizationConfig] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [OrganizationName] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_OrganizationConfig] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002151654_added organization configuration and wallet setup'
)
BEGIN
    CREATE TABLE [Wallets] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [AccountNumber] nvarchar(max) NOT NULL,
        [AccountName] nvarchar(max) NOT NULL,
        [BankName] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [WalletCode] nvarchar(max) NULL,
        [WalletProviderCode] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [OrganizationCode] nvarchar(max) NULL,
        CONSTRAINT [PK_Wallets] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002151654_added organization configuration and wallet setup'
)
BEGIN
    CREATE TABLE [WalletProviderConfiguration] (
        [Id] bigint NOT NULL IDENTITY,
        [DefaultWalletProvider] nvarchar(max) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [EnableMultiWallet] bit NOT NULL,
        [OrganizationConfigurationId] bigint NOT NULL,
        CONSTRAINT [PK_WalletProviderConfiguration] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WalletProviderConfiguration_OrganizationConfig_OrganizationConfigurationId] FOREIGN KEY ([OrganizationConfigurationId]) REFERENCES [OrganizationConfig] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002151654_added organization configuration and wallet setup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WalletProviderConfiguration_OrganizationConfigurationId] ON [WalletProviderConfiguration] ([OrganizationConfigurationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002151654_added organization configuration and wallet setup'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251002151654_added organization configuration and wallet setup', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    EXEC sp_rename N'[BusinessOrganizations].[OrganizaionId]', N'OrganizationId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    ALTER TABLE [OrganizationConfig] ADD [OrganizationId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    DECLARE @var36 sysname;
    SELECT @var36 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmerCreditScores]') AND [c].[name] = N'Score');
    IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [FarmerCreditScores] DROP CONSTRAINT [' + @var36 + '];');
    ALTER TABLE [FarmerCreditScores] ALTER COLUMN [Score] decimal(18,2) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    ALTER TABLE [FarmerCreditScores] ADD [CompletedRepayments] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    ALTER TABLE [FarmerCreditScores] ADD [DefaultedRepayments] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    ALTER TABLE [FarmerCreditScores] ADD [TotalSubscriptions] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [CooperativeCreditScores] (
        [Id] bigint NOT NULL IDENTITY,
        [CooperativeId] bigint NOT NULL,
        [Score] decimal(18,2) NOT NULL,
        [TotalSubscriptions] bigint NOT NULL,
        [CompletedRepayments] bigint NOT NULL,
        [DefaultedRepayments] bigint NOT NULL,
        [LastUpdated] datetime2 NOT NULL,
        CONSTRAINT [PK_CooperativeCreditScores] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [DisbursementConfig] (
        [Id] int NOT NULL IDENTITY,
        [DisbursementMode] nvarchar(max) NOT NULL,
        [DisbursementTarget] nvarchar(max) NOT NULL,
        [RequiresApprovalForDisbursement] bit NOT NULL,
        [UsesInvoiceReference] bit NOT NULL,
        CONSTRAINT [PK_DisbursementConfig] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [OrganizationBundlePlans] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CropType] nvarchar(max) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [MinHectare] decimal(18,2) NOT NULL,
        [MaxHectare] decimal(18,2) NOT NULL,
        [CommitmentFee] decimal(18,2) NOT NULL,
        [OrganizationConfigurationId] bigint NOT NULL,
        CONSTRAINT [PK_OrganizationBundlePlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationBundlePlans_OrganizationConfig_OrganizationConfigurationId] FOREIGN KEY ([OrganizationConfigurationId]) REFERENCES [OrganizationConfig] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [RepaymentConfig] (
        [Id] int NOT NULL IDENTITY,
        [RepaymentMode] nvarchar(max) NOT NULL,
        [ExpectedRepaymentPercentage] decimal(18,2) NULL,
        [RepaymentDurationInMonths] bigint NULL,
        [AllowInstallments] bit NOT NULL,
        [IsRepaymentInPercentage] bit NOT NULL,
        [IsRepaymentStaticFee] bit NOT NULL,
        [RepaymentStaticFee] decimal(18,2) NULL,
        CONSTRAINT [PK_RepaymentConfig] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [SubscriptionConfig] (
        [Id] int NOT NULL IDENTITY,
        [RequiresEquity] bit NOT NULL,
        [EquityType] nvarchar(max) NOT NULL,
        [EquityPercentage] decimal(18,2) NULL,
        [IsEquityInPercentage] bit NOT NULL,
        [IsEquityStaticFee] bit NOT NULL,
        [EquityStaticFee] decimal(18,2) NULL,
        [CooperativeContributes] bit NOT NULL,
        [OrganizationContributes] bit NOT NULL,
        [FinancialPartnerContributes] bit NOT NULL,
        [ContributionSplitJson] nvarchar(max) NOT NULL,
        [AllowFarmerSelfSubscription] bit NOT NULL,
        [AllowCooperativeSubscription] bit NOT NULL,
        CONSTRAINT [PK_SubscriptionConfig] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [OrganizationBundleItem] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationBundlePlansId] bigint NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [UnitPrice] decimal(18,2) NULL,
        CONSTRAINT [PK_OrganizationBundleItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationBundleItem_OrganizationBundlePlans_OrganizationBundlePlansId] FOREIGN KEY ([OrganizationBundlePlansId]) REFERENCES [OrganizationBundlePlans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [OrganizationSubscriptions] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [RequestingForFarmerId] bigint NULL,
        [InitiatedByCooperativeId] bigint NULL,
        [BeneficiaryType] nvarchar(max) NOT NULL,
        [BeneficiaryId] bigint NOT NULL,
        [BundlePlanId] bigint NULL,
        [OrganizationBundlePlansId] bigint NOT NULL,
        [InputDescription] nvarchar(max) NOT NULL,
        [TotalCost] decimal(18,2) NOT NULL,
        [EquityContribution] decimal(18,2) NOT NULL,
        [EquityType] nvarchar(max) NOT NULL,
        [OrganizationContribution] decimal(18,2) NOT NULL,
        [PartnerContribution] decimal(18,2) NOT NULL,
        [SubscriptionInitiatedOn] datetimeoffset NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [SubscriptionApprovedOn] datetimeoffset NOT NULL,
        [SubscriptionApprovedByUserId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_OrganizationSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationSubscriptions_OrganizationBundlePlans_OrganizationBundlePlansId] FOREIGN KEY ([OrganizationBundlePlansId]) REFERENCES [OrganizationBundlePlans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [InputFinancingConfiguration] (
        [Id] bigint NOT NULL IDENTITY,
        [SubscriptionConfigId] int NOT NULL,
        [DisbursementConfigId] int NOT NULL,
        [RepaymentConfigId] int NOT NULL,
        [BundleEnabled] bit NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [IsEnabled] bit NOT NULL,
        [OrganizationConfigurationId] bigint NOT NULL,
        CONSTRAINT [PK_InputFinancingConfiguration] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InputFinancingConfiguration_DisbursementConfig_DisbursementConfigId] FOREIGN KEY ([DisbursementConfigId]) REFERENCES [DisbursementConfig] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InputFinancingConfiguration_OrganizationConfig_OrganizationConfigurationId] FOREIGN KEY ([OrganizationConfigurationId]) REFERENCES [OrganizationConfig] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InputFinancingConfiguration_RepaymentConfig_RepaymentConfigId] FOREIGN KEY ([RepaymentConfigId]) REFERENCES [RepaymentConfig] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InputFinancingConfiguration_SubscriptionConfig_SubscriptionConfigId] FOREIGN KEY ([SubscriptionConfigId]) REFERENCES [SubscriptionConfig] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [Distributions] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [SubscriptionId] bigint NOT NULL,
        [Mode] nvarchar(max) NOT NULL,
        [DistributionReceiverType] nvarchar(max) NOT NULL,
        [DistributionReceiverId] bigint NOT NULL,
        [Amount] decimal(18,2) NULL,
        [InputDetails] nvarchar(max) NOT NULL,
        [ReferenceNumber] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [DistributedOn] datetimeoffset NULL,
        [DistributedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_Distributions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Distributions_OrganizationSubscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [OrganizationSubscriptions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [Invoices] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [SubscriptionId] bigint NOT NULL,
        [InvoiceNumber] nvarchar(64) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [DateIssued] datetimeoffset NOT NULL,
        [IsApproved] bit NOT NULL,
        [ReferenceCode] nvarchar(64) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CollectedOn] datetimeoffset NULL,
        [CollectedByUserId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_OrganizationSubscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [OrganizationSubscriptions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [OrganizationRepayment] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [DistributionId] bigint NOT NULL,
        [PaidByType] nvarchar(max) NOT NULL,
        [PaidById] nvarchar(max) NOT NULL,
        [Mode] nvarchar(max) NOT NULL,
        [AmountPaid] decimal(18,2) NULL,
        [ProduceDetails] nvarchar(max) NOT NULL,
        [PaymentDate] datetimeoffset NOT NULL,
        [Status] int NOT NULL,
        CONSTRAINT [PK_OrganizationRepayment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationRepayment_Distributions_DistributionId] FOREIGN KEY ([DistributionId]) REFERENCES [Distributions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [InvoiceItems] (
        [Id] bigint NOT NULL IDENTITY,
        [ItemName] nvarchar(max) NOT NULL,
        [ItemDescription] nvarchar(max) NOT NULL,
        [ItemQuantity] bigint NOT NULL,
        [InvoiceId] bigint NOT NULL,
        CONSTRAINT [PK_InvoiceItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoiceItems_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE TABLE [Receipts] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationRepaymentId] bigint NOT NULL,
        [ReceiptNumber] nvarchar(max) NOT NULL,
        [AmountAcknowledged] decimal(18,2) NOT NULL,
        [Mode] nvarchar(max) NOT NULL,
        [IssuedOn] datetimeoffset NOT NULL,
        [Status] int NOT NULL,
        CONSTRAINT [PK_Receipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Receipts_OrganizationRepayment_OrganizationRepaymentId] FOREIGN KEY ([OrganizationRepaymentId]) REFERENCES [OrganizationRepayment] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE INDEX [IX_OrganizationConfig_OrganizationId] ON [OrganizationConfig] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Distributions_ReferenceNumber] ON [Distributions] ([ReferenceNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE INDEX [IX_Distributions_SubscriptionId] ON [Distributions] ([SubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE INDEX [IX_InputFinancingConfiguration_DisbursementConfigId] ON [InputFinancingConfiguration] ([DisbursementConfigId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InputFinancingConfiguration_OrganizationConfigurationId] ON [InputFinancingConfiguration] ([OrganizationConfigurationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE INDEX [IX_InputFinancingConfiguration_RepaymentConfigId] ON [InputFinancingConfiguration] ([RepaymentConfigId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE INDEX [IX_InputFinancingConfiguration_SubscriptionConfigId] ON [InputFinancingConfiguration] ([SubscriptionConfigId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE INDEX [IX_InvoiceItems_InvoiceId] ON [InvoiceItems] ([InvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_ReferenceCode] ON [Invoices] ([ReferenceCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_SubscriptionId] ON [Invoices] ([SubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE INDEX [IX_OrganizationBundleItem_OrganizationBundlePlansId] ON [OrganizationBundleItem] ([OrganizationBundlePlansId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE INDEX [IX_OrganizationBundlePlans_OrganizationConfigurationId] ON [OrganizationBundlePlans] ([OrganizationConfigurationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE INDEX [IX_OrganizationRepayment_DistributionId] ON [OrganizationRepayment] ([DistributionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE INDEX [IX_OrganizationSubscriptions_OrganizationBundlePlansId] ON [OrganizationSubscriptions] ([OrganizationBundlePlansId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Receipts_OrganizationRepaymentId] ON [Receipts] ([OrganizationRepaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    ALTER TABLE [OrganizationConfig] ADD CONSTRAINT [FK_OrganizationConfig_BusinessOrganizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [BusinessOrganizations] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007121641_updatedorganizationconfiguration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251007121641_updatedorganizationconfiguration', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007125247_nullableconfig'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251007125247_nullableconfig', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    ALTER TABLE [InputFinancingConfiguration] DROP CONSTRAINT [FK_InputFinancingConfiguration_DisbursementConfig_DisbursementConfigId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    ALTER TABLE [InputFinancingConfiguration] DROP CONSTRAINT [FK_InputFinancingConfiguration_RepaymentConfig_RepaymentConfigId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    ALTER TABLE [InputFinancingConfiguration] DROP CONSTRAINT [FK_InputFinancingConfiguration_SubscriptionConfig_SubscriptionConfigId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    DROP INDEX [IX_WalletProviderConfiguration_OrganizationConfigurationId] ON [WalletProviderConfiguration];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    DROP INDEX [IX_InputFinancingConfiguration_OrganizationConfigurationId] ON [InputFinancingConfiguration];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    EXEC sp_rename N'[InputFinancingConfiguration].[SubscriptionConfigId]', N'SubscriptionConfigsId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    EXEC sp_rename N'[InputFinancingConfiguration].[RepaymentConfigId]', N'RepaymentConfigsId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    EXEC sp_rename N'[InputFinancingConfiguration].[DisbursementConfigId]', N'DisbursementConfigsId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    EXEC sp_rename N'[InputFinancingConfiguration].[IX_InputFinancingConfiguration_SubscriptionConfigId]', N'IX_InputFinancingConfiguration_SubscriptionConfigsId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    EXEC sp_rename N'[InputFinancingConfiguration].[IX_InputFinancingConfiguration_RepaymentConfigId]', N'IX_InputFinancingConfiguration_RepaymentConfigsId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    EXEC sp_rename N'[InputFinancingConfiguration].[IX_InputFinancingConfiguration_DisbursementConfigId]', N'IX_InputFinancingConfiguration_DisbursementConfigsId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    CREATE INDEX [IX_WalletProviderConfiguration_OrganizationConfigurationId] ON [WalletProviderConfiguration] ([OrganizationConfigurationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    CREATE INDEX [IX_InputFinancingConfiguration_OrganizationConfigurationId] ON [InputFinancingConfiguration] ([OrganizationConfigurationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    ALTER TABLE [InputFinancingConfiguration] ADD CONSTRAINT [FK_InputFinancingConfiguration_DisbursementConfig_DisbursementConfigsId] FOREIGN KEY ([DisbursementConfigsId]) REFERENCES [DisbursementConfig] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    ALTER TABLE [InputFinancingConfiguration] ADD CONSTRAINT [FK_InputFinancingConfiguration_RepaymentConfig_RepaymentConfigsId] FOREIGN KEY ([RepaymentConfigsId]) REFERENCES [RepaymentConfig] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    ALTER TABLE [InputFinancingConfiguration] ADD CONSTRAINT [FK_InputFinancingConfiguration_SubscriptionConfig_SubscriptionConfigsId] FOREIGN KEY ([SubscriptionConfigsId]) REFERENCES [SubscriptionConfig] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007145248_updatedconfigurationtable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251007145248_updatedconfigurationtable', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007151502_updatedconfigurationtablenull'
)
BEGIN
    DECLARE @var37 sysname;
    SELECT @var37 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SubscriptionConfig]') AND [c].[name] = N'ContributionSplitJson');
    IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [SubscriptionConfig] DROP CONSTRAINT [' + @var37 + '];');
    ALTER TABLE [SubscriptionConfig] ALTER COLUMN [ContributionSplitJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007151502_updatedconfigurationtablenull'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251007151502_updatedconfigurationtablenull', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008155855_updatedisdefaultwallet'
)
BEGIN
    EXEC sp_rename N'[WalletProviderConfiguration].[EnableMultiWallet]', N'IsDefault', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008155855_updatedisdefaultwallet'
)
BEGIN
    DECLARE @var38 sysname;
    SELECT @var38 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WalletProviderConfiguration]') AND [c].[name] = N'DefaultWalletProvider');
    IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [WalletProviderConfiguration] DROP CONSTRAINT [' + @var38 + '];');
    ALTER TABLE [WalletProviderConfiguration] ALTER COLUMN [DefaultWalletProvider] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008155855_updatedisdefaultwallet'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251008155855_updatedisdefaultwallet', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    ALTER TABLE [Distributions] DROP CONSTRAINT [FK_Distributions_OrganizationSubscriptions_SubscriptionId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    ALTER TABLE [Invoices] DROP CONSTRAINT [FK_Invoices_OrganizationSubscriptions_SubscriptionId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    ALTER TABLE [Receipts] DROP CONSTRAINT [FK_Receipts_OrganizationRepayment_OrganizationRepaymentId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    DROP TABLE [OrganizationRepayment];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    DROP TABLE [OrganizationSubscriptions];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    EXEC sp_rename N'[Receipts].[OrganizationRepaymentId]', N'SubscriptionRepaymentsId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    EXEC sp_rename N'[Receipts].[IX_Receipts_OrganizationRepaymentId]', N'IX_Receipts_SubscriptionRepaymentsId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    EXEC sp_rename N'[Invoices].[SubscriptionId]', N'SubscriptionsId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    EXEC sp_rename N'[Invoices].[IX_Invoices_SubscriptionId]', N'IX_Invoices_SubscriptionsId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    ALTER TABLE [InputFinancingConfiguration] ADD [ConfigurationCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    ALTER TABLE [InputFinancingConfiguration] ADD [Name] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    CREATE TABLE [InputFinancingSchemes] (
        [Id] bigint NOT NULL IDENTITY,
        [ConfigurationCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [SchemeName] nvarchar(max) NOT NULL,
        [SchemeCode] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Commodity] nvarchar(max) NOT NULL,
        [BeneficiaryType] nvarchar(max) NOT NULL,
        [TotalArea] decimal(18,2) NOT NULL,
        [TotalValue] decimal(18,2) NOT NULL,
        [FundingSource] nvarchar(max) NOT NULL,
        [Season] nvarchar(max) NOT NULL,
        [WarehouseId] nvarchar(max) NOT NULL,
        [InputFinancingConfigurationId] bigint NOT NULL,
        [IsActive] bit NOT NULL,
        [UnitValuePerHectare] decimal(18,2) NOT NULL,
        [RepaymentStartDate] datetimeoffset NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [RepaymentEndDate] datetimeoffset NOT NULL,
        [SubscriptionOpenDate] datetimeoffset NOT NULL,
        [SubscriptionCloseDate] datetimeoffset NOT NULL,
        CONSTRAINT [PK_InputFinancingSchemes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    CREATE TABLE [SubscriptionRepayments] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [DistributionsId] bigint NOT NULL,
        [PaidByType] nvarchar(max) NOT NULL,
        [PaidById] nvarchar(max) NOT NULL,
        [Mode] nvarchar(max) NOT NULL,
        [AmountPaid] decimal(18,2) NULL,
        [ProduceDetails] nvarchar(max) NOT NULL,
        [PaymentDate] datetimeoffset NOT NULL,
        [Status] int NOT NULL,
        CONSTRAINT [PK_SubscriptionRepayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubscriptionRepayments_Distributions_DistributionsId] FOREIGN KEY ([DistributionsId]) REFERENCES [Distributions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    CREATE TABLE [Subscriptions] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [RequestingForFarmerId] bigint NULL,
        [InitiatedByCooperativeId] bigint NULL,
        [BeneficiaryType] nvarchar(max) NOT NULL,
        [BeneficiaryId] bigint NOT NULL,
        [BundlePlanId] bigint NULL,
        [OrganizationBundlePlansId] bigint NOT NULL,
        [InputDescription] nvarchar(max) NOT NULL,
        [TotalCost] decimal(18,2) NOT NULL,
        [EquityContribution] decimal(18,2) NOT NULL,
        [EquityType] nvarchar(max) NOT NULL,
        [OrganizationContribution] decimal(18,2) NOT NULL,
        [PartnerContribution] decimal(18,2) NOT NULL,
        [SubscriptionInitiatedOn] datetimeoffset NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [SubscriptionApprovedOn] datetimeoffset NOT NULL,
        [SubscriptionApprovedByUserId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Subscriptions_OrganizationBundlePlans_OrganizationBundlePlansId] FOREIGN KEY ([OrganizationBundlePlansId]) REFERENCES [OrganizationBundlePlans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    CREATE TABLE [SchemeTarget] (
        [Id] bigint NOT NULL IDENTITY,
        [InputFinancingSchemeId] bigint NOT NULL,
        [TargetType] nvarchar(max) NOT NULL,
        [TargetId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_SchemeTarget] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SchemeTarget_InputFinancingSchemes_InputFinancingSchemeId] FOREIGN KEY ([InputFinancingSchemeId]) REFERENCES [InputFinancingSchemes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    CREATE INDEX [IX_SchemeTarget_InputFinancingSchemeId] ON [SchemeTarget] ([InputFinancingSchemeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    CREATE INDEX [IX_SubscriptionRepayments_DistributionsId] ON [SubscriptionRepayments] ([DistributionsId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_OrganizationBundlePlansId] ON [Subscriptions] ([OrganizationBundlePlansId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    ALTER TABLE [Distributions] ADD CONSTRAINT [FK_Distributions_Subscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [Subscriptions] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    ALTER TABLE [Invoices] ADD CONSTRAINT [FK_Invoices_Subscriptions_SubscriptionsId] FOREIGN KEY ([SubscriptionsId]) REFERENCES [Subscriptions] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    ALTER TABLE [Receipts] ADD CONSTRAINT [FK_Receipts_SubscriptionRepayments_SubscriptionRepaymentsId] FOREIGN KEY ([SubscriptionRepaymentsId]) REFERENCES [SubscriptionRepayments] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015155616_input financing scheme'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251015155616_input financing scheme', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016131453_removed target group'
)
BEGIN
    DROP TABLE [SchemeTarget];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016131453_removed target group'
)
BEGIN
    EXEC sp_rename N'[InputFinancingSchemes].[ConfigurationCode]', N'InputFinancingConfigurationCode', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016131453_removed target group'
)
BEGIN
    DECLARE @var39 sysname;
    SELECT @var39 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputFinancingSchemes]') AND [c].[name] = N'WarehouseId');
    IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [InputFinancingSchemes] DROP CONSTRAINT [' + @var39 + '];');
    ALTER TABLE [InputFinancingSchemes] ALTER COLUMN [WarehouseId] bigint NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016131453_removed target group'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251016131453_removed target group', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023143130_organization level input subscription'
)
BEGIN
    ALTER TABLE [Wallets] ADD [WalletBalance] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023143130_organization level input subscription'
)
BEGIN
    DECLARE @var40 sysname;
    SELECT @var40 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SubscriptionConfig]') AND [c].[name] = N'EquityStaticFee');
    IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [SubscriptionConfig] DROP CONSTRAINT [' + @var40 + '];');
    EXEC(N'UPDATE [SubscriptionConfig] SET [EquityStaticFee] = 0.0 WHERE [EquityStaticFee] IS NULL');
    ALTER TABLE [SubscriptionConfig] ALTER COLUMN [EquityStaticFee] decimal(18,2) NOT NULL;
    ALTER TABLE [SubscriptionConfig] ADD DEFAULT 0.0 FOR [EquityStaticFee];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023143130_organization level input subscription'
)
BEGIN
    DECLARE @var41 sysname;
    SELECT @var41 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RepaymentConfig]') AND [c].[name] = N'RepaymentStaticFee');
    IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [RepaymentConfig] DROP CONSTRAINT [' + @var41 + '];');
    EXEC(N'UPDATE [RepaymentConfig] SET [RepaymentStaticFee] = 0.0 WHERE [RepaymentStaticFee] IS NULL');
    ALTER TABLE [RepaymentConfig] ALTER COLUMN [RepaymentStaticFee] decimal(18,2) NOT NULL;
    ALTER TABLE [RepaymentConfig] ADD DEFAULT 0.0 FOR [RepaymentStaticFee];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023143130_organization level input subscription'
)
BEGIN
    DECLARE @var42 sysname;
    SELECT @var42 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RepaymentConfig]') AND [c].[name] = N'RepaymentDurationInMonths');
    IF @var42 IS NOT NULL EXEC(N'ALTER TABLE [RepaymentConfig] DROP CONSTRAINT [' + @var42 + '];');
    EXEC(N'UPDATE [RepaymentConfig] SET [RepaymentDurationInMonths] = CAST(0 AS bigint) WHERE [RepaymentDurationInMonths] IS NULL');
    ALTER TABLE [RepaymentConfig] ALTER COLUMN [RepaymentDurationInMonths] bigint NOT NULL;
    ALTER TABLE [RepaymentConfig] ADD DEFAULT CAST(0 AS bigint) FOR [RepaymentDurationInMonths];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023143130_organization level input subscription'
)
BEGIN
    CREATE TABLE [InputSubscriptions] (
        [Id] bigint NOT NULL IDENTITY,
        [InputSchemeId] bigint NOT NULL,
        [ConfigurationCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [SubscribedByUserId] nvarchar(max) NOT NULL,
        [SubscriberType] nvarchar(max) NOT NULL,
        [CooperativeId] bigint NOT NULL,
        [EquityAmount] decimal(18,2) NOT NULL,
        [RepaymentAmount] decimal(18,2) NOT NULL,
        [RepaymentStartDate] datetimeoffset NOT NULL,
        [RepaymentEndDate] datetimeoffset NOT NULL,
        [PaymentProofUrl] nvarchar(max) NULL,
        [TransactionRef] nvarchar(max) NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [VerifiedBy] nvarchar(max) NULL,
        [ApprovedBy] nvarchar(max) NULL,
        [ApprovedOn] datetimeoffset NOT NULL,
        [VerifiedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_InputSubscriptions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023143130_organization level input subscription'
)
BEGIN
    CREATE TABLE [WalletTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [WalletId] bigint NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TransactionType] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Reference] nvarchar(max) NOT NULL,
        [BalanceAfter] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WalletTransactions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023143130_organization level input subscription'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251023143130_organization level input subscription', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251103151649_added shouldverify nin in subaccount table'
)
BEGIN
    ALTER TABLE [SubAccounts] ADD [IsNinVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251103151649_added shouldverify nin in subaccount table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251103151649_added shouldverify nin in subaccount table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    DECLARE @var43 sysname;
    SELECT @var43 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'State');
    IF @var43 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var43 + '];');
    ALTER TABLE [Farmers] ALTER COLUMN [State] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    DECLARE @var44 sysname;
    SELECT @var44 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'Location');
    IF @var44 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var44 + '];');
    ALTER TABLE [Farmers] ALTER COLUMN [Location] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    DECLARE @var45 sysname;
    SELECT @var45 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'Lga');
    IF @var45 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var45 + '];');
    ALTER TABLE [Farmers] ALTER COLUMN [Lga] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    CREATE TABLE [Farms] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [AreaSize] float NOT NULL,
        [LocationDescription] nvarchar(max) NOT NULL,
        [OwnershipType] nvarchar(max) NOT NULL,
        [FarmerUserId] nvarchar(max) NULL,
        [CooperativeId] bigint NOT NULL,
        [CommunityId] bigint NOT NULL,
        [MappedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [FarmLocation] nvarchar(max) NOT NULL,
        [FarmState] nvarchar(max) NOT NULL,
        [FarmLga] nvarchar(max) NOT NULL,
        [FarmWard] nvarchar(max) NOT NULL,
        [CoordinateTakenBy] nvarchar(max) NOT NULL,
        [CoordinateTakenByUserId] nvarchar(max) NOT NULL,
        [AreaInDegress] float NOT NULL,
        [AreaInSqMetre] float NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [FlaggedOn] datetimeoffset NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        [IsFarmFlaggedBySystem] bit NOT NULL,
        [FlaggedReason] nvarchar(max) NULL,
        [CoordinateOverlapWithFarmerIds] nvarchar(max) NULL,
        [UnflagFarm] bit NOT NULL,
        [FarmUnflaggedByUserId] nvarchar(max) NULL,
        [FarmUnflaggedByName] nvarchar(max) NULL,
        [UnflaggedOn] datetimeoffset NULL,
        CONSTRAINT [PK_Farms] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    CREATE TABLE [FarmAllocations] (
        [Id] bigint NOT NULL IDENTITY,
        [CooperativeId] bigint NOT NULL,
        [FarmerUserid] nvarchar(max) NULL,
        [AllocatedToType] nvarchar(max) NOT NULL,
        [AllocatedArea] float NOT NULL,
        [Season] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [AssignedAt] datetimeoffset NOT NULL,
        [FarmId] bigint NOT NULL,
        CONSTRAINT [PK_FarmAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FarmAllocations_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    CREATE TABLE [FarmCoordinate] (
        [Id] bigint NOT NULL IDENTITY,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [Order] bigint NOT NULL,
        [FarmId] bigint NOT NULL,
        CONSTRAINT [PK_FarmCoordinate] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FarmCoordinate_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    CREATE TABLE [FarmMappingAuditTrails] (
        [Id] bigint NOT NULL IDENTITY,
        [Action] nvarchar(max) NOT NULL,
        [PerformedByUserId] nvarchar(max) NOT NULL,
        [PerformedByName] nvarchar(max) NOT NULL,
        [PerformedOn] datetimeoffset NOT NULL,
        [Remarks] nvarchar(max) NOT NULL,
        [FarmId] bigint NOT NULL,
        CONSTRAINT [PK_FarmMappingAuditTrails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FarmMappingAuditTrails_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    CREATE INDEX [IX_FarmAllocations_FarmId] ON [FarmAllocations] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    CREATE INDEX [IX_FarmCoordinate_FarmId] ON [FarmCoordinate] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    CREATE INDEX [IX_FarmMappingAuditTrails_FarmId] ON [FarmMappingAuditTrails] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111191019_made state details nullable on farmer table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251111191019_made state details nullable on farmer table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112120332_added farmer organization'
)
BEGIN
    DECLARE @var46 sysname;
    SELECT @var46 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'OrganizationCode');
    IF @var46 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var46 + '];');
    ALTER TABLE [Farmers] DROP COLUMN [OrganizationCode];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112120332_added farmer organization'
)
BEGIN
    CREATE TABLE [FarmerOrganizations] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerId] bigint NOT NULL,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [OnBoardedOn] datetimeoffset NOT NULL,
        [OnboardedByUserId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_FarmerOrganizations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112120332_added farmer organization'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112120332_added farmer organization', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112123634_added farmer organization nullablecolumn'
)
BEGIN
    DECLARE @var47 sysname;
    SELECT @var47 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmerOrganizations]') AND [c].[name] = N'OnboardedByUserId');
    IF @var47 IS NOT NULL EXEC(N'ALTER TABLE [FarmerOrganizations] DROP CONSTRAINT [' + @var47 + '];');
    ALTER TABLE [FarmerOrganizations] ALTER COLUMN [OnboardedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112123634_added farmer organization nullablecolumn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112123634_added farmer organization nullablecolumn', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120144129_dropped unique columns on m and e table'
)
BEGIN
    DROP INDEX [IX_MandEActivityTypes_Name] ON [MandEActivityTypes];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120144129_dropped unique columns on m and e table'
)
BEGIN
    DECLARE @var48 sysname;
    SELECT @var48 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MandEActivityTypes]') AND [c].[name] = N'Name');
    IF @var48 IS NOT NULL EXEC(N'ALTER TABLE [MandEActivityTypes] DROP CONSTRAINT [' + @var48 + '];');
    ALTER TABLE [MandEActivityTypes] ALTER COLUMN [Name] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120144129_dropped unique columns on m and e table'
)
BEGIN
    DECLARE @var49 sysname;
    SELECT @var49 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'FlaggedOn');
    IF @var49 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var49 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [FlaggedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120144129_dropped unique columns on m and e table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251120144129_dropped unique columns on m and e table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114081756_added for M and E using dataset'
)
BEGIN
    DECLARE @var50 sysname;
    SELECT @var50 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MandEFieldDefinitions]') AND [c].[name] = N'BelongToBusinessCode');
    IF @var50 IS NOT NULL EXEC(N'ALTER TABLE [MandEFieldDefinitions] DROP CONSTRAINT [' + @var50 + '];');
    ALTER TABLE [MandEFieldDefinitions] ALTER COLUMN [BelongToBusinessCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114081756_added for M and E using dataset'
)
BEGIN
    CREATE TABLE [MandEDatasets] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        [OwnerId] nvarchar(max) NULL,
        [MandEOwnerType] int NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_MandEDatasets] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114081756_added for M and E using dataset'
)
BEGIN
    CREATE TABLE [MandEChartDefinitions] (
        [Id] bigint NOT NULL IDENTITY,
        [MandEDatasetId] bigint NOT NULL,
        [ChartType] nvarchar(max) NOT NULL,
        [XKey] nvarchar(max) NOT NULL,
        [YKey] nvarchar(max) NOT NULL,
        [GroupByKey] nvarchar(max) NULL,
        CONSTRAINT [PK_MandEChartDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MandEChartDefinitions_MandEDatasets_MandEDatasetId] FOREIGN KEY ([MandEDatasetId]) REFERENCES [MandEDatasets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114081756_added for M and E using dataset'
)
BEGIN
    CREATE TABLE [MandEDataFieldDefinitions] (
        [Id] bigint NOT NULL IDENTITY,
        [MandEDatasetId] bigint NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Key] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [IsComputed] bit NOT NULL,
        [Formula] nvarchar(max) NULL,
        [Order] bigint NOT NULL,
        [Label] nvarchar(max) NOT NULL,
        [IsRequired] bit NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NULL,
        [OrganizationCode] nvarchar(max) NULL,
        CONSTRAINT [PK_MandEDataFieldDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MandEDataFieldDefinitions_MandEDatasets_MandEDatasetId] FOREIGN KEY ([MandEDatasetId]) REFERENCES [MandEDatasets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114081756_added for M and E using dataset'
)
BEGIN
    CREATE TABLE [MandERecords] (
        [Id] bigint NOT NULL IDENTITY,
        [MandEDatasetId] bigint NOT NULL,
        [DataJson] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [CreatedByUserId] nvarchar(max) NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        CONSTRAINT [PK_MandERecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MandERecords_MandEDatasets_MandEDatasetId] FOREIGN KEY ([MandEDatasetId]) REFERENCES [MandEDatasets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114081756_added for M and E using dataset'
)
BEGIN
    CREATE INDEX [IX_MandEChartDefinitions_MandEDatasetId] ON [MandEChartDefinitions] ([MandEDatasetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114081756_added for M and E using dataset'
)
BEGIN
    CREATE INDEX [IX_MandEDataFieldDefinitions_MandEDatasetId] ON [MandEDataFieldDefinitions] ([MandEDatasetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114081756_added for M and E using dataset'
)
BEGIN
    CREATE INDEX [IX_MandERecords_MandEDatasetId] ON [MandERecords] ([MandEDatasetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114081756_added for M and E using dataset'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260114081756_added for M and E using dataset', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260116134318_added option field to fields'
)
BEGIN
    ALTER TABLE [MandEDataFieldDefinitions] ADD [Options] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260116134318_added option field to fields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260116134318_added option field to fields', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260116154242_updated mandedataset'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260116154242_updated mandedataset', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    ALTER TABLE [MandERecords] ADD [UpdatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    ALTER TABLE [MandERecords] ADD [UpdatedOn] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var51 sysname;
    SELECT @var51 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'Name');
    IF @var51 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var51 + '];');
    ALTER TABLE [Farmers] ALTER COLUMN [Name] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var52 sysname;
    SELECT @var52 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'FarmerCategory');
    IF @var52 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var52 + '];');
    ALTER TABLE [Farmers] ALTER COLUMN [FarmerCategory] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var53 sysname;
    SELECT @var53 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cooperatives]') AND [c].[name] = N'State');
    IF @var53 IS NOT NULL EXEC(N'ALTER TABLE [Cooperatives] DROP CONSTRAINT [' + @var53 + '];');
    ALTER TABLE [Cooperatives] ALTER COLUMN [State] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var54 sysname;
    SELECT @var54 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cooperatives]') AND [c].[name] = N'PhoneNumber');
    IF @var54 IS NOT NULL EXEC(N'ALTER TABLE [Cooperatives] DROP CONSTRAINT [' + @var54 + '];');
    ALTER TABLE [Cooperatives] ALTER COLUMN [PhoneNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var55 sysname;
    SELECT @var55 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cooperatives]') AND [c].[name] = N'Name');
    IF @var55 IS NOT NULL EXEC(N'ALTER TABLE [Cooperatives] DROP CONSTRAINT [' + @var55 + '];');
    ALTER TABLE [Cooperatives] ALTER COLUMN [Name] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var56 sysname;
    SELECT @var56 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cooperatives]') AND [c].[name] = N'Location');
    IF @var56 IS NOT NULL EXEC(N'ALTER TABLE [Cooperatives] DROP CONSTRAINT [' + @var56 + '];');
    ALTER TABLE [Cooperatives] ALTER COLUMN [Location] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var57 sysname;
    SELECT @var57 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cooperatives]') AND [c].[name] = N'Lga');
    IF @var57 IS NOT NULL EXEC(N'ALTER TABLE [Cooperatives] DROP CONSTRAINT [' + @var57 + '];');
    ALTER TABLE [Cooperatives] ALTER COLUMN [Lga] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var58 sysname;
    SELECT @var58 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cooperatives]') AND [c].[name] = N'IsAssignedToAgentId');
    IF @var58 IS NOT NULL EXEC(N'ALTER TABLE [Cooperatives] DROP CONSTRAINT [' + @var58 + '];');
    ALTER TABLE [Cooperatives] ALTER COLUMN [IsAssignedToAgentId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var59 sysname;
    SELECT @var59 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cooperatives]') AND [c].[name] = N'ContactPerson');
    IF @var59 IS NOT NULL EXEC(N'ALTER TABLE [Cooperatives] DROP CONSTRAINT [' + @var59 + '];');
    ALTER TABLE [Cooperatives] ALTER COLUMN [ContactPerson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var60 sysname;
    SELECT @var60 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'Name');
    IF @var60 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var60 + '];');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [Name] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsPhoneVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [UserLevel] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var61 sysname;
    SELECT @var61 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agents]') AND [c].[name] = N'State');
    IF @var61 IS NOT NULL EXEC(N'ALTER TABLE [Agents] DROP CONSTRAINT [' + @var61 + '];');
    ALTER TABLE [Agents] ALTER COLUMN [State] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var62 sysname;
    SELECT @var62 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agents]') AND [c].[name] = N'PhoneNumber');
    IF @var62 IS NOT NULL EXEC(N'ALTER TABLE [Agents] DROP CONSTRAINT [' + @var62 + '];');
    ALTER TABLE [Agents] ALTER COLUMN [PhoneNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var63 sysname;
    SELECT @var63 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agents]') AND [c].[name] = N'Name');
    IF @var63 IS NOT NULL EXEC(N'ALTER TABLE [Agents] DROP CONSTRAINT [' + @var63 + '];');
    ALTER TABLE [Agents] ALTER COLUMN [Name] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var64 sysname;
    SELECT @var64 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agents]') AND [c].[name] = N'Location');
    IF @var64 IS NOT NULL EXEC(N'ALTER TABLE [Agents] DROP CONSTRAINT [' + @var64 + '];');
    ALTER TABLE [Agents] ALTER COLUMN [Location] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var65 sysname;
    SELECT @var65 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agents]') AND [c].[name] = N'Lga');
    IF @var65 IS NOT NULL EXEC(N'ALTER TABLE [Agents] DROP CONSTRAINT [' + @var65 + '];');
    ALTER TABLE [Agents] ALTER COLUMN [Lga] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    DECLARE @var66 sysname;
    SELECT @var66 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agents]') AND [c].[name] = N'BelongToBusinessCode');
    IF @var66 IS NOT NULL EXEC(N'ALTER TABLE [Agents] DROP CONSTRAINT [' + @var66 + '];');
    ALTER TABLE [Agents] ALTER COLUMN [BelongToBusinessCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119100707_updated tables with nullables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119100707_updated tables with nullables', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119133514_added otp to service'
)
BEGIN
    DECLARE @var67 sysname;
    SELECT @var67 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SubAccounts]') AND [c].[name] = N'SubAccountType');
    IF @var67 IS NOT NULL EXEC(N'ALTER TABLE [SubAccounts] DROP CONSTRAINT [' + @var67 + '];');
    ALTER TABLE [SubAccounts] ALTER COLUMN [SubAccountType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119133514_added otp to service'
)
BEGIN
    DECLARE @var68 sysname;
    SELECT @var68 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SubAccounts]') AND [c].[name] = N'Status');
    IF @var68 IS NOT NULL EXEC(N'ALTER TABLE [SubAccounts] DROP CONSTRAINT [' + @var68 + '];');
    ALTER TABLE [SubAccounts] ALTER COLUMN [Status] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119133514_added otp to service'
)
BEGIN
    CREATE TABLE [OneTimePasswords] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [CodeHash] nvarchar(max) NOT NULL,
        [Target] int NOT NULL,
        [Destination] nvarchar(450) NOT NULL,
        [Purpose] int NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        [AttemptCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OneTimePasswords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OneTimePasswords_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119133514_added otp to service'
)
BEGIN
    CREATE INDEX [IX_OneTimePasswords_Destination] ON [OneTimePasswords] ([Destination]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119133514_added otp to service'
)
BEGIN
    CREATE INDEX [IX_OneTimePasswords_ExpiresAt] ON [OneTimePasswords] ([ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119133514_added otp to service'
)
BEGIN
    CREATE INDEX [IX_OneTimePasswords_UserId_Purpose_Target] ON [OneTimePasswords] ([UserId], [Purpose], [Target]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119133514_added otp to service'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119133514_added otp to service', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119211106_updated farmer table'
)
BEGIN
    DECLARE @var69 sysname;
    SELECT @var69 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'PhoneNumber');
    IF @var69 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var69 + '];');
    ALTER TABLE [Farmers] ALTER COLUMN [PhoneNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119211106_updated farmer table'
)
BEGIN
    DECLARE @var70 sysname;
    SELECT @var70 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'OnboardedByUserId');
    IF @var70 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var70 + '];');
    ALTER TABLE [Farmers] ALTER COLUMN [OnboardedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119211106_updated farmer table'
)
BEGIN
    ALTER TABLE [Cooperatives] ADD [OnboardedById] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119211106_updated farmer table'
)
BEGIN
    ALTER TABLE [Agents] ADD [OnboardedById] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119211106_updated farmer table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119211106_updated farmer table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121131401_update phone number variable'
)
BEGIN
    CREATE TABLE [UserDevices] (
        [Id] nvarchar(450) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [DeviceId] nvarchar(max) NOT NULL,
        [DeviceName] nvarchar(max) NOT NULL,
        [IPAddress] nvarchar(max) NOT NULL,
        [LastSeen] datetimeoffset NOT NULL,
        CONSTRAINT [PK_UserDevices] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121131401_update phone number variable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260121131401_update phone number variable', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121132030_nullable phone added'
)
BEGIN
    DECLARE @var71 sysname;
    SELECT @var71 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'PhoneNumber');
    IF @var71 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var71 + '];');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [PhoneNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121132030_nullable phone added'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260121132030_nullable phone added', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260123125417_updated farmer table byyy'
)
BEGIN
    DECLARE @var72 sysname;
    SELECT @var72 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'BelongToCooperativeId');
    IF @var72 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var72 + '];');
    ALTER TABLE [Farmers] DROP COLUMN [BelongToCooperativeId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260123125417_updated farmer table byyy'
)
BEGIN
    CREATE TABLE [FarmerAgents] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerId] bigint NOT NULL,
        [AgentId] bigint NOT NULL,
        [AssignedOn] datetimeoffset NOT NULL,
        [AssignedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_FarmerAgents] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260123125417_updated farmer table byyy'
)
BEGIN
    CREATE TABLE [FarmerCooperatives] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerId] bigint NOT NULL,
        [CooperativeId] bigint NOT NULL,
        [JoinedOn] datetimeoffset NOT NULL,
        [OnboardedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_FarmerCooperatives] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260123125417_updated farmer table byyy'
)
BEGIN
    CREATE TABLE [KYCDetails] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [LoggedOn] datetimeoffset NOT NULL,
        [VerifiedBy] nvarchar(max) NULL,
        [Birthcountry] nvarchar(max) NULL,
        [Birthlga] nvarchar(max) NULL,
        [Birthstate] nvarchar(max) NULL,
        [Birthdate] nvarchar(max) NULL,
        [Gender] nvarchar(max) NOT NULL,
        [Maidenname] nvarchar(max) NULL,
        [Middlename] nvarchar(max) NOT NULL,
        [Nin] nvarchar(max) NOT NULL,
        [Firstname] nvarchar(max) NOT NULL,
        [Surname] nvarchar(max) NOT NULL,
        [Phonenumber] nvarchar(max) NOT NULL,
        [Residentialaddress] nvarchar(max) NULL,
        [Photourl] nvarchar(max) NULL,
        CONSTRAINT [PK_KYCDetails] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260123125417_updated farmer table byyy'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260123125417_updated farmer table byyy', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    DECLARE @var73 sysname;
    SELECT @var73 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'OwnershipType');
    IF @var73 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var73 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [OwnershipType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    DECLARE @var74 sysname;
    SELECT @var74 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'Name');
    IF @var74 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var74 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [Name] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    DECLARE @var75 sysname;
    SELECT @var75 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'MappedBy');
    IF @var75 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var75 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [MappedBy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    DECLARE @var76 sysname;
    SELECT @var76 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'LocationDescription');
    IF @var76 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var76 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [LocationDescription] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    DECLARE @var77 sysname;
    SELECT @var77 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'CreatedBy');
    IF @var77 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var77 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [CreatedBy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    DECLARE @var78 sysname;
    SELECT @var78 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'CoordinateTakenByUserId');
    IF @var78 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var78 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [CoordinateTakenByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    DECLARE @var79 sysname;
    SELECT @var79 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'CoordinateTakenBy');
    IF @var79 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var79 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [CoordinateTakenBy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    DECLARE @var80 sysname;
    SELECT @var80 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmPlotDetails]') AND [c].[name] = N'CoordinateTakenByUserId');
    IF @var80 IS NOT NULL EXEC(N'ALTER TABLE [FarmPlotDetails] DROP CONSTRAINT [' + @var80 + '];');
    ALTER TABLE [FarmPlotDetails] ALTER COLUMN [CoordinateTakenByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    DECLARE @var81 sysname;
    SELECT @var81 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmPlotDetails]') AND [c].[name] = N'CoordinateTakenBy');
    IF @var81 IS NOT NULL EXEC(N'ALTER TABLE [FarmPlotDetails] DROP CONSTRAINT [' + @var81 + '];');
    ALTER TABLE [FarmPlotDetails] ALTER COLUMN [CoordinateTakenBy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    ALTER TABLE [Cooperatives] ADD [CooperativeLeaderFirstName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    ALTER TABLE [Cooperatives] ADD [CooperativeLeaderGender] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    ALTER TABLE [Cooperatives] ADD [CooperativeLeaderLastName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    ALTER TABLE [Cooperatives] ADD [CooperativeLeaderMiddleName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    ALTER TABLE [Cooperatives] ADD [Tier] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    CREATE TABLE [FarmLandDocuments] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmPlotDetailsId] bigint NOT NULL,
        [DocumentType] int NOT NULL,
        [DocumentUrl] nvarchar(max) NOT NULL,
        [DocumentTitle] nvarchar(max) NULL,
        [DocumentId] nvarchar(max) NOT NULL,
        [FarmId] bigint NULL,
        CONSTRAINT [PK_FarmLandDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FarmLandDocuments_FarmPlotDetails_FarmPlotDetailsId] FOREIGN KEY ([FarmPlotDetailsId]) REFERENCES [FarmPlotDetails] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FarmLandDocuments_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    CREATE INDEX [IX_FarmLandDocuments_FarmId] ON [FarmLandDocuments] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    CREATE INDEX [IX_FarmLandDocuments_FarmPlotDetailsId] ON [FarmLandDocuments] ([FarmPlotDetailsId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124145813_updated user level tier'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260124145813_updated user level tier', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmLandDocuments] DROP CONSTRAINT [FK_FarmLandDocuments_FarmPlotDetails_FarmPlotDetailsId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmLandDocuments] DROP CONSTRAINT [FK_FarmLandDocuments_Farms_FarmId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DROP TABLE [Cooperatives];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var82 sysname;
    SELECT @var82 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'CoordinateOverlapWithFarmerIds');
    IF @var82 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var82 + '];');
    ALTER TABLE [Farms] DROP COLUMN [CoordinateOverlapWithFarmerIds];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var83 sysname;
    SELECT @var83 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'CoordinateTakenBy');
    IF @var83 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var83 + '];');
    ALTER TABLE [Farms] DROP COLUMN [CoordinateTakenBy];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var84 sysname;
    SELECT @var84 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'CoordinateTakenByUserId');
    IF @var84 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var84 + '];');
    ALTER TABLE [Farms] DROP COLUMN [CoordinateTakenByUserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var85 sysname;
    SELECT @var85 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'FarmUnflaggedByName');
    IF @var85 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var85 + '];');
    ALTER TABLE [Farms] DROP COLUMN [FarmUnflaggedByName];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var86 sysname;
    SELECT @var86 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'FarmUnflaggedByUserId');
    IF @var86 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var86 + '];');
    ALTER TABLE [Farms] DROP COLUMN [FarmUnflaggedByUserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var87 sysname;
    SELECT @var87 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'FlaggedOn');
    IF @var87 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var87 + '];');
    ALTER TABLE [Farms] DROP COLUMN [FlaggedOn];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var88 sysname;
    SELECT @var88 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'FlaggedReason');
    IF @var88 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var88 + '];');
    ALTER TABLE [Farms] DROP COLUMN [FlaggedReason];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var89 sysname;
    SELECT @var89 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'IsFarmFlaggedBySystem');
    IF @var89 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var89 + '];');
    ALTER TABLE [Farms] DROP COLUMN [IsFarmFlaggedBySystem];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var90 sysname;
    SELECT @var90 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'UnflaggedOn');
    IF @var90 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var90 + '];');
    ALTER TABLE [Farms] DROP COLUMN [UnflaggedOn];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var91 sysname;
    SELECT @var91 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'UserLevel');
    IF @var91 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var91 + '];');
    ALTER TABLE [AspNetUsers] DROP COLUMN [UserLevel];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    EXEC sp_rename N'[Farms].[UnflagFarm]', N'IsMapped', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    EXEC sp_rename N'[Farms].[CooperativeId]', N'MembershipId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    EXEC sp_rename N'[Farms].[AreaInSqMetre]', N'TotalAreaInSqMetre', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    EXEC sp_rename N'[Farms].[AreaInDegress]', N'TotalAreaInDegress', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [Farms] ADD [AgentId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DECLARE @var92 sysname;
    SELECT @var92 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmLandDocuments]') AND [c].[name] = N'FarmPlotDetailsId');
    IF @var92 IS NOT NULL EXEC(N'ALTER TABLE [FarmLandDocuments] DROP CONSTRAINT [' + @var92 + '];');
    ALTER TABLE [FarmLandDocuments] ALTER COLUMN [FarmPlotDetailsId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    DROP INDEX [IX_FarmLandDocuments_FarmId] ON [FarmLandDocuments];
    DECLARE @var93 sysname;
    SELECT @var93 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmLandDocuments]') AND [c].[name] = N'FarmId');
    IF @var93 IS NOT NULL EXEC(N'ALTER TABLE [FarmLandDocuments] DROP CONSTRAINT [' + @var93 + '];');
    EXEC(N'UPDATE [FarmLandDocuments] SET [FarmId] = CAST(0 AS bigint) WHERE [FarmId] IS NULL');
    ALTER TABLE [FarmLandDocuments] ALTER COLUMN [FarmId] bigint NOT NULL;
    ALTER TABLE [FarmLandDocuments] ADD DEFAULT CAST(0 AS bigint) FOR [FarmId];
    CREATE INDEX [IX_FarmLandDocuments_FarmId] ON [FarmLandDocuments] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmLandDocuments] ADD [IsVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmLandDocuments] ADD [UploadedOn] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [Farmers] ADD [Level] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [CapturedBy] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [CapturedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [CoordinateOverlapWithFarmerIds] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [FarmUnflaggedByName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [FarmUnflaggedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [FlaggedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [FlaggedReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [IsFarmFlaggedBySystem] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [MeasurementDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [MeasurementMode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [SizeInHectares] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [SizeInKm] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [UnflagFarm] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmCoordinate] ADD [UnflaggedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    CREATE TABLE [MemberShipTypes] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [UpdatedOn] datetimeoffset NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_MemberShipTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    CREATE TABLE [MemberShips] (
        [Id] bigint NOT NULL IDENTITY,
        [IsLeaderAFarmer] bit NOT NULL,
        [Name] nvarchar(max) NULL,
        [Location] nvarchar(max) NULL,
        [State] nvarchar(max) NULL,
        [Lga] nvarchar(max) NULL,
        [Ward] nvarchar(max) NULL,
        [ContactPerson] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [MembershipLeaderFirstName] nvarchar(max) NULL,
        [MembershipLeaderMiddleName] nvarchar(max) NULL,
        [MembershipLeaderLastName] nvarchar(max) NULL,
        [MembershipLeaderGender] nvarchar(max) NULL,
        [UserId] nvarchar(max) NOT NULL,
        [BelongToBusinessCode] nvarchar(max) NOT NULL,
        [OnBoardedOn] datetimeoffset NOT NULL,
        [IsAssignedToAgentId] nvarchar(max) NULL,
        [HasbeenAssignedAgent] bit NOT NULL,
        [OrganizationCode] nvarchar(max) NULL,
        [OnboardedById] nvarchar(max) NULL,
        [Tier] int NOT NULL,
        [MemberShipTypeId] bigint NOT NULL,
        CONSTRAINT [PK_MemberShips] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberShips_MemberShipTypes_MemberShipTypeId] FOREIGN KEY ([MemberShipTypeId]) REFERENCES [MemberShipTypes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    CREATE INDEX [IX_MemberShips_MemberShipTypeId] ON [MemberShips] ([MemberShipTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmLandDocuments] ADD CONSTRAINT [FK_FarmLandDocuments_FarmPlotDetails_FarmPlotDetailsId] FOREIGN KEY ([FarmPlotDetailsId]) REFERENCES [FarmPlotDetails] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    ALTER TABLE [FarmLandDocuments] ADD CONSTRAINT [FK_FarmLandDocuments_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201162307_update cooperative to membership'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260201162307_update cooperative to membership', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203132733_updated Farms table'
)
BEGIN
    DECLARE @var94 sysname;
    SELECT @var94 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'FarmerUserId');
    IF @var94 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var94 + '];');
    ALTER TABLE [Farms] DROP COLUMN [FarmerUserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203132733_updated Farms table'
)
BEGIN
    ALTER TABLE [Farms] ADD [FarmerId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203132733_updated Farms table'
)
BEGIN
    ALTER TABLE [Farms] ADD [MappingStatus] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203132733_updated Farms table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260203132733_updated Farms table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    ALTER TABLE [FarmCoordinate] DROP CONSTRAINT [FK_FarmCoordinate_Farms_FarmId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    ALTER TABLE [FarmCoordinate] DROP CONSTRAINT [PK_FarmCoordinate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    DECLARE @var95 sysname;
    SELECT @var95 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'AgentId');
    IF @var95 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var95 + '];');
    ALTER TABLE [Farms] DROP COLUMN [AgentId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    DECLARE @var96 sysname;
    SELECT @var96 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'CommunityId');
    IF @var96 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var96 + '];');
    ALTER TABLE [Farms] DROP COLUMN [CommunityId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    DECLARE @var97 sysname;
    SELECT @var97 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'MembershipId');
    IF @var97 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var97 + '];');
    ALTER TABLE [Farms] DROP COLUMN [MembershipId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    EXEC sp_rename N'[FarmCoordinate]', N'FarmCoordinates';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    EXEC sp_rename N'[Farms].[IsMapped]', N'IsSizeVerified', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    EXEC sp_rename N'[FarmCoordinates].[IsFarmFlaggedBySystem]', N'IsSystemOverlap', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    EXEC sp_rename N'[FarmCoordinates].[IX_FarmCoordinate_FarmId]', N'IX_FarmCoordinates_FarmId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    DECLARE @var98 sysname;
    SELECT @var98 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'FarmWard');
    IF @var98 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var98 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [FarmWard] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    DECLARE @var99 sysname;
    SELECT @var99 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'FarmState');
    IF @var99 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var99 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [FarmState] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    DECLARE @var100 sysname;
    SELECT @var100 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'FarmLocation');
    IF @var100 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var100 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [FarmLocation] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    DECLARE @var101 sysname;
    SELECT @var101 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'FarmLga');
    IF @var101 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var101 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [FarmLga] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    ALTER TABLE [Farms] ADD [FarmPolygon] geography NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    ALTER TABLE [Farms] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    ALTER TABLE [Farms] ADD [IsDocumentVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    ALTER TABLE [Farms] ADD [IsMappingFlagged] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    ALTER TABLE [Farms] ADD [IsMappingVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    ALTER TABLE [Farms] ADD [MappingFlagReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    DECLARE @var102 sysname;
    SELECT @var102 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmLandDocuments]') AND [c].[name] = N'DocumentId');
    IF @var102 IS NOT NULL EXEC(N'ALTER TABLE [FarmLandDocuments] DROP CONSTRAINT [' + @var102 + '];');
    ALTER TABLE [FarmLandDocuments] ALTER COLUMN [DocumentId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    DECLARE @var103 sysname;
    SELECT @var103 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmCoordinates]') AND [c].[name] = N'CapturedBy');
    IF @var103 IS NOT NULL EXEC(N'ALTER TABLE [FarmCoordinates] DROP CONSTRAINT [' + @var103 + '];');
    ALTER TABLE [FarmCoordinates] ALTER COLUMN [CapturedBy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    ALTER TABLE [FarmCoordinates] ADD CONSTRAINT [PK_FarmCoordinates] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    ALTER TABLE [FarmCoordinates] ADD CONSTRAINT [FK_FarmCoordinates_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN

            CREATE SPATIAL INDEX IX_Farms_FarmPolygon
            ON Farms(FarmPolygon)
            USING GEOGRAPHY_GRID
            WITH (
                GRIDS =(LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = HIGH, LEVEL_4 = HIGH),
                CELLS_PER_OBJECT = 16
            );
        
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204153119_upgraded farmmapping'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260204153119_upgraded farmmapping', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    DECLARE @var104 sysname;
    SELECT @var104 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmCoordinates]') AND [c].[name] = N'CapturedBy');
    IF @var104 IS NOT NULL EXEC(N'ALTER TABLE [FarmCoordinates] DROP CONSTRAINT [' + @var104 + '];');
    ALTER TABLE [FarmCoordinates] DROP COLUMN [CapturedBy];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    DECLARE @var105 sysname;
    SELECT @var105 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmCoordinates]') AND [c].[name] = N'CapturedOn');
    IF @var105 IS NOT NULL EXEC(N'ALTER TABLE [FarmCoordinates] DROP CONSTRAINT [' + @var105 + '];');
    ALTER TABLE [FarmCoordinates] DROP COLUMN [CapturedOn];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    DECLARE @var106 sysname;
    SELECT @var106 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmCoordinates]') AND [c].[name] = N'MeasurementDate');
    IF @var106 IS NOT NULL EXEC(N'ALTER TABLE [FarmCoordinates] DROP CONSTRAINT [' + @var106 + '];');
    ALTER TABLE [FarmCoordinates] DROP COLUMN [MeasurementDate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    DECLARE @var107 sysname;
    SELECT @var107 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmCoordinates]') AND [c].[name] = N'MeasurementMode');
    IF @var107 IS NOT NULL EXEC(N'ALTER TABLE [FarmCoordinates] DROP CONSTRAINT [' + @var107 + '];');
    ALTER TABLE [FarmCoordinates] DROP COLUMN [MeasurementMode];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    DECLARE @var108 sysname;
    SELECT @var108 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmCoordinates]') AND [c].[name] = N'SizeInHectares');
    IF @var108 IS NOT NULL EXEC(N'ALTER TABLE [FarmCoordinates] DROP CONSTRAINT [' + @var108 + '];');
    ALTER TABLE [FarmCoordinates] DROP COLUMN [SizeInHectares];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    DECLARE @var109 sysname;
    SELECT @var109 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmCoordinates]') AND [c].[name] = N'SizeInKm');
    IF @var109 IS NOT NULL EXEC(N'ALTER TABLE [FarmCoordinates] DROP CONSTRAINT [' + @var109 + '];');
    ALTER TABLE [FarmCoordinates] DROP COLUMN [SizeInKm];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    ALTER TABLE [Farms] ADD [CapturedBy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    ALTER TABLE [Farms] ADD [CapturedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    ALTER TABLE [Farms] ADD [MeasurementDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    ALTER TABLE [Farms] ADD [MeasurementMode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    ALTER TABLE [Farms] ADD [SizeInHectares] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    ALTER TABLE [Farms] ADD [SizeInKm] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114137_update farms table to include capturing trail'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205114137_update farms table to include capturing trail', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206120941_updated farm measurement'
)
BEGIN
    ALTER TABLE [Farms] ADD [DistanceCovered] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206120941_updated farm measurement'
)
BEGIN
    ALTER TABLE [Farms] ADD [MappingAccuracy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206120941_updated farm measurement'
)
BEGIN
    ALTER TABLE [Farms] ADD [PinnedLatitude] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206120941_updated farm measurement'
)
BEGIN
    ALTER TABLE [Farms] ADD [PinnedLongitude] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206120941_updated farm measurement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260206120941_updated farm measurement', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216083644_created user wallet'
)
BEGIN
    CREATE TABLE [UserWallets] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [WalletProviderId] bigint NOT NULL,
        [WalletBalance] decimal(18,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDefault] bit NOT NULL,
        [AddedOn] datetimeoffset NOT NULL,
        [WalletNumber] nvarchar(50) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_UserWallets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserWallets_WalletProviders_WalletProviderId] FOREIGN KEY ([WalletProviderId]) REFERENCES [WalletProviders] ([ProviderId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216083644_created user wallet'
)
BEGIN
    CREATE TABLE [UserWalletTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [UserWalletId] bigint NOT NULL,
        [WalletTransactionType] int NOT NULL,
        [TransactionAmount] decimal(18,2) NOT NULL,
        [CommissionFee] decimal(18,2) NOT NULL,
        [TransactionFee] decimal(18,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [TransactionDescription] nvarchar(max) NOT NULL,
        [ChargedTo] nvarchar(max) NOT NULL,
        [ChargedToId] nvarchar(max) NOT NULL,
        [TransactionDate] datetimeoffset NOT NULL,
        [WalletTransactionStatus] int NOT NULL,
        [TransactionReference] nvarchar(50) NOT NULL,
        [BalanceBefore] decimal(18,2) NOT NULL,
        [BalanceAfter] decimal(18,2) NOT NULL,
        [ProviderReference] nvarchar(max) NULL,
        [ProviderStatus] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_UserWalletTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserWalletTransactions_UserWallets_UserWalletId] FOREIGN KEY ([UserWalletId]) REFERENCES [UserWallets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216083644_created user wallet'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserWallets_UserId_WalletProviderId] ON [UserWallets] ([UserId], [WalletProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216083644_created user wallet'
)
BEGIN
    CREATE INDEX [IX_UserWallets_WalletProviderId] ON [UserWallets] ([WalletProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216083644_created user wallet'
)
BEGIN
    CREATE INDEX [IX_UserWalletTransactions_UserWalletId] ON [UserWalletTransactions] ([UserWalletId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216083644_created user wallet'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216083644_created user wallet', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216084902_updated farm mapping table'
)
BEGIN
    DECLARE @var110 sysname;
    SELECT @var110 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farms]') AND [c].[name] = N'MeasurementMode');
    IF @var110 IS NOT NULL EXEC(N'ALTER TABLE [Farms] DROP CONSTRAINT [' + @var110 + '];');
    ALTER TABLE [Farms] ALTER COLUMN [MeasurementMode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216084902_updated farm mapping table'
)
BEGIN
    DECLARE @var111 sysname;
    SELECT @var111 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmMappingAuditTrails]') AND [c].[name] = N'Action');
    IF @var111 IS NOT NULL EXEC(N'ALTER TABLE [FarmMappingAuditTrails] DROP CONSTRAINT [' + @var111 + '];');
    ALTER TABLE [FarmMappingAuditTrails] ALTER COLUMN [Action] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216084902_updated farm mapping table'
)
BEGIN
    DECLARE @var112 sysname;
    SELECT @var112 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmLandDocuments]') AND [c].[name] = N'DocumentUrl');
    IF @var112 IS NOT NULL EXEC(N'ALTER TABLE [FarmLandDocuments] DROP CONSTRAINT [' + @var112 + '];');
    ALTER TABLE [FarmLandDocuments] ALTER COLUMN [DocumentUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216084902_updated farm mapping table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216084902_updated farm mapping table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220073936_updated userwallet creation'
)
BEGIN
    ALTER TABLE [UserWallets] ADD [BelongToBusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220073936_updated userwallet creation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserWalletTransactions_TransactionReference] ON [UserWalletTransactions] ([TransactionReference]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220073936_updated userwallet creation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260220073936_updated userwallet creation', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    ALTER TABLE [UserWallets] DROP CONSTRAINT [FK_UserWallets_WalletProviders_WalletProviderId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [FK_UserWalletTransactions_UserWallets_UserWalletId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DROP TABLE [Wallets];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DROP TABLE [WalletTransactions];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var113 sysname;
    SELECT @var113 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWallets]') AND [c].[name] = N'IsDefault');
    IF @var113 IS NOT NULL EXEC(N'ALTER TABLE [UserWallets] DROP CONSTRAINT [' + @var113 + '];');
    ALTER TABLE [UserWallets] DROP COLUMN [IsDefault];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var114 sysname;
    SELECT @var114 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWalletTransactions]') AND [c].[name] = N'WalletTransactionType');
    IF @var114 IS NOT NULL EXEC(N'ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [' + @var114 + '];');
    ALTER TABLE [UserWalletTransactions] ALTER COLUMN [WalletTransactionType] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var115 sysname;
    SELECT @var115 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWalletTransactions]') AND [c].[name] = N'WalletTransactionStatus');
    IF @var115 IS NOT NULL EXEC(N'ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [' + @var115 + '];');
    ALTER TABLE [UserWalletTransactions] ALTER COLUMN [WalletTransactionStatus] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var116 sysname;
    SELECT @var116 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWalletTransactions]') AND [c].[name] = N'TransactionDescription');
    IF @var116 IS NOT NULL EXEC(N'ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [' + @var116 + '];');
    ALTER TABLE [UserWalletTransactions] ALTER COLUMN [TransactionDescription] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var117 sysname;
    SELECT @var117 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWalletTransactions]') AND [c].[name] = N'ProviderStatus');
    IF @var117 IS NOT NULL EXEC(N'ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [' + @var117 + '];');
    ALTER TABLE [UserWalletTransactions] ALTER COLUMN [ProviderStatus] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var118 sysname;
    SELECT @var118 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWalletTransactions]') AND [c].[name] = N'ProviderReference');
    IF @var118 IS NOT NULL EXEC(N'ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [' + @var118 + '];');
    ALTER TABLE [UserWalletTransactions] ALTER COLUMN [ProviderReference] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var119 sysname;
    SELECT @var119 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWalletTransactions]') AND [c].[name] = N'Currency');
    IF @var119 IS NOT NULL EXEC(N'ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [' + @var119 + '];');
    ALTER TABLE [UserWalletTransactions] ALTER COLUMN [Currency] nvarchar(3) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var120 sysname;
    SELECT @var120 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWalletTransactions]') AND [c].[name] = N'ChargedToId');
    IF @var120 IS NOT NULL EXEC(N'ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [' + @var120 + '];');
    ALTER TABLE [UserWalletTransactions] ALTER COLUMN [ChargedToId] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var121 sysname;
    SELECT @var121 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWalletTransactions]') AND [c].[name] = N'ChargedTo');
    IF @var121 IS NOT NULL EXEC(N'ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [' + @var121 + '];');
    ALTER TABLE [UserWalletTransactions] ALTER COLUMN [ChargedTo] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DROP INDEX [IX_UserWallets_UserId_WalletProviderId] ON [UserWallets];
    DECLARE @var122 sysname;
    SELECT @var122 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWallets]') AND [c].[name] = N'UserId');
    IF @var122 IS NOT NULL EXEC(N'ALTER TABLE [UserWallets] DROP CONSTRAINT [' + @var122 + '];');
    ALTER TABLE [UserWallets] ALTER COLUMN [UserId] nvarchar(100) NOT NULL;
    CREATE UNIQUE INDEX [IX_UserWallets_UserId_WalletProviderId] ON [UserWallets] ([UserId], [WalletProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var123 sysname;
    SELECT @var123 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWallets]') AND [c].[name] = N'Currency');
    IF @var123 IS NOT NULL EXEC(N'ALTER TABLE [UserWallets] DROP CONSTRAINT [' + @var123 + '];');
    ALTER TABLE [UserWallets] ALTER COLUMN [Currency] nvarchar(3) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    DECLARE @var124 sysname;
    SELECT @var124 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWallets]') AND [c].[name] = N'BelongToBusinessCode');
    IF @var124 IS NOT NULL EXEC(N'ALTER TABLE [UserWallets] DROP CONSTRAINT [' + @var124 + '];');
    ALTER TABLE [UserWallets] ALTER COLUMN [BelongToBusinessCode] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    CREATE TABLE [UserWalletContexts] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(100) NOT NULL,
        [UserWalletId] bigint NOT NULL,
        [ContextType] nvarchar(450) NOT NULL,
        [ContextReferenceId] nvarchar(100) NULL,
        [IsDefault] bit NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_UserWalletContexts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserWalletContexts_UserWallets_UserWalletId] FOREIGN KEY ([UserWalletId]) REFERENCES [UserWallets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserWalletContexts_UserId_ContextType_ContextReferenceId] ON [UserWalletContexts] ([UserId], [ContextType], [ContextReferenceId]) WHERE [ContextReferenceId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    CREATE INDEX [IX_UserWalletContexts_UserWalletId] ON [UserWalletContexts] ([UserWalletId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    ALTER TABLE [UserWallets] ADD CONSTRAINT [FK_UserWallets_WalletProviders_WalletProviderId] FOREIGN KEY ([WalletProviderId]) REFERENCES [WalletProviders] ([ProviderId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    ALTER TABLE [UserWalletTransactions] ADD CONSTRAINT [FK_UserWalletTransactions_UserWallets_UserWalletId] FOREIGN KEY ([UserWalletId]) REFERENCES [UserWallets] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221084700_updated userwallet to cater for organizations as well'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260221084700_updated userwallet to cater for organizations as well', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    EXEC sp_rename N'[Farms].[TotalAreaInSqMetre]', N'FarmSizeInSqMetre', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    EXEC sp_rename N'[Farms].[TotalAreaInDegress]', N'FarmSizeInDegress', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    EXEC sp_rename N'[Farms].[AreaSize]', N'FarmSize', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    EXEC sp_rename N'[BusinessOrganizations].[OrganizationName]', N'UserId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    EXEC sp_rename N'[BusinessOrganizations].[Location]', N'RoleInOrganization', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    EXEC sp_rename N'[BusinessOrganizations].[Contact]', N'PhoneNumber', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    EXEC sp_rename N'[BusinessOrganizations].[ContacPerson]', N'OrganizationPhone', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [Farms] ADD [FarmSizeType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [CACNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [CompanyAddress] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [CompanyName] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [CompanyType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [FirstName] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [FullName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [LastName] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [LgaId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [OnboardedOn] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [StateId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    CREATE INDEX [IX_BusinessOrganizations_LgaId] ON [BusinessOrganizations] ([LgaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    CREATE INDEX [IX_BusinessOrganizations_StateId] ON [BusinessOrganizations] ([StateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD CONSTRAINT [FK_BusinessOrganizations_LGAs_LgaId] FOREIGN KEY ([LgaId]) REFERENCES [LGAs] ([LgaId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD CONSTRAINT [FK_BusinessOrganizations_States_StateId] FOREIGN KEY ([StateId]) REFERENCES [States] ([StateId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226134344_updated organization table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260226134344_updated organization table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227071638_added activity log table'
)
BEGIN
    DECLARE @var125 sysname;
    SELECT @var125 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MemberShips]') AND [c].[name] = N'IsAssignedToAgentId');
    IF @var125 IS NOT NULL EXEC(N'ALTER TABLE [MemberShips] DROP CONSTRAINT [' + @var125 + '];');
    ALTER TABLE [MemberShips] DROP COLUMN [IsAssignedToAgentId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227071638_added activity log table'
)
BEGIN
    ALTER TABLE [MemberShips] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227071638_added activity log table'
)
BEGIN
    CREATE TABLE [ActivityLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActivityName] nvarchar(max) NOT NULL,
        [ActivityType] int NOT NULL,
        [PerformedByUserId] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227071638_added activity log table'
)
BEGIN
    CREATE TABLE [MemberShipAgents] (
        [Id] bigint NOT NULL IDENTITY,
        [AgentId] bigint NOT NULL,
        [MemberShipId] bigint NOT NULL,
        [AssignedOn] datetimeoffset NOT NULL,
        [OnboardedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_MemberShipAgents] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227071638_added activity log table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260227071638_added activity log table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227095217_added businesscode and orgcode on activitylog'
)
BEGIN
    ALTER TABLE [ActivityLogs] ADD [BusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227095217_added businesscode and orgcode on activitylog'
)
BEGIN
    ALTER TABLE [ActivityLogs] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227095217_added businesscode and orgcode on activitylog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260227095217_added businesscode and orgcode on activitylog', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260302141405_updated farmer level'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260302141405_updated farmer level', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303153149_added input'
)
BEGIN
    CREATE TABLE [Inputs] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [UnitOfMeasure] nvarchar(max) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Inputs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303153149_added input'
)
BEGIN
    CREATE TABLE [InputTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerId] bigint NOT NULL,
        [FarmId] bigint NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TransactionType] int NOT NULL,
        [PaymentStatus] int NOT NULL,
        [ReferenceNumber] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [TransactionDate] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_InputTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InputTransactions_Farmers_FarmerId] FOREIGN KEY ([FarmerId]) REFERENCES [Farmers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InputTransactions_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303153149_added input'
)
BEGIN
    CREATE TABLE [InputAllocations] (
        [Id] bigint NOT NULL IDENTITY,
        [FarmerId] bigint NOT NULL,
        [FarmId] bigint NOT NULL,
        [InputId] bigint NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [AllocatedDate] datetime2 NOT NULL,
        [InputTransactionId] bigint NULL,
        CONSTRAINT [PK_InputAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InputAllocations_Farmers_FarmerId] FOREIGN KEY ([FarmerId]) REFERENCES [Farmers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InputAllocations_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InputAllocations_InputTransactions_InputTransactionId] FOREIGN KEY ([InputTransactionId]) REFERENCES [InputTransactions] ([Id]),
        CONSTRAINT [FK_InputAllocations_Inputs_InputId] FOREIGN KEY ([InputId]) REFERENCES [Inputs] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303153149_added input'
)
BEGIN
    CREATE INDEX [IX_InputAllocations_FarmerId] ON [InputAllocations] ([FarmerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303153149_added input'
)
BEGIN
    CREATE INDEX [IX_InputAllocations_FarmId] ON [InputAllocations] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303153149_added input'
)
BEGIN
    CREATE INDEX [IX_InputAllocations_InputId] ON [InputAllocations] ([InputId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303153149_added input'
)
BEGIN
    CREATE INDEX [IX_InputAllocations_InputTransactionId] ON [InputAllocations] ([InputTransactionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303153149_added input'
)
BEGIN
    CREATE INDEX [IX_InputTransactions_FarmerId] ON [InputTransactions] ([FarmerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303153149_added input'
)
BEGIN
    CREATE INDEX [IX_InputTransactions_FarmId] ON [InputTransactions] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303153149_added input'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260303153149_added input', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306130748_added transaction pin for user'
)
BEGIN
    ALTER TABLE [UserWallets] ADD [TransactionPin] nvarchar(6) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306130748_added transaction pin for user'
)
BEGIN
    ALTER TABLE [UserWallets] ADD [Txn] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306130748_added transaction pin for user'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260306130748_added transaction pin for user', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306143918_added isactive to agent'
)
BEGIN
    ALTER TABLE [Agents] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306143918_added isactive to agent'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260306143918_added isactive to agent', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260309101749_chekcing migration file'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260309101749_chekcing migration file', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260310131008_added wallet securities'
)
BEGIN
    DECLARE @var126 sysname;
    SELECT @var126 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWallets]') AND [c].[name] = N'TransactionPin');
    IF @var126 IS NOT NULL EXEC(N'ALTER TABLE [UserWallets] DROP CONSTRAINT [' + @var126 + '];');
    ALTER TABLE [UserWallets] DROP COLUMN [TransactionPin];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260310131008_added wallet securities'
)
BEGIN
    DECLARE @var127 sysname;
    SELECT @var127 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWallets]') AND [c].[name] = N'Txn');
    IF @var127 IS NOT NULL EXEC(N'ALTER TABLE [UserWallets] DROP CONSTRAINT [' + @var127 + '];');
    ALTER TABLE [UserWallets] DROP COLUMN [Txn];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260310131008_added wallet securities'
)
BEGIN
    CREATE TABLE [WalletSecurities] (
        [Id] bigint NOT NULL IDENTITY,
        [UserWalletId] bigint NOT NULL,
        [TransactionPinHash] nvarchar(200) NOT NULL,
        [Txn] nvarchar(max) NOT NULL,
        [PinFailedAttempts] int NOT NULL,
        [PinLockedUntil] datetimeoffset NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [LastPinChangeDate] datetimeoffset NULL,
        CONSTRAINT [PK_WalletSecurities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WalletSecurities_UserWallets_UserWalletId] FOREIGN KEY ([UserWalletId]) REFERENCES [UserWallets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260310131008_added wallet securities'
)
BEGIN
    CREATE INDEX [IX_WalletSecurities_UserWalletId] ON [WalletSecurities] ([UserWalletId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260310131008_added wallet securities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260310131008_added wallet securities', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    DECLARE @var128 sysname;
    SELECT @var128 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MemberShips]') AND [c].[name] = N'OrganizationCode');
    IF @var128 IS NOT NULL EXEC(N'ALTER TABLE [MemberShips] DROP CONSTRAINT [' + @var128 + '];');
    ALTER TABLE [MemberShips] DROP COLUMN [OrganizationCode];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    DECLARE @var129 sysname;
    SELECT @var129 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agents]') AND [c].[name] = N'OrganizationCode');
    IF @var129 IS NOT NULL EXEC(N'ALTER TABLE [Agents] DROP CONSTRAINT [' + @var129 + '];');
    ALTER TABLE [Agents] DROP COLUMN [OrganizationCode];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsEmailVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsPinEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE TABLE [LocationTypes] (
        [Id] bigint NOT NULL IDENTITY,
        [Type] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NULL,
        CONSTRAINT [PK_LocationTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE TABLE [OrganizationAgents] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationId] bigint NOT NULL,
        [AgentId] bigint NOT NULL,
        [AssignedOn] datetimeoffset NOT NULL,
        [AssignedByUserId] nvarchar(max) NULL,
        [LocationId] bigint NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_OrganizationAgents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationAgents_BusinessOrganizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [BusinessOrganizations] ([OrganizationId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE TABLE [OrganizationMemberships] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationId] bigint NOT NULL,
        [MembershipId] bigint NOT NULL,
        [AssignedOn] datetimeoffset NOT NULL,
        [AssignedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_OrganizationMemberships] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE TABLE [OrganizationRoles] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationId] bigint NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [RoleName] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_OrganizationRoles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationRoles_BusinessOrganizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [BusinessOrganizations] ([OrganizationId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE TABLE [OrganizationLocations] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationId] bigint NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [LocationName] nvarchar(max) NOT NULL,
        [LocationTypeId] bigint NOT NULL,
        [StateId] bigint NOT NULL,
        [LgaId] bigint NOT NULL,
        [Address] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_OrganizationLocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationLocations_BusinessOrganizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [BusinessOrganizations] ([OrganizationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrganizationLocations_LGAs_LgaId] FOREIGN KEY ([LgaId]) REFERENCES [LGAs] ([LgaId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrganizationLocations_LocationTypes_LocationTypeId] FOREIGN KEY ([LocationTypeId]) REFERENCES [LocationTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrganizationLocations_States_StateId] FOREIGN KEY ([StateId]) REFERENCES [States] ([StateId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE TABLE [OrganizationAgentRoles] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationAgentId] bigint NOT NULL,
        [RoleId] bigint NOT NULL,
        [AssignedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_OrganizationAgentRoles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationAgentRoles_OrganizationAgents_OrganizationAgentId] FOREIGN KEY ([OrganizationAgentId]) REFERENCES [OrganizationAgents] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrganizationAgentRoles_OrganizationRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [OrganizationRoles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE INDEX [IX_OrganizationAgentRoles_OrganizationAgentId] ON [OrganizationAgentRoles] ([OrganizationAgentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE INDEX [IX_OrganizationAgentRoles_RoleId] ON [OrganizationAgentRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE INDEX [IX_OrganizationAgents_OrganizationId] ON [OrganizationAgents] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE INDEX [IX_OrganizationLocations_LgaId] ON [OrganizationLocations] ([LgaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE INDEX [IX_OrganizationLocations_LocationTypeId] ON [OrganizationLocations] ([LocationTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE INDEX [IX_OrganizationLocations_OrganizationId] ON [OrganizationLocations] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE INDEX [IX_OrganizationLocations_StateId] ON [OrganizationLocations] ([StateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    CREATE INDEX [IX_OrganizationRoles_OrganizationId] ON [OrganizationRoles] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313103241_organizationagentmodulesetup'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260313103241_organizationagentmodulesetup', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313183133_updated organuzation location'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [Capacity] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313183133_updated organuzation location'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [CapacityType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313183133_updated organuzation location'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [StockLevel] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313183133_updated organuzation location'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260313183133_updated organuzation location', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318101816_updated farmer organization'
)
BEGIN
    ALTER TABLE [FarmerOrganizations] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318101816_updated farmer organization'
)
BEGIN
    ALTER TABLE [FarmerOrganizations] ADD [IsDeactivated] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318101816_updated farmer organization'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260318101816_updated farmer organization', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326125109_updated users tablewith farmeronboarding status'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [FarmerOnboardingStatus] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326125109_updated users tablewith farmeronboarding status'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [OnboardingError] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326125109_updated users tablewith farmeronboarding status'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260326125109_updated users tablewith farmeronboarding status', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327141117_added IsActive to tables'
)
BEGIN
    ALTER TABLE [UserOrganizations] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327141117_added IsActive to tables'
)
BEGIN
    ALTER TABLE [OrganizationMemberships] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327141117_added IsActive to tables'
)
BEGIN
    ALTER TABLE [MemberShipAgents] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327141117_added IsActive to tables'
)
BEGIN
    ALTER TABLE [FarmerCooperatives] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327141117_added IsActive to tables'
)
BEGIN
    ALTER TABLE [FarmerAgents] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327141117_added IsActive to tables'
)
BEGIN
    ALTER TABLE [BusinessWorkers] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327141117_added IsActive to tables'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327141117_added IsActive to tables'
)
BEGIN
    ALTER TABLE [Businesses] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327141117_added IsActive to tables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260327141117_added IsActive to tables', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    DECLARE @var130 sysname;
    SELECT @var130 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmerOrganizations]') AND [c].[name] = N'IsDeactivated');
    IF @var130 IS NOT NULL EXEC(N'ALTER TABLE [FarmerOrganizations] DROP CONSTRAINT [' + @var130 + '];');
    ALTER TABLE [FarmerOrganizations] DROP COLUMN [IsDeactivated];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [OrganizationMemberships] ADD [DeactivatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [OrganizationMemberships] ADD [DeactivationReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [CreatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [DeactivatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [DeactivationReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [OrganizationAgents] ADD [DeactivatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [OrganizationAgents] ADD [DeactivationReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [MemberShipAgents] ADD [DeactivatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [MemberShipAgents] ADD [DeactivationReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerSalesTracker] ADD [ContextType] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerSalesTracker] ADD [PaymentStatus] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerOrganizations] ADD [DeactivatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerOrganizations] ADD [DeactivatedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerOrganizations] ADD [DeactivationReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerCooperatives] ADD [DeactivatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerCooperatives] ADD [DeactivatedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerCooperatives] ADD [DeactivationReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerCooperatives] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerAgents] ADD [DeactivatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerAgents] ADD [DeactivatedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerAgents] ADD [DeactivationReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    ALTER TABLE [FarmerAgents] ADD [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260330185154_updated farmer mappings org'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260330185154_updated farmer mappings org', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331142317_updated default password'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [HasDefaultPassword] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331142317_updated default password'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [HasDefaultPin] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331142317_updated default password'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331142317_updated default password', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413114319_added background job tracking'
)
BEGIN
    CREATE TABLE [BulkUploadJobs] (
        [Id] uniqueidentifier NOT NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [OrganizationCode] nvarchar(max) NOT NULL,
        [CreatedByUserId] nvarchar(max) NOT NULL,
        [OnboardedByUserId] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ErrorMessage] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [TotalRows] int NOT NULL,
        [Successful] int NOT NULL,
        [Failed] int NOT NULL,
        CONSTRAINT [PK_BulkUploadJobs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413114319_added background job tracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260413114319_added background job tracking', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413135719_updated organization for nullable location value'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] DROP CONSTRAINT [FK_BusinessOrganizations_LGAs_LgaId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413135719_updated organization for nullable location value'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] DROP CONSTRAINT [FK_BusinessOrganizations_States_StateId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413135719_updated organization for nullable location value'
)
BEGIN
    DECLARE @var131 sysname;
    SELECT @var131 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusinessOrganizations]') AND [c].[name] = N'StateId');
    IF @var131 IS NOT NULL EXEC(N'ALTER TABLE [BusinessOrganizations] DROP CONSTRAINT [' + @var131 + '];');
    ALTER TABLE [BusinessOrganizations] ALTER COLUMN [StateId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413135719_updated organization for nullable location value'
)
BEGIN
    DECLARE @var132 sysname;
    SELECT @var132 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusinessOrganizations]') AND [c].[name] = N'LgaId');
    IF @var132 IS NOT NULL EXEC(N'ALTER TABLE [BusinessOrganizations] DROP CONSTRAINT [' + @var132 + '];');
    ALTER TABLE [BusinessOrganizations] ALTER COLUMN [LgaId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413135719_updated organization for nullable location value'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD CONSTRAINT [FK_BusinessOrganizations_LGAs_LgaId] FOREIGN KEY ([LgaId]) REFERENCES [LGAs] ([LgaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413135719_updated organization for nullable location value'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] ADD CONSTRAINT [FK_BusinessOrganizations_States_StateId] FOREIGN KEY ([StateId]) REFERENCES [States] ([StateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413135719_updated organization for nullable location value'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260413135719_updated organization for nullable location value', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414131720_updated repayments table'
)
BEGIN
    DECLARE @var133 sysname;
    SELECT @var133 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Repayments]') AND [c].[name] = N'FarmerUserId');
    IF @var133 IS NOT NULL EXEC(N'ALTER TABLE [Repayments] DROP CONSTRAINT [' + @var133 + '];');
    ALTER TABLE [Repayments] DROP COLUMN [FarmerUserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414131720_updated repayments table'
)
BEGIN
    ALTER TABLE [Repayments] ADD [FarmerId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414131720_updated repayments table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260414131720_updated repayments table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [DateOfCommissioning] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [DeletedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [HasTemperatureControl] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [Latitude] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [LocationStatus] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [Longitude] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [OwnershipType] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [ParentLocationId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [TemperatureControlTypeId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE TABLE [LocationInventories] (
        [Id] bigint NOT NULL IDENTITY,
        [LocationId] bigint NOT NULL,
        [RecordedOn] datetimeoffset NOT NULL,
        [InventoryCode] nvarchar(max) NULL,
        [BatchNumber] nvarchar(max) NULL,
        [InventoryOwner] nvarchar(max) NOT NULL,
        [Quantity] float NOT NULL,
        [QuantityType] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [StorageDate] datetimeoffset NULL,
        [ExpiryDate] datetimeoffset NULL,
        [QualityGrade] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [RecordByUserId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_LocationInventories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LocationInventories_OrganizationLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [OrganizationLocations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE TABLE [Modules] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Code] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_Modules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE TABLE [StorageCategories] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_StorageCategories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE TABLE [TemperatureControlTypes] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_TemperatureControlTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE TABLE [LocationModules] (
        [LocationId] bigint NOT NULL,
        [ModuleId] bigint NOT NULL,
        CONSTRAINT [PK_LocationModules] PRIMARY KEY ([LocationId], [ModuleId]),
        CONSTRAINT [FK_LocationModules_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LocationModules_OrganizationLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [OrganizationLocations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE TABLE [LocationStorageCategories] (
        [LocationId] bigint NOT NULL,
        [StorageCategoryId] bigint NOT NULL,
        CONSTRAINT [PK_LocationStorageCategories] PRIMARY KEY ([LocationId], [StorageCategoryId]),
        CONSTRAINT [FK_LocationStorageCategories_OrganizationLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [OrganizationLocations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LocationStorageCategories_StorageCategories_StorageCategoryId] FOREIGN KEY ([StorageCategoryId]) REFERENCES [StorageCategories] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE INDEX [IX_OrganizationLocations_ParentLocationId] ON [OrganizationLocations] ([ParentLocationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE INDEX [IX_OrganizationLocations_TemperatureControlTypeId] ON [OrganizationLocations] ([TemperatureControlTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE INDEX [IX_LocationInventories_LocationId] ON [LocationInventories] ([LocationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE INDEX [IX_LocationModules_ModuleId] ON [LocationModules] ([ModuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    CREATE INDEX [IX_LocationStorageCategories_StorageCategoryId] ON [LocationStorageCategories] ([StorageCategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD CONSTRAINT [FK_OrganizationLocations_OrganizationLocations_ParentLocationId] FOREIGN KEY ([ParentLocationId]) REFERENCES [OrganizationLocations] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD CONSTRAINT [FK_OrganizationLocations_TemperatureControlTypes_TemperatureControlTypeId] FOREIGN KEY ([TemperatureControlTypeId]) REFERENCES [TemperatureControlTypes] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155047_added organization sub locations and inventory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260423155047_added organization sub locations and inventory', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155759_added businesscode to tables'
)
BEGIN
    ALTER TABLE [TemperatureControlTypes] ADD [BusinessCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155759_added businesscode to tables'
)
BEGIN
    ALTER TABLE [StorageCategories] ADD [BusinessCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155759_added businesscode to tables'
)
BEGIN
    ALTER TABLE [Modules] ADD [BusinessCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423155759_added businesscode to tables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260423155759_added businesscode to tables', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260423161925_instantiated collection'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260423161925_instantiated collection', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [OrganizationAgents] ADD [DeactivatedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [OrganizationAgents] ADD [IsPrimary] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [LocationInventories] ADD [BusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [Agents] ADD [Tier] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    CREATE INDEX [IX_OrganizationMemberships_MembershipId] ON [OrganizationMemberships] ([MembershipId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    CREATE INDEX [IX_OrganizationMemberships_OrganizationId] ON [OrganizationMemberships] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    CREATE INDEX [IX_OrganizationAgents_AgentId] ON [OrganizationAgents] ([AgentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OrganizationAgents_LocationId_IsPrimary] ON [OrganizationAgents] ([LocationId], [IsPrimary]) WHERE [IsPrimary] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    CREATE INDEX [IX_MemberShipAgents_AgentId] ON [MemberShipAgents] ([AgentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    CREATE INDEX [IX_MemberShipAgents_MemberShipId] ON [MemberShipAgents] ([MemberShipId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    CREATE INDEX [IX_FarmerOrganizations_FarmerId] ON [FarmerOrganizations] ([FarmerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    CREATE INDEX [IX_FarmerCooperatives_FarmerId] ON [FarmerCooperatives] ([FarmerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    CREATE INDEX [IX_FarmerAgents_AgentId] ON [FarmerAgents] ([AgentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    CREATE INDEX [IX_FarmerAgents_FarmerId] ON [FarmerAgents] ([FarmerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    DELETE FROM FarmerOrganizations WHERE FarmerId IS NOT NULL AND FarmerId NOT IN (SELECT Id FROM Farmers);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    DELETE FROM FarmerCooperatives WHERE FarmerId IS NOT NULL AND FarmerId NOT IN (SELECT Id FROM Farmers);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    DELETE FROM FarmerAgents WHERE FarmerId IS NOT NULL AND FarmerId NOT IN (SELECT Id FROM Farmers);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    DELETE FROM OrganizationAgents WHERE LocationId IS NOT NULL AND LocationId NOT IN (SELECT Id FROM OrganizationLocations);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    DELETE FROM OrganizationMemberships WHERE OrganizationId IS NOT NULL AND OrganizationId NOT IN (SELECT OrganizationId FROM BusinessOrganizations);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    DELETE FROM OrganizationMemberships WHERE MembershipId IS NOT NULL AND MembershipId NOT IN (SELECT Id FROM MemberShips);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [FarmerAgents] ADD CONSTRAINT [FK_FarmerAgents_Agents_AgentId] FOREIGN KEY ([AgentId]) REFERENCES [Agents] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [FarmerAgents] ADD CONSTRAINT [FK_FarmerAgents_Farmers_FarmerId] FOREIGN KEY ([FarmerId]) REFERENCES [Farmers] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [FarmerCooperatives] ADD CONSTRAINT [FK_FarmerCooperatives_Farmers_FarmerId] FOREIGN KEY ([FarmerId]) REFERENCES [Farmers] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [FarmerOrganizations] ADD CONSTRAINT [FK_FarmerOrganizations_Farmers_FarmerId] FOREIGN KEY ([FarmerId]) REFERENCES [Farmers] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [MemberShipAgents] ADD CONSTRAINT [FK_MemberShipAgents_Agents_AgentId] FOREIGN KEY ([AgentId]) REFERENCES [Agents] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [MemberShipAgents] ADD CONSTRAINT [FK_MemberShipAgents_MemberShips_MemberShipId] FOREIGN KEY ([MemberShipId]) REFERENCES [MemberShips] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [OrganizationAgents] ADD CONSTRAINT [FK_OrganizationAgents_Agents_AgentId] FOREIGN KEY ([AgentId]) REFERENCES [Agents] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [OrganizationAgents] ADD CONSTRAINT [FK_OrganizationAgents_OrganizationLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [OrganizationLocations] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [OrganizationMemberships] ADD CONSTRAINT [FK_OrganizationMemberships_BusinessOrganizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [BusinessOrganizations] ([OrganizationId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    ALTER TABLE [OrganizationMemberships] ADD CONSTRAINT [FK_OrganizationMemberships_MemberShips_MembershipId] FOREIGN KEY ([MembershipId]) REFERENCES [MemberShips] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260424100931_added relationship'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260424100931_added relationship', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    ALTER TABLE [InputAllocations] DROP CONSTRAINT [FK_InputAllocations_Inputs_InputId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    DROP TABLE [Inputs];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    DECLARE @var134 sysname;
    SELECT @var134 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LocationInventories]') AND [c].[name] = N'InventoryCode');
    IF @var134 IS NOT NULL EXEC(N'ALTER TABLE [LocationInventories] DROP CONSTRAINT [' + @var134 + '];');
    ALTER TABLE [LocationInventories] DROP COLUMN [InventoryCode];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    ALTER TABLE [LocationInventories] ADD [OrganizationProductId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE TABLE [EconomicsOfProductions] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Crop] nvarchar(max) NOT NULL,
        [LandSize] decimal(18,2) NOT NULL,
        [UnitOfMeasure] nvarchar(max) NOT NULL,
        [ExpectedYield] decimal(18,2) NOT NULL,
        [ExpectedMarketPrice] decimal(18,2) NOT NULL,
        [TotalCostOfProduction] decimal(18,2) NOT NULL,
        [Season] nvarchar(max) NOT NULL,
        [OrganizationId] bigint NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_EconomicsOfProductions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EconomicsOfProductions_BusinessOrganizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [BusinessOrganizations] ([OrganizationId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE TABLE [InputFinancingCategory] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_InputFinancingCategory] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE TABLE [OrganizationProductCategories] (
        [Id] bigint NOT NULL IDENTITY,
        [CategoryName] nvarchar(max) NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_OrganizationProductCategories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE TABLE [InputPackages] (
        [Id] bigint NOT NULL IDENTITY,
        [InputFinancingCategoryId] bigint NOT NULL,
        [SubScriptionType] int NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [OrganizationId] bigint NOT NULL,
        [PackageStructure] int NOT NULL,
        [TotalPackageValue] decimal(18,2) NOT NULL,
        [CommitmentRequired] bit NOT NULL,
        [CommitmentType] int NOT NULL,
        [CommitmentValue] decimal(18,2) NOT NULL,
        [RiskCategory] int NOT NULL,
        [RepaymentMode] int NOT NULL,
        [RepaymentPeriodInMonths] int NOT NULL,
        [UserWalletId] bigint NOT NULL,
        [RepaymentValue] decimal(18,2) NOT NULL,
        [Season] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [IsActive] bit NOT NULL,
        [Version] int NOT NULL,
        [ParentPackageId] bigint NULL,
        [AllowPartialUtilization] bit NOT NULL,
        [PlannedDisbursementDate] datetimeoffset NULL,
        [InterestRatePercentage] decimal(18,2) NOT NULL,
        [IsInterestApplied] bit NOT NULL,
        [ScheduleType] int NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [UpdatedOn] datetimeoffset NULL,
        [CreatedByUserId] bigint NOT NULL,
        [EconomicsOfProductionId] bigint NULL,
        [IsGeneratedFromEop] bit NOT NULL,
        [IsEditable] bit NOT NULL,
        [PublishedOn] datetimeoffset NULL,
        CONSTRAINT [PK_InputPackages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InputPackages_BusinessOrganizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [BusinessOrganizations] ([OrganizationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_InputPackages_EconomicsOfProductions_EconomicsOfProductionId] FOREIGN KEY ([EconomicsOfProductionId]) REFERENCES [EconomicsOfProductions] ([Id]),
        CONSTRAINT [FK_InputPackages_InputFinancingCategory_InputFinancingCategoryId] FOREIGN KEY ([InputFinancingCategoryId]) REFERENCES [InputFinancingCategory] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InputPackages_InputPackages_ParentPackageId] FOREIGN KEY ([ParentPackageId]) REFERENCES [InputPackages] ([Id]),
        CONSTRAINT [FK_InputPackages_UserWallets_UserWalletId] FOREIGN KEY ([UserWalletId]) REFERENCES [UserWallets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE TABLE [OrganizationProducts] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationId] bigint NOT NULL,
        [OrganizationProductCategoryId] bigint NOT NULL,
        [ProductName] nvarchar(max) NOT NULL,
        [ProductCode] nvarchar(max) NOT NULL,
        [Season] nvarchar(max) NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [UnitType] nvarchar(max) NULL,
        CONSTRAINT [PK_OrganizationProducts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationProducts_BusinessOrganizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [BusinessOrganizations] ([OrganizationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrganizationProducts_OrganizationProductCategories_OrganizationProductCategoryId] FOREIGN KEY ([OrganizationProductCategoryId]) REFERENCES [OrganizationProductCategories] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE TABLE [AdditionalInputPackageCosts] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [InputPackageId] bigint NOT NULL,
        [IsActive] bit NOT NULL,
        [Type] int NOT NULL,
        [IsPerUnit] bit NOT NULL,
        [Quantity] decimal(18,2) NULL,
        CONSTRAINT [PK_AdditionalInputPackageCosts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AdditionalInputPackageCosts_InputPackages_InputPackageId] FOREIGN KEY ([InputPackageId]) REFERENCES [InputPackages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE TABLE [EopCostItems] (
        [Id] bigint NOT NULL IDENTITY,
        [EconomicsOfProductionId] bigint NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [UnitCost] decimal(18,2) NOT NULL,
        [OrganizationProductId] bigint NULL,
        CONSTRAINT [PK_EopCostItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EopCostItems_EconomicsOfProductions_EconomicsOfProductionId] FOREIGN KEY ([EconomicsOfProductionId]) REFERENCES [EconomicsOfProductions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EopCostItems_OrganizationProducts_OrganizationProductId] FOREIGN KEY ([OrganizationProductId]) REFERENCES [OrganizationProducts] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE TABLE [InputPackageItem] (
        [Id] bigint NOT NULL IDENTITY,
        [InputPackageId] bigint NOT NULL,
        [OrganizationProductId] bigint NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [UnitOfMeasure] nvarchar(max) NOT NULL,
        [UnitCost] decimal(18,2) NOT NULL,
        [OverrideCost] decimal(18,2) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_InputPackageItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InputPackageItem_InputPackages_InputPackageId] FOREIGN KEY ([InputPackageId]) REFERENCES [InputPackages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InputPackageItem_OrganizationProducts_OrganizationProductId] FOREIGN KEY ([OrganizationProductId]) REFERENCES [OrganizationProducts] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_LocationInventories_OrganizationProductId] ON [LocationInventories] ([OrganizationProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_AdditionalInputPackageCosts_InputPackageId] ON [AdditionalInputPackageCosts] ([InputPackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_EconomicsOfProductions_OrganizationId] ON [EconomicsOfProductions] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_EopCostItems_EconomicsOfProductionId] ON [EopCostItems] ([EconomicsOfProductionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_EopCostItems_OrganizationProductId] ON [EopCostItems] ([OrganizationProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_InputPackageItem_InputPackageId] ON [InputPackageItem] ([InputPackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_InputPackageItem_OrganizationProductId] ON [InputPackageItem] ([OrganizationProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_InputPackages_EconomicsOfProductionId] ON [InputPackages] ([EconomicsOfProductionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_InputPackages_InputFinancingCategoryId] ON [InputPackages] ([InputFinancingCategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_InputPackages_OrganizationId] ON [InputPackages] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_InputPackages_ParentPackageId] ON [InputPackages] ([ParentPackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_InputPackages_UserWalletId] ON [InputPackages] ([UserWalletId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_OrganizationProducts_OrganizationId] ON [OrganizationProducts] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    CREATE INDEX [IX_OrganizationProducts_OrganizationProductCategoryId] ON [OrganizationProducts] ([OrganizationProductCategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    ALTER TABLE [InputAllocations] ADD CONSTRAINT [FK_InputAllocations_InputPackages_InputId] FOREIGN KEY ([InputId]) REFERENCES [InputPackages] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    ALTER TABLE [LocationInventories] ADD CONSTRAINT [FK_LocationInventories_OrganizationProducts_OrganizationProductId] FOREIGN KEY ([OrganizationProductId]) REFERENCES [OrganizationProducts] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506152633_updated input financing package'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506152633_updated input financing package', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507145548_updated organization product table'
)
BEGIN
    ALTER TABLE [InputPackageItem] DROP CONSTRAINT [FK_InputPackageItem_OrganizationProducts_OrganizationProductId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507145548_updated organization product table'
)
BEGIN
    ALTER TABLE [OrganizationProducts] ADD [Description] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507145548_updated organization product table'
)
BEGIN
    ALTER TABLE [OrganizationProducts] ADD [MaximumOrderQuantity] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507145548_updated organization product table'
)
BEGIN
    ALTER TABLE [OrganizationProducts] ADD [MinimumOrderQuantity] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507145548_updated organization product table'
)
BEGIN
    ALTER TABLE [InputPackageItem] ADD CONSTRAINT [FK_InputPackageItem_OrganizationProducts_OrganizationProductId] FOREIGN KEY ([OrganizationProductId]) REFERENCES [OrganizationProducts] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507145548_updated organization product table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507145548_updated organization product table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512172602_added inventory transaction'
)
BEGIN
    CREATE TABLE [InventoryTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [LocationInventoryId] bigint NOT NULL,
        [OrganizationProductId] bigint NOT NULL,
        [FromLocationId] bigint NULL,
        [ToLocationId] bigint NULL,
        [TransactionType] int NOT NULL,
        [Quantity] float NOT NULL,
        [QuantityType] nvarchar(max) NOT NULL,
        [QuantityBefore] float NOT NULL,
        [QuantityAfter] float NOT NULL,
        [BatchNumber] nvarchar(max) NULL,
        [Reason] nvarchar(max) NULL,
        [ReferenceNumber] nvarchar(max) NULL,
        [PerformedByUserId] nvarchar(max) NOT NULL,
        [TransactionDate] datetimeoffset NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [OrganizationId] bigint NOT NULL,
        CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryTransactions_BusinessOrganizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [BusinessOrganizations] ([OrganizationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_InventoryTransactions_LocationInventories_LocationInventoryId] FOREIGN KEY ([LocationInventoryId]) REFERENCES [LocationInventories] ([Id]),
        CONSTRAINT [FK_InventoryTransactions_OrganizationLocations_FromLocationId] FOREIGN KEY ([FromLocationId]) REFERENCES [OrganizationLocations] ([Id]),
        CONSTRAINT [FK_InventoryTransactions_OrganizationLocations_ToLocationId] FOREIGN KEY ([ToLocationId]) REFERENCES [OrganizationLocations] ([Id]),
        CONSTRAINT [FK_InventoryTransactions_OrganizationProducts_OrganizationProductId] FOREIGN KEY ([OrganizationProductId]) REFERENCES [OrganizationProducts] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512172602_added inventory transaction'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_FromLocationId] ON [InventoryTransactions] ([FromLocationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512172602_added inventory transaction'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_LocationInventoryId] ON [InventoryTransactions] ([LocationInventoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512172602_added inventory transaction'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_OrganizationId] ON [InventoryTransactions] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512172602_added inventory transaction'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_OrganizationProductId] ON [InventoryTransactions] ([OrganizationProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512172602_added inventory transaction'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_ToLocationId] ON [InventoryTransactions] ([ToLocationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512172602_added inventory transaction'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512172602_added inventory transaction', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputPackageItem] DROP CONSTRAINT [FK_InputPackageItem_InputPackages_InputPackageId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputPackageItem] DROP CONSTRAINT [FK_InputPackageItem_OrganizationProducts_OrganizationProductId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputPackages] DROP CONSTRAINT [FK_InputPackages_InputFinancingCategory_InputFinancingCategoryId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputPackages] DROP CONSTRAINT [FK_InputPackages_UserWallets_UserWalletId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputPackageItem] DROP CONSTRAINT [PK_InputPackageItem];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputFinancingCategory] DROP CONSTRAINT [PK_InputFinancingCategory];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    EXEC sp_rename N'[InputPackageItem]', N'InputPackageItems';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    EXEC sp_rename N'[InputFinancingCategory]', N'InputFinancingCategories';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    EXEC sp_rename N'[InputPackageItems].[IX_InputPackageItem_OrganizationProductId]', N'IX_InputPackageItems_OrganizationProductId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    EXEC sp_rename N'[InputPackageItems].[IX_InputPackageItem_InputPackageId]', N'IX_InputPackageItems_InputPackageId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    DECLARE @var135 sysname;
    SELECT @var135 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'UserWalletId');
    IF @var135 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var135 + '];');
    ALTER TABLE [InputPackages] ALTER COLUMN [UserWalletId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputFinancingCategories] ADD [BusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputFinancingCategories] ADD [CreatedOn] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputPackageItems] ADD CONSTRAINT [PK_InputPackageItems] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputFinancingCategories] ADD CONSTRAINT [PK_InputFinancingCategories] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputPackageItems] ADD CONSTRAINT [FK_InputPackageItems_InputPackages_InputPackageId] FOREIGN KEY ([InputPackageId]) REFERENCES [InputPackages] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputPackageItems] ADD CONSTRAINT [FK_InputPackageItems_OrganizationProducts_OrganizationProductId] FOREIGN KEY ([OrganizationProductId]) REFERENCES [OrganizationProducts] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputPackages] ADD CONSTRAINT [FK_InputPackages_InputFinancingCategories_InputFinancingCategoryId] FOREIGN KEY ([InputFinancingCategoryId]) REFERENCES [InputFinancingCategories] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    ALTER TABLE [InputPackages] ADD CONSTRAINT [FK_InputPackages_UserWallets_UserWalletId] FOREIGN KEY ([UserWalletId]) REFERENCES [UserWallets] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514162022_update input package category'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260514162022_update input package category', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515131052_updated organization location'
)
BEGIN
    DECLARE @var136 sysname;
    SELECT @var136 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrganizationLocations]') AND [c].[name] = N'LocationStatus');
    IF @var136 IS NOT NULL EXEC(N'ALTER TABLE [OrganizationLocations] DROP CONSTRAINT [' + @var136 + '];');
    ALTER TABLE [OrganizationLocations] ALTER COLUMN [LocationStatus] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515131052_updated organization location'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD [WardId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515131052_updated organization location'
)
BEGIN
    CREATE INDEX [IX_OrganizationLocations_WardId] ON [OrganizationLocations] ([WardId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515131052_updated organization location'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD CONSTRAINT [FK_OrganizationLocations_Wards_WardId] FOREIGN KEY ([WardId]) REFERENCES [Wards] ([WardId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515131052_updated organization location'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260515131052_updated organization location', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518104447_updated inventory table'
)
BEGIN
    DECLARE @var137 sysname;
    SELECT @var137 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LocationInventories]') AND [c].[name] = N'InventoryOwner');
    IF @var137 IS NOT NULL EXEC(N'ALTER TABLE [LocationInventories] DROP CONSTRAINT [' + @var137 + '];');
    ALTER TABLE [LocationInventories] ALTER COLUMN [InventoryOwner] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518104447_updated inventory table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260518104447_updated inventory table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518110032_added audit trail for delete action'
)
BEGIN
    ALTER TABLE [LocationInventories] ADD [DeleteReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518110032_added audit trail for delete action'
)
BEGIN
    ALTER TABLE [LocationInventories] ADD [DeletedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518110032_added audit trail for delete action'
)
BEGIN
    ALTER TABLE [LocationInventories] ADD [DeletedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518110032_added audit trail for delete action'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260518110032_added audit trail for delete action', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] DROP CONSTRAINT [FK_BusinessOrganizations_LGAs_LgaId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] DROP CONSTRAINT [FK_BusinessOrganizations_States_StateId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [EconomicsOfProductions] DROP CONSTRAINT [FK_EconomicsOfProductions_BusinessOrganizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [InputPackages] DROP CONSTRAINT [FK_InputPackages_BusinessOrganizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [InventoryTransactions] DROP CONSTRAINT [FK_InventoryTransactions_BusinessOrganizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationAgents] DROP CONSTRAINT [FK_OrganizationAgents_BusinessOrganizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationConfig] DROP CONSTRAINT [FK_OrganizationConfig_BusinessOrganizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationLocations] DROP CONSTRAINT [FK_OrganizationLocations_BusinessOrganizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationMemberships] DROP CONSTRAINT [FK_OrganizationMemberships_BusinessOrganizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationProducts] DROP CONSTRAINT [FK_OrganizationProducts_BusinessOrganizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationRoles] DROP CONSTRAINT [FK_OrganizationRoles_BusinessOrganizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [BusinessOrganizations] DROP CONSTRAINT [PK_BusinessOrganizations];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    EXEC sp_rename N'[BusinessOrganizations]', N'Organizations';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    EXEC sp_rename N'[Organizations].[IX_BusinessOrganizations_StateId]', N'IX_Organizations_StateId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    EXEC sp_rename N'[Organizations].[IX_BusinessOrganizations_LgaId]', N'IX_Organizations_LgaId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Businesses] ADD [CustomDomain] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Businesses] ADD [EmailSenderName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Businesses] ADD [FaviconUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Businesses] ADD [IsSuspended] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Businesses] ADD [LogoUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Businesses] ADD [SecondaryColor] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Businesses] ADD [SuspensionReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Businesses] ADD [ThemeColor] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Organizations] ADD CONSTRAINT [PK_Organizations] PRIMARY KEY ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE TABLE [BusinessBillingInvoices] (
        [Id] bigint NOT NULL IDENTITY,
        [BusinessId] bigint NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [DueDate] datetimeoffset NOT NULL,
        CONSTRAINT [PK_BusinessBillingInvoices] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE TABLE [BusinessLicenses] (
        [Id] bigint NOT NULL IDENTITY,
        [BusinessId] bigint NOT NULL,
        [LicenseKey] nvarchar(max) NOT NULL,
        [LicenseType] nvarchar(max) NOT NULL,
        [StartDate] datetimeoffset NOT NULL,
        [EndDate] datetimeoffset NOT NULL,
        [DurationInMonths] int NOT NULL,
        [IsActive] bit NOT NULL,
        [GracePeriodDays] int NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [AutoRenew] bit NOT NULL,
        [PaymentReference] nvarchar(max) NULL,
        [DeactivatedReason] nvarchar(max) NULL,
        CONSTRAINT [PK_BusinessLicenses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BusinessLicenses_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([BusinessId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE TABLE [MigrationTrackers] (
        [Id] bigint NOT NULL IDENTITY,
        [BusinessId] bigint NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Completed] bit NOT NULL,
        [RunOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_MigrationTrackers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE TABLE [OrganizationBusinesses] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationId] bigint NOT NULL,
        [BusinessId] bigint NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_OrganizationBusinesses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationBusinesses_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([BusinessId]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrganizationBusinesses_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE TABLE [SystemModules] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Code] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Icon] nvarchar(max) NULL,
        [Route] nvarchar(max) NULL,
        [DisplayOrder] int NOT NULL,
        [RequiresSubscription] bit NOT NULL,
        CONSTRAINT [PK_SystemModules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE TABLE [UserBusinesses] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [BusinessId] bigint NOT NULL,
        [Role] nvarchar(max) NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [JoinedOn] datetimeoffset NOT NULL,
        [InvitedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_UserBusinesses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserBusinesses_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserBusinesses_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([BusinessId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE TABLE [BusinessPayments] (
        [Id] bigint NOT NULL IDENTITY,
        [BusinessBillingInvoiceId] bigint NOT NULL,
        [AmountPaid] decimal(18,2) NOT NULL,
        [PaymentReference] nvarchar(max) NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [PaidOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_BusinessPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BusinessPayments_BusinessBillingInvoices_BusinessBillingInvoiceId] FOREIGN KEY ([BusinessBillingInvoiceId]) REFERENCES [BusinessBillingInvoices] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE TABLE [BusinessLicenseModules] (
        [Id] bigint NOT NULL IDENTITY,
        [BusinessLicenseId] bigint NOT NULL,
        [SystemModuleId] bigint NOT NULL,
        [IsEnabled] bit NOT NULL,
        [EnabledOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_BusinessLicenseModules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BusinessLicenseModules_BusinessLicenses_BusinessLicenseId] FOREIGN KEY ([BusinessLicenseId]) REFERENCES [BusinessLicenses] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BusinessLicenseModules_SystemModules_SystemModuleId] FOREIGN KEY ([SystemModuleId]) REFERENCES [SystemModules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE INDEX [IX_BusinessLicenseModules_BusinessLicenseId] ON [BusinessLicenseModules] ([BusinessLicenseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE INDEX [IX_BusinessLicenseModules_SystemModuleId] ON [BusinessLicenseModules] ([SystemModuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE INDEX [IX_BusinessLicenses_BusinessId] ON [BusinessLicenses] ([BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE INDEX [IX_BusinessPayments_BusinessBillingInvoiceId] ON [BusinessPayments] ([BusinessBillingInvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE INDEX [IX_OrganizationBusinesses_BusinessId] ON [OrganizationBusinesses] ([BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE INDEX [IX_OrganizationBusinesses_OrganizationId] ON [OrganizationBusinesses] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE INDEX [IX_UserBusinesses_BusinessId] ON [UserBusinesses] ([BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    CREATE INDEX [IX_UserBusinesses_UserId] ON [UserBusinesses] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [EconomicsOfProductions] ADD CONSTRAINT [FK_EconomicsOfProductions_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [InputPackages] ADD CONSTRAINT [FK_InputPackages_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD CONSTRAINT [FK_InventoryTransactions_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationAgents] ADD CONSTRAINT [FK_OrganizationAgents_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationLocations] ADD CONSTRAINT [FK_OrganizationLocations_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationMemberships] ADD CONSTRAINT [FK_OrganizationMemberships_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationProducts] ADD CONSTRAINT [FK_OrganizationProducts_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationRoles] ADD CONSTRAINT [FK_OrganizationRoles_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Organizations] ADD CONSTRAINT [FK_Organizations_LGAs_LgaId] FOREIGN KEY ([LgaId]) REFERENCES [LGAs] ([LgaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [Organizations] ADD CONSTRAINT [FK_Organizations_States_StateId] FOREIGN KEY ([StateId]) REFERENCES [States] ([StateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    ALTER TABLE [OrganizationConfig] ADD CONSTRAINT [FK_OrganizationConfig_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520115517_added multi tenancy arch'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260520115517_added multi tenancy arch', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    EXEC sp_rename N'[InputPackages].[Status]', N'RepaymentFrequency', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    EXEC sp_rename N'[InputPackages].[Season]', N'InputTarget', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    EXEC sp_rename N'[InputPackages].[RepaymentPeriodInMonths]', N'RepaymentDuration', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    DECLARE @var138 sysname;
    SELECT @var138 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'CreatedByUserId');
    IF @var138 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var138 + '];');
    ALTER TABLE [InputPackages] ALTER COLUMN [CreatedByUserId] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [AdditionalCostAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [CommitmentPaymentEndDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [CommitmentPaymentStartDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [InputSeason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [IsSelectedWalletProvisioned] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [RepaymentEndDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [RepaymentGracePeriod] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [RepaymentStartDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [TotalFinancingValue] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [WalletProviderId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    DECLARE @var139 sysname;
    SELECT @var139 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackageItems]') AND [c].[name] = N'UnitOfMeasure');
    IF @var139 IS NOT NULL EXEC(N'ALTER TABLE [InputPackageItems] DROP CONSTRAINT [' + @var139 + '];');
    ALTER TABLE [InputPackageItems] ALTER COLUMN [UnitOfMeasure] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    CREATE INDEX [IX_InputPackages_WalletProviderId] ON [InputPackages] ([WalletProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    ALTER TABLE [InputPackages] ADD CONSTRAINT [FK_InputPackages_WalletProviders_WalletProviderId] FOREIGN KEY ([WalletProviderId]) REFERENCES [WalletProviders] ([ProviderId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601124419_updated input financing configs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260601124419_updated input financing configs', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601140935_added outbox message'
)
BEGIN
    CREATE TABLE [OutboxMessages] (
        [Id] uniqueidentifier NOT NULL,
        [EventType] nvarchar(max) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ProcessedOn] datetime2 NULL,
        [IsProcessed] bit NOT NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601140935_added outbox message'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260601140935_added outbox message', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601220900_updated nullables in InputPackage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260601220900_updated nullables in InputPackage', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602003221_added repaymentschedule engine config'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [UpdatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602003221_added repaymentschedule engine config'
)
BEGIN
    CREATE TABLE [RepaymentSchedules] (
        [Id] bigint NOT NULL IDENTITY,
        [InputPackageId] bigint NOT NULL,
        [InstallmentNumber] int NOT NULL,
        [DueDate] datetimeoffset NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [IsWithinGracePeriod] bit NOT NULL,
        [IsPaid] bit NOT NULL,
        [PaidOn] datetimeoffset NULL,
        CONSTRAINT [PK_RepaymentSchedules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602003221_added repaymentschedule engine config'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260602003221_added repaymentschedule engine config', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    DROP TABLE [InputAllocations];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    DROP TABLE [InputTransactions];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    EXEC sp_rename N'[RepaymentSchedules].[InputPackageId]', N'InputPackageSubscriptionId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [AmountPaid] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [IsOverdue] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [PenaltyAccrued] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [PenaltyPaid] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    DECLARE @var140 sysname;
    SELECT @var140 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrganizationLocations]') AND [c].[name] = N'StockLevel');
    IF @var140 IS NOT NULL EXEC(N'ALTER TABLE [OrganizationLocations] DROP CONSTRAINT [' + @var140 + '];');
    ALTER TABLE [OrganizationLocations] ALTER COLUMN [StockLevel] decimal(18,2) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    DECLARE @var141 sysname;
    SELECT @var141 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrganizationLocations]') AND [c].[name] = N'Capacity');
    IF @var141 IS NOT NULL EXEC(N'ALTER TABLE [OrganizationLocations] DROP CONSTRAINT [' + @var141 + '];');
    ALTER TABLE [OrganizationLocations] ALTER COLUMN [Capacity] decimal(18,2) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    DECLARE @var142 sysname;
    SELECT @var142 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LocationInventories]') AND [c].[name] = N'Quantity');
    IF @var142 IS NOT NULL EXEC(N'ALTER TABLE [LocationInventories] DROP CONSTRAINT [' + @var142 + '];');
    ALTER TABLE [LocationInventories] ALTER COLUMN [Quantity] decimal(18,2) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    DECLARE @var143 sysname;
    SELECT @var143 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventoryTransactions]') AND [c].[name] = N'QuantityBefore');
    IF @var143 IS NOT NULL EXEC(N'ALTER TABLE [InventoryTransactions] DROP CONSTRAINT [' + @var143 + '];');
    ALTER TABLE [InventoryTransactions] ALTER COLUMN [QuantityBefore] decimal(18,2) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    DECLARE @var144 sysname;
    SELECT @var144 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventoryTransactions]') AND [c].[name] = N'QuantityAfter');
    IF @var144 IS NOT NULL EXEC(N'ALTER TABLE [InventoryTransactions] DROP CONSTRAINT [' + @var144 + '];');
    ALTER TABLE [InventoryTransactions] ALTER COLUMN [QuantityAfter] decimal(18,2) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    DECLARE @var145 sysname;
    SELECT @var145 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventoryTransactions]') AND [c].[name] = N'Quantity');
    IF @var145 IS NOT NULL EXEC(N'ALTER TABLE [InventoryTransactions] DROP CONSTRAINT [' + @var145 + '];');
    ALTER TABLE [InventoryTransactions] ALTER COLUMN [Quantity] decimal(18,2) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD [SubscriptionId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [BusinessCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [InterestRate] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    ALTER TABLE [EconomicsOfProductions] ADD [BusinessCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    CREATE TABLE [InputPackageSubscriptions] (
        [Id] bigint NOT NULL IDENTITY,
        [InputPackageId] bigint NOT NULL,
        [OrganizationId] bigint NULL,
        [SubscriberUserId] nvarchar(max) NOT NULL,
        [AddedByUserId] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [SubscriberType] int NOT NULL,
        [SubscribedOn] datetimeoffset NOT NULL,
        [IsActive] bit NOT NULL,
        [RepaymentFrequency] int NOT NULL,
        [RepaymentDuration] int NOT NULL,
        [RepaymentStartDate] datetimeoffset NULL,
        [RepaymentEndDate] datetimeoffset NULL,
        [RepaymentGracePeriod] datetimeoffset NULL,
        [RepaymentMode] int NOT NULL,
        [TotalFinancingValue] decimal(18,2) NOT NULL,
        [CommitmentRequired] bit NOT NULL,
        [CommitmentValue] decimal(18,2) NOT NULL,
        [CommitmentPaymentStartDate] datetimeoffset NULL,
        [CommitmentPaymentEndDate] datetimeoffset NULL,
        [ApprovedByUserId] nvarchar(max) NULL,
        [ApprovedOn] datetimeoffset NULL,
        [RejectedByUserId] nvarchar(max) NULL,
        [RejectedOn] datetimeoffset NULL,
        [RejectionReason] nvarchar(max) NULL,
        [DisbursedOn] datetimeoffset NULL,
        [CompletedOn] datetimeoffset NULL,
        [RiskCategory] int NOT NULL,
        [LastOverdueDetectedOn] datetimeoffset NULL,
        [OverdueCount] int NOT NULL,
        [RiskLevel] int NOT NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_InputPackageSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InputPackageSubscriptions_InputPackages_InputPackageId] FOREIGN KEY ([InputPackageId]) REFERENCES [InputPackages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InputPackageSubscriptions_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    CREATE TABLE [InputSubscriptionDisbursements] (
        [Id] bigint NOT NULL IDENTITY,
        [SubscriptionId] bigint NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Reference] nvarchar(max) NOT NULL,
        [DisbursedOn] datetimeoffset NOT NULL,
        [DisbursedByUserId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_InputSubscriptionDisbursements] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    CREATE TABLE [PenaltyPolicies] (
        [Id] bigint NOT NULL IDENTITY,
        [InputPackageId] bigint NOT NULL,
        [FlatFee] decimal(18,2) NOT NULL,
        [DailyInterestRate] decimal(18,2) NOT NULL,
        [MaxPenaltyCap] decimal(18,2) NOT NULL,
        [GracePeriodDays] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_PenaltyPolicies] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    CREATE TABLE [RepaymentPenalties] (
        [Id] bigint NOT NULL IDENTITY,
        [SubscriptionId] bigint NOT NULL,
        [RepaymentScheduleId] bigint NOT NULL,
        [FlatFeeApplied] decimal(18,2) NOT NULL,
        [InterestApplied] decimal(18,2) NOT NULL,
        [TotalPenalty] decimal(18,2) NOT NULL,
        [CalculatedOn] datetimeoffset NOT NULL,
        [IsReversed] bit NOT NULL,
        CONSTRAINT [PK_RepaymentPenalties] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    CREATE TABLE [SubscriptionPaymentAllocations] (
        [Id] bigint NOT NULL IDENTITY,
        [InputPackageSubscriptionPaymentId] bigint NOT NULL,
        [RepaymentScheduleId] bigint NULL,
        [PenaltyPortion] decimal(18,2) NOT NULL,
        [InterestPortion] decimal(18,2) NOT NULL,
        [PrincipalPortion] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_SubscriptionPaymentAllocations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    CREATE TABLE [InputPackageSubscriptionPayments] (
        [Id] bigint NOT NULL IDENTITY,
        [SubscriptionId] bigint NOT NULL,
        [PaymentType] int NOT NULL,
        [AmountPaid] decimal(18,2) NOT NULL,
        [PaymentChannel] int NOT NULL,
        [Status] int NOT NULL,
        [Reference] nvarchar(max) NOT NULL,
        [PaidOn] datetimeoffset NOT NULL,
        [ExternalReference] nvarchar(max) NULL,
        CONSTRAINT [PK_InputPackageSubscriptionPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InputPackageSubscriptionPayments_InputPackageSubscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [InputPackageSubscriptions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    CREATE INDEX [IX_RepaymentSchedules_InputPackageSubscriptionId] ON [RepaymentSchedules] ([InputPackageSubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    CREATE INDEX [IX_InputPackageSubscriptionPayments_SubscriptionId] ON [InputPackageSubscriptionPayments] ([SubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    CREATE INDEX [IX_InputPackageSubscriptions_InputPackageId] ON [InputPackageSubscriptions] ([InputPackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    CREATE INDEX [IX_InputPackageSubscriptions_OrganizationId] ON [InputPackageSubscriptions] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD CONSTRAINT [FK_RepaymentSchedules_InputPackageSubscriptions_InputPackageSubscriptionId] FOREIGN KEY ([InputPackageSubscriptionId]) REFERENCES [InputPackageSubscriptions] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603004416_input financing subscription'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603004416_input financing subscription', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603091921_input financing subscription businesscode'
)
BEGIN
    ALTER TABLE [InputPackages] DROP CONSTRAINT [FK_InputPackages_Organizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603091921_input financing subscription businesscode'
)
BEGIN
    DECLARE @var146 sysname;
    SELECT @var146 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'OrganizationId');
    IF @var146 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var146 + '];');
    ALTER TABLE [InputPackages] ALTER COLUMN [OrganizationId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603091921_input financing subscription businesscode'
)
BEGIN
    ALTER TABLE [InputPackages] ADD CONSTRAINT [FK_InputPackages_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603091921_input financing subscription businesscode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603091921_input financing subscription businesscode', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603092552_changed tier'
)
BEGIN
    EXEC sp_rename N'[MemberShips].[Tier]', N'TierLevel', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603092552_changed tier'
)
BEGIN
    EXEC sp_rename N'[Agents].[Tier]', N'TierLevel', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603092552_changed tier'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603092552_changed tier', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    DECLARE @var147 sysname;
    SELECT @var147 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackageSubscriptions]') AND [c].[name] = N'SubscriberUserId');
    IF @var147 IS NOT NULL EXEC(N'ALTER TABLE [InputPackageSubscriptions] DROP CONSTRAINT [' + @var147 + '];');
    ALTER TABLE [InputPackageSubscriptions] ALTER COLUMN [SubscriberUserId] nvarchar(450) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    DECLARE @var148 sysname;
    SELECT @var148 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackageSubscriptions]') AND [c].[name] = N'AddedByUserId');
    IF @var148 IS NOT NULL EXEC(N'ALTER TABLE [InputPackageSubscriptions] DROP CONSTRAINT [' + @var148 + '];');
    ALTER TABLE [InputPackageSubscriptions] ALTER COLUMN [AddedByUserId] nvarchar(450) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [CreatedById] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [UpdatedById] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    CREATE INDEX [IX_InputPackageSubscriptions_AddedByUserId] ON [InputPackageSubscriptions] ([AddedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    CREATE INDEX [IX_InputPackageSubscriptions_SubscriberUserId] ON [InputPackageSubscriptions] ([SubscriberUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    CREATE INDEX [IX_InputPackages_CreatedById] ON [InputPackages] ([CreatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    CREATE INDEX [IX_InputPackages_UpdatedById] ON [InputPackages] ([UpdatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    ALTER TABLE [InputPackages] ADD CONSTRAINT [FK_InputPackages_AspNetUsers_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [AspNetUsers] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    ALTER TABLE [InputPackages] ADD CONSTRAINT [FK_InputPackages_AspNetUsers_UpdatedById] FOREIGN KEY ([UpdatedById]) REFERENCES [AspNetUsers] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    ALTER TABLE [InputPackageSubscriptions] ADD CONSTRAINT [FK_InputPackageSubscriptions_AspNetUsers_AddedByUserId] FOREIGN KEY ([AddedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    ALTER TABLE [InputPackageSubscriptions] ADD CONSTRAINT [FK_InputPackageSubscriptions_AspNetUsers_SubscriberUserId] FOREIGN KEY ([SubscriberUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603113642_added navigation property'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603113642_added navigation property', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    DECLARE @var149 sysname;
    SELECT @var149 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'InterestRate');
    IF @var149 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var149 + '];');
    EXEC(N'UPDATE [InputPackages] SET [InterestRate] = 0.0 WHERE [InterestRate] IS NULL');
    ALTER TABLE [InputPackages] ALTER COLUMN [InterestRate] decimal(18,2) NOT NULL;
    ALTER TABLE [InputPackages] ADD DEFAULT 0.0 FOR [InterestRate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [AlignToDisbursement] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [ApplyInterestOnDefault] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [DefaultPenaltyRate] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [Duration] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [Frequency] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [InterestType] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [PatternType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    ALTER TABLE [InputPackages] ADD [RepaymeRequired] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    CREATE TABLE [CommitmentPlans] (
        [Id] bigint NOT NULL IDENTITY,
        [InputPackageId] bigint NOT NULL,
        [CommitmentType] int NOT NULL,
        [CommitmentValue] decimal(18,2) NOT NULL,
        [CommitmentPaymentStartDate] datetimeoffset NULL,
        [CommitmentPaymentEndDate] datetimeoffset NULL,
        CONSTRAINT [PK_CommitmentPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CommitmentPlans_InputPackages_InputPackageId] FOREIGN KEY ([InputPackageId]) REFERENCES [InputPackages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    CREATE TABLE [RepaymentPlans] (
        [Id] bigint NOT NULL IDENTITY,
        [InputPackageId] bigint NOT NULL,
        [RepaymentValue] decimal(18,2) NOT NULL,
        [ScheduleType] int NOT NULL,
        [Frequency] int NOT NULL,
        [RepaymentFrequency] int NOT NULL,
        [RepaymentDuration] int NOT NULL,
        [RepaymentMode] int NOT NULL,
        [RiskCategory] int NOT NULL,
        [RepaymentStartDate] datetimeoffset NULL,
        [RepaymentEndDate] datetimeoffset NULL,
        [RepaymentGracePeriod] datetimeoffset NULL,
        [Duration] int NOT NULL,
        [PatternType] nvarchar(max) NULL,
        [AlignToDisbursement] bit NOT NULL,
        [OrganizationId] bigint NULL,
        [BusinessCode] nvarchar(max) NOT NULL,
        [InterestRate] decimal(18,2) NOT NULL,
        [InterestType] int NOT NULL,
        [ApplyInterestOnDefault] bit NOT NULL,
        [DefaultPenaltyRate] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_RepaymentPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RepaymentPlans_InputPackages_InputPackageId] FOREIGN KEY ([InputPackageId]) REFERENCES [InputPackages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CommitmentPlans_InputPackageId] ON [CommitmentPlans] ([InputPackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RepaymentPlans_InputPackageId] ON [RepaymentPlans] ([InputPackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604150839_split commitment and repayment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604150839_split commitment and repayment', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var150 sysname;
    SELECT @var150 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RepaymentPlans]') AND [c].[name] = N'BusinessCode');
    IF @var150 IS NOT NULL EXEC(N'ALTER TABLE [RepaymentPlans] DROP CONSTRAINT [' + @var150 + '];');
    ALTER TABLE [RepaymentPlans] DROP COLUMN [BusinessCode];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var151 sysname;
    SELECT @var151 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RepaymentPlans]') AND [c].[name] = N'Duration');
    IF @var151 IS NOT NULL EXEC(N'ALTER TABLE [RepaymentPlans] DROP CONSTRAINT [' + @var151 + '];');
    ALTER TABLE [RepaymentPlans] DROP COLUMN [Duration];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var152 sysname;
    SELECT @var152 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RepaymentPlans]') AND [c].[name] = N'OrganizationId');
    IF @var152 IS NOT NULL EXEC(N'ALTER TABLE [RepaymentPlans] DROP CONSTRAINT [' + @var152 + '];');
    ALTER TABLE [RepaymentPlans] DROP COLUMN [OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var153 sysname;
    SELECT @var153 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'AlignToDisbursement');
    IF @var153 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var153 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [AlignToDisbursement];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var154 sysname;
    SELECT @var154 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'ApplyInterestOnDefault');
    IF @var154 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var154 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [ApplyInterestOnDefault];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var155 sysname;
    SELECT @var155 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'CommitmentPaymentEndDate');
    IF @var155 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var155 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [CommitmentPaymentEndDate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var156 sysname;
    SELECT @var156 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'CommitmentPaymentStartDate');
    IF @var156 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var156 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [CommitmentPaymentStartDate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var157 sysname;
    SELECT @var157 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'CommitmentType');
    IF @var157 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var157 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [CommitmentType];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var158 sysname;
    SELECT @var158 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'CommitmentValue');
    IF @var158 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var158 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [CommitmentValue];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var159 sysname;
    SELECT @var159 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'DefaultPenaltyRate');
    IF @var159 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var159 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [DefaultPenaltyRate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var160 sysname;
    SELECT @var160 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'Duration');
    IF @var160 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var160 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [Duration];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var161 sysname;
    SELECT @var161 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'Frequency');
    IF @var161 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var161 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [Frequency];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var162 sysname;
    SELECT @var162 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'InterestRate');
    IF @var162 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var162 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [InterestRate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var163 sysname;
    SELECT @var163 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'InterestType');
    IF @var163 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var163 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [InterestType];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var164 sysname;
    SELECT @var164 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'PatternType');
    IF @var164 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var164 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [PatternType];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var165 sysname;
    SELECT @var165 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'RepaymentDuration');
    IF @var165 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var165 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [RepaymentDuration];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var166 sysname;
    SELECT @var166 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'RepaymentEndDate');
    IF @var166 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var166 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [RepaymentEndDate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var167 sysname;
    SELECT @var167 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'RepaymentFrequency');
    IF @var167 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var167 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [RepaymentFrequency];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var168 sysname;
    SELECT @var168 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'RepaymentGracePeriod');
    IF @var168 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var168 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [RepaymentGracePeriod];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var169 sysname;
    SELECT @var169 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'RepaymentMode');
    IF @var169 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var169 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [RepaymentMode];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var170 sysname;
    SELECT @var170 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'RepaymentStartDate');
    IF @var170 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var170 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [RepaymentStartDate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var171 sysname;
    SELECT @var171 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'RepaymentValue');
    IF @var171 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var171 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [RepaymentValue];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var172 sysname;
    SELECT @var172 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'RiskCategory');
    IF @var172 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var172 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [RiskCategory];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    DECLARE @var173 sysname;
    SELECT @var173 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'ScheduleType');
    IF @var173 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var173 + '];');
    ALTER TABLE [InputPackages] DROP COLUMN [ScheduleType];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    EXEC sp_rename N'[InputPackages].[RepaymeRequired]', N'RepaymentRequired', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604192007_dropped tables on input package'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604192007_dropped tables on input package', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InventoryTransactions] DROP CONSTRAINT [FK_InventoryTransactions_OrganizationProducts_OrganizationProductId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    DECLARE @var174 sysname;
    SELECT @var174 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputSubscriptionDisbursements]') AND [c].[name] = N'Amount');
    IF @var174 IS NOT NULL EXEC(N'ALTER TABLE [InputSubscriptionDisbursements] DROP CONSTRAINT [' + @var174 + '];');
    ALTER TABLE [InputSubscriptionDisbursements] DROP COLUMN [Amount];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    DECLARE @var175 sysname;
    SELECT @var175 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventoryTransactions]') AND [c].[name] = N'OrganizationProductId');
    IF @var175 IS NOT NULL EXEC(N'ALTER TABLE [InventoryTransactions] DROP CONSTRAINT [' + @var175 + '];');
    ALTER TABLE [InventoryTransactions] ALTER COLUMN [OrganizationProductId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD [Notes] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    DECLARE @var176 sysname;
    SELECT @var176 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputSubscriptionDisbursements]') AND [c].[name] = N'Reference');
    IF @var176 IS NOT NULL EXEC(N'ALTER TABLE [InputSubscriptionDisbursements] DROP CONSTRAINT [' + @var176 + '];');
    ALTER TABLE [InputSubscriptionDisbursements] ALTER COLUMN [Reference] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    DECLARE @var177 sysname;
    SELECT @var177 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputSubscriptionDisbursements]') AND [c].[name] = N'DisbursedOn');
    IF @var177 IS NOT NULL EXEC(N'ALTER TABLE [InputSubscriptionDisbursements] DROP CONSTRAINT [' + @var177 + '];');
    ALTER TABLE [InputSubscriptionDisbursements] ALTER COLUMN [DisbursedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    DECLARE @var178 sysname;
    SELECT @var178 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputSubscriptionDisbursements]') AND [c].[name] = N'DisbursedByUserId');
    IF @var178 IS NOT NULL EXEC(N'ALTER TABLE [InputSubscriptionDisbursements] DROP CONSTRAINT [' + @var178 + '];');
    ALTER TABLE [InputSubscriptionDisbursements] ALTER COLUMN [DisbursedByUserId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [ApprovedAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [ApprovedByUserId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [ApprovedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [DisbursedAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [RemainingBalance] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [RequestByUserId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [RequestCreatedOn] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [RequestedAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [RequestedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [Status] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    DECLARE @var179 sysname;
    SELECT @var179 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackageSubscriptionPayments]') AND [c].[name] = N'PaidOn');
    IF @var179 IS NOT NULL EXEC(N'ALTER TABLE [InputPackageSubscriptionPayments] DROP CONSTRAINT [' + @var179 + '];');
    ALTER TABLE [InputPackageSubscriptionPayments] ALTER COLUMN [PaidOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputPackageSubscriptionPayments] ADD [CreatedOn] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputPackageSubscriptionPayments] ADD [RecordedByUserId] nvarchar(450) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputPackageSubscriptionPayments] ADD [TransactionResponse] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputPackageSubscriptionPayments] ADD [WalletProviderId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [AdditionalInputPackageCosts] ADD [AdditionalCostTreatmentType] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE TABLE [PaymentReconciliation] (
        [Id] bigint NOT NULL IDENTITY,
        [PaymentId] bigint NOT NULL,
        [ReviewedByUserId] nvarchar(max) NOT NULL,
        [IsConfirmed] bit NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [ReviewedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_PaymentReconciliation] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentReconciliation_InputPackageSubscriptionPayments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [InputPackageSubscriptionPayments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE TABLE [ProviderWalletTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [Reference] nvarchar(max) NOT NULL,
        [Provider] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [SubscriberWalletNumber] nvarchar(max) NOT NULL,
        [BusinessWalletNumber] nvarchar(max) NOT NULL,
        [RecipientWalletNumber] nvarchar(max) NOT NULL,
        [DebitReference] nvarchar(max) NOT NULL,
        [CreditReference] nvarchar(max) NOT NULL,
        [ProviderTransactionId] nvarchar(max) NOT NULL,
        [FailureReason] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        CONSTRAINT [PK_ProviderWalletTransactions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE TABLE [SubscriptionDocuments] (
        [Id] bigint NOT NULL IDENTITY,
        [SubscriptionId] bigint NOT NULL,
        [DocumentType] int NOT NULL,
        [UploadedOn] datetimeoffset NOT NULL,
        [UploadedBy] nvarchar(max) NOT NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [FileName] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_SubscriptionDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubscriptionDocuments_InputPackageSubscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [InputPackageSubscriptions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE TABLE [ProviderWalletTransactionLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ProviderWalletTransactionId] bigint NOT NULL,
        [Step] nvarchar(max) NOT NULL,
        [RequestPayload] nvarchar(max) NOT NULL,
        [ResponsePayload] nvarchar(max) NOT NULL,
        [IsSuccess] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProviderWalletTransactionLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProviderWalletTransactionLogs_ProviderWalletTransactions_ProviderWalletTransactionId] FOREIGN KEY ([ProviderWalletTransactionId]) REFERENCES [ProviderWalletTransactions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE INDEX [IX_InputSubscriptionDisbursements_ApprovedByUserId] ON [InputSubscriptionDisbursements] ([ApprovedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE INDEX [IX_InputSubscriptionDisbursements_DisbursedByUserId] ON [InputSubscriptionDisbursements] ([DisbursedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE INDEX [IX_InputSubscriptionDisbursements_RequestByUserId] ON [InputSubscriptionDisbursements] ([RequestByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE INDEX [IX_InputSubscriptionDisbursements_SubscriptionId] ON [InputSubscriptionDisbursements] ([SubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE INDEX [IX_InputPackageSubscriptionPayments_RecordedByUserId] ON [InputPackageSubscriptionPayments] ([RecordedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE INDEX [IX_InputPackageSubscriptionPayments_WalletProviderId] ON [InputPackageSubscriptionPayments] ([WalletProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE INDEX [IX_PaymentReconciliation_PaymentId] ON [PaymentReconciliation] ([PaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE INDEX [IX_ProviderWalletTransactionLogs_ProviderWalletTransactionId] ON [ProviderWalletTransactionLogs] ([ProviderWalletTransactionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    CREATE INDEX [IX_SubscriptionDocuments_SubscriptionId] ON [SubscriptionDocuments] ([SubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputPackageSubscriptionPayments] ADD CONSTRAINT [FK_InputPackageSubscriptionPayments_AspNetUsers_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputPackageSubscriptionPayments] ADD CONSTRAINT [FK_InputPackageSubscriptionPayments_WalletProviders_WalletProviderId] FOREIGN KEY ([WalletProviderId]) REFERENCES [WalletProviders] ([ProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD CONSTRAINT [FK_InputSubscriptionDisbursements_AspNetUsers_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [AspNetUsers] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD CONSTRAINT [FK_InputSubscriptionDisbursements_AspNetUsers_DisbursedByUserId] FOREIGN KEY ([DisbursedByUserId]) REFERENCES [AspNetUsers] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD CONSTRAINT [FK_InputSubscriptionDisbursements_AspNetUsers_RequestByUserId] FOREIGN KEY ([RequestByUserId]) REFERENCES [AspNetUsers] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD CONSTRAINT [FK_InputSubscriptionDisbursements_InputPackageSubscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [InputPackageSubscriptions] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD CONSTRAINT [FK_InventoryTransactions_OrganizationProducts_OrganizationProductId] FOREIGN KEY ([OrganizationProductId]) REFERENCES [OrganizationProducts] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610113601_InputPackage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260610113601_InputPackage', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    DROP INDEX [IX_FarmerOrganizations_FarmerId] ON [FarmerOrganizations];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    DROP INDEX [IX_FarmerCooperatives_FarmerId] ON [FarmerCooperatives];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    DROP INDEX [IX_FarmerAgents_FarmerId] ON [FarmerAgents];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    DECLARE @var180 sysname;
    SELECT @var180 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FarmerOrganizations]') AND [c].[name] = N'OrganizationCode');
    IF @var180 IS NOT NULL EXEC(N'ALTER TABLE [FarmerOrganizations] DROP CONSTRAINT [' + @var180 + '];');
    ALTER TABLE [FarmerOrganizations] ALTER COLUMN [OrganizationCode] nvarchar(450) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    ALTER TABLE [CommitmentPlans] ADD [CommitmentPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    DECLARE @var181 sysname;
    SELECT @var181 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'PhoneNumber');
    IF @var181 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var181 + '];');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [PhoneNumber] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN

        UPDATE AspNetUsers
        SET PhoneNumber = NULL
        WHERE PhoneNumber = '';

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsLeaderProfileComplete] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    CREATE INDEX [IX_FarmerOrganizations_FarmerId_OrganizationCode] ON [FarmerOrganizations] ([FarmerId], [OrganizationCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    CREATE INDEX [IX_FarmerCooperatives_FarmerId_CooperativeId] ON [FarmerCooperatives] ([FarmerId], [CooperativeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    CREATE INDEX [IX_FarmerAgents_FarmerId_AgentId] ON [FarmerAgents] ([FarmerId], [AgentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetUsers_PhoneNumber] ON [AspNetUsers] ([PhoneNumber]) WHERE [PhoneNumber] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615110537_update on input financing'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615110537_update on input financing', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623110206_added creditwalletid'
)
BEGIN
    ALTER TABLE [UserWalletTransactions] ADD [CreditWalletId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623110206_added creditwalletid'
)
BEGIN
    CREATE TABLE [WalletReconciliationLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [DebitReference] nvarchar(max) NOT NULL,
        [CreditReference] nvarchar(max) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [FlaggedAt] datetimeoffset NOT NULL,
        [ResolvedAt] datetimeoffset NULL,
        [ResolvedBy] nvarchar(max) NULL,
        [ResolutionNote] nvarchar(max) NULL,
        CONSTRAINT [PK_WalletReconciliationLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623110206_added creditwalletid'
)
BEGIN
    CREATE INDEX [IX_UserWalletTransactions_CreditWalletId] ON [UserWalletTransactions] ([CreditWalletId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623110206_added creditwalletid'
)
BEGIN
    ALTER TABLE [UserWalletTransactions] ADD CONSTRAINT [FK_UserWalletTransactions_UserWallets_CreditWalletId] FOREIGN KEY ([CreditWalletId]) REFERENCES [UserWallets] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623110206_added creditwalletid'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260623110206_added creditwalletid', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623110647_added creditwalletid update'
)
BEGIN
    ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [FK_UserWalletTransactions_UserWallets_CreditWalletId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623110647_added creditwalletid update'
)
BEGIN
    DECLARE @var182 sysname;
    SELECT @var182 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWalletTransactions]') AND [c].[name] = N'UserWalletId');
    IF @var182 IS NOT NULL EXEC(N'ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [' + @var182 + '];');
    ALTER TABLE [UserWalletTransactions] ALTER COLUMN [UserWalletId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623110647_added creditwalletid update'
)
BEGIN
    DECLARE @var183 sysname;
    SELECT @var183 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserWalletTransactions]') AND [c].[name] = N'CreditWalletId');
    IF @var183 IS NOT NULL EXEC(N'ALTER TABLE [UserWalletTransactions] DROP CONSTRAINT [' + @var183 + '];');
    ALTER TABLE [UserWalletTransactions] ALTER COLUMN [CreditWalletId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623110647_added creditwalletid update'
)
BEGIN
    ALTER TABLE [UserWalletTransactions] ADD CONSTRAINT [FK_UserWalletTransactions_UserWallets_CreditWalletId] FOREIGN KEY ([CreditWalletId]) REFERENCES [UserWallets] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623110647_added creditwalletid update'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260623110647_added creditwalletid update', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625082316_updated InputPackageSubscriptionPayment'
)
BEGIN
    DECLARE @var184 sysname;
    SELECT @var184 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackageSubscriptionPayments]') AND [c].[name] = N'Reference');
    IF @var184 IS NOT NULL EXEC(N'ALTER TABLE [InputPackageSubscriptionPayments] DROP CONSTRAINT [' + @var184 + '];');
    ALTER TABLE [InputPackageSubscriptionPayments] ALTER COLUMN [Reference] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625082316_updated InputPackageSubscriptionPayment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625082316_updated InputPackageSubscriptionPayment', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [LocationInventories] DROP CONSTRAINT [FK_LocationInventories_OrganizationProducts_OrganizationProductId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    DECLARE @var185 sysname;
    SELECT @var185 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LocationInventories]') AND [c].[name] = N'OrganizationProductId');
    IF @var185 IS NOT NULL EXEC(N'ALTER TABLE [LocationInventories] DROP CONSTRAINT [' + @var185 + '];');
    ALTER TABLE [LocationInventories] ALTER COLUMN [OrganizationProductId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [LocationInventories] ADD [LocationInventoryType] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [LocationInventories] ADD [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD [Amount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD [Latitude] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD [Longitude] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [IsPartial] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [Latitude] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [LocationId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [Longitude] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE TABLE [DisbursementLineItems] (
        [Id] bigint NOT NULL IDENTITY,
        [DisbursementId] bigint NOT NULL,
        [SubscriptionId] bigint NOT NULL,
        [OrganizationProductId] bigint NOT NULL,
        [SubscribedQuantity] decimal(18,2) NOT NULL,
        [DisbursedQuantity] decimal(18,2) NOT NULL,
        [PendingQuantity] decimal(18,2) NOT NULL,
        [UnitCost] decimal(18,2) NOT NULL,
        [TotalDisbursedValue] decimal(18,2) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [LocationId] bigint NOT NULL,
        [OrganizationLocationId] bigint NOT NULL,
        [AgentUserId] nvarchar(max) NOT NULL,
        [UserId] nvarchar(450) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        CONSTRAINT [PK_DisbursementLineItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DisbursementLineItems_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_DisbursementLineItems_InputPackageSubscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [InputPackageSubscriptions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DisbursementLineItems_InputSubscriptionDisbursements_DisbursementId] FOREIGN KEY ([DisbursementId]) REFERENCES [InputSubscriptionDisbursements] ([Id]),
        CONSTRAINT [FK_DisbursementLineItems_OrganizationLocations_OrganizationLocationId] FOREIGN KEY ([OrganizationLocationId]) REFERENCES [OrganizationLocations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DisbursementLineItems_OrganizationProducts_OrganizationProductId] FOREIGN KEY ([OrganizationProductId]) REFERENCES [OrganizationProducts] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE TABLE [MonetaryDisbursementTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [SubscriptionId] bigint NOT NULL,
        [DisbursementId] bigint NOT NULL,
        [LocationId] bigint NOT NULL,
        [OrganizationLocationId] bigint NOT NULL,
        [SubscribedAmount] decimal(18,2) NOT NULL,
        [DisbursedAmount] decimal(18,2) NOT NULL,
        [PendingAmount] decimal(18,2) NOT NULL,
        [ReferenceNumber] nvarchar(max) NOT NULL,
        [AgentUserId] nvarchar(max) NOT NULL,
        [UserId] nvarchar(450) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [DisbursedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_MonetaryDisbursementTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MonetaryDisbursementTransactions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_MonetaryDisbursementTransactions_InputPackageSubscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [InputPackageSubscriptions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MonetaryDisbursementTransactions_InputSubscriptionDisbursements_DisbursementId] FOREIGN KEY ([DisbursementId]) REFERENCES [InputSubscriptionDisbursements] ([Id]),
        CONSTRAINT [FK_MonetaryDisbursementTransactions_OrganizationLocations_OrganizationLocationId] FOREIGN KEY ([OrganizationLocationId]) REFERENCES [OrganizationLocations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE INDEX [IX_InputSubscriptionDisbursements_LocationId] ON [InputSubscriptionDisbursements] ([LocationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE INDEX [IX_DisbursementLineItems_DisbursementId] ON [DisbursementLineItems] ([DisbursementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE INDEX [IX_DisbursementLineItems_OrganizationLocationId] ON [DisbursementLineItems] ([OrganizationLocationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE INDEX [IX_DisbursementLineItems_OrganizationProductId] ON [DisbursementLineItems] ([OrganizationProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE INDEX [IX_DisbursementLineItems_SubscriptionId] ON [DisbursementLineItems] ([SubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE INDEX [IX_DisbursementLineItems_UserId] ON [DisbursementLineItems] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE INDEX [IX_MonetaryDisbursementTransactions_DisbursementId] ON [MonetaryDisbursementTransactions] ([DisbursementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE INDEX [IX_MonetaryDisbursementTransactions_OrganizationLocationId] ON [MonetaryDisbursementTransactions] ([OrganizationLocationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE INDEX [IX_MonetaryDisbursementTransactions_SubscriptionId] ON [MonetaryDisbursementTransactions] ([SubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    CREATE INDEX [IX_MonetaryDisbursementTransactions_UserId] ON [MonetaryDisbursementTransactions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD CONSTRAINT [FK_InputSubscriptionDisbursements_OrganizationLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [OrganizationLocations] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    ALTER TABLE [LocationInventories] ADD CONSTRAINT [FK_LocationInventories_OrganizationProducts_OrganizationProductId] FOREIGN KEY ([OrganizationProductId]) REFERENCES [OrganizationProducts] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629144105_updated disbursement module'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260629144105_updated disbursement module', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629184014_updated organizationagent table'
)
BEGIN
    ALTER TABLE [OrganizationAgents] DROP CONSTRAINT [FK_OrganizationAgents_OrganizationLocations_LocationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629184014_updated organizationagent table'
)
BEGIN
    DECLARE @var186 sysname;
    SELECT @var186 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrganizationAgents]') AND [c].[name] = N'LocationId');
    IF @var186 IS NOT NULL EXEC(N'ALTER TABLE [OrganizationAgents] DROP CONSTRAINT [' + @var186 + '];');
    ALTER TABLE [OrganizationAgents] ALTER COLUMN [LocationId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629184014_updated organizationagent table'
)
BEGIN
    ALTER TABLE [OrganizationAgents] ADD CONSTRAINT [FK_OrganizationAgents_OrganizationLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [OrganizationLocations] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629184014_updated organizationagent table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260629184014_updated organizationagent table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630180024_updatesubsriouet'
)
BEGIN
    ALTER TABLE [SubscriptionDocuments] ADD [Key] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630180024_updatesubsriouet'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260630180024_updatesubsriouet', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701101958_updted subscription with paymentlist'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701101958_updted subscription with paymentlist', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    ALTER TABLE [UserWallets] ADD [MaxCheckSum] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    DECLARE @var187 sysname;
    SELECT @var187 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Organizations]') AND [c].[name] = N'OrganizationCode');
    IF @var187 IS NOT NULL EXEC(N'ALTER TABLE [Organizations] DROP CONSTRAINT [' + @var187 + '];');
    ALTER TABLE [Organizations] ALTER COLUMN [OrganizationCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    DECLARE @var188 sysname;
    SELECT @var188 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Organizations]') AND [c].[name] = N'LastName');
    IF @var188 IS NOT NULL EXEC(N'ALTER TABLE [Organizations] DROP CONSTRAINT [' + @var188 + '];');
    ALTER TABLE [Organizations] ALTER COLUMN [LastName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    DECLARE @var189 sysname;
    SELECT @var189 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Organizations]') AND [c].[name] = N'FirstName');
    IF @var189 IS NOT NULL EXEC(N'ALTER TABLE [Organizations] DROP CONSTRAINT [' + @var189 + '];');
    ALTER TABLE [Organizations] ALTER COLUMN [FirstName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    DECLARE @var190 sysname;
    SELECT @var190 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Organizations]') AND [c].[name] = N'CompanyName');
    IF @var190 IS NOT NULL EXEC(N'ALTER TABLE [Organizations] DROP CONSTRAINT [' + @var190 + '];');
    ALTER TABLE [Organizations] ALTER COLUMN [CompanyName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    DECLARE @var191 sysname;
    SELECT @var191 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'NIN');
    IF @var191 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var191 + '];');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [NIN] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    DECLARE @var192 sysname;
    SELECT @var192 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'BVN');
    IF @var192 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var192 + '];');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [BVN] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE TABLE [BusinessWalletProviders] (
        [BusinessWalletProviderId] bigint NOT NULL IDENTITY,
        [BusinessCode] nvarchar(450) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [IsDefault] bit NOT NULL,
        [ProviderId] bigint NOT NULL,
        CONSTRAINT [PK_BusinessWalletProviders] PRIMARY KEY ([BusinessWalletProviderId]),
        CONSTRAINT [FK_BusinessWalletProviders_WalletProviders_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [WalletProviders] ([ProviderId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE TABLE [SMSProvider] (
        [ProviderId] bigint NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_SMSProvider] PRIMARY KEY ([ProviderId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE TABLE [BusinessWalletProviderParameters] (
        [ParameterId] bigint NOT NULL IDENTITY,
        [Key] nvarchar(max) NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Env] nvarchar(max) NOT NULL,
        [BusinessWalletProviderId] bigint NOT NULL,
        CONSTRAINT [PK_BusinessWalletProviderParameters] PRIMARY KEY ([ParameterId]),
        CONSTRAINT [FK_BusinessWalletProviderParameters_BusinessWalletProviders_BusinessWalletProviderId] FOREIGN KEY ([BusinessWalletProviderId]) REFERENCES [BusinessWalletProviders] ([BusinessWalletProviderId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE TABLE [BusinessSMSProviders] (
        [BusinessSMSProviderId] bigint NOT NULL IDENTITY,
        [BusinessCode] nvarchar(450) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [IsDefault] bit NOT NULL,
        [ProviderId] bigint NOT NULL,
        CONSTRAINT [PK_BusinessSMSProviders] PRIMARY KEY ([BusinessSMSProviderId]),
        CONSTRAINT [FK_BusinessSMSProviders_SMSProvider_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [SMSProvider] ([ProviderId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE TABLE [SMSProviderParameters] (
        [ParameterId] bigint NOT NULL IDENTITY,
        [Value] nvarchar(max) NOT NULL,
        [Key] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [SMSProviderProviderId] bigint NOT NULL,
        [Env] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_SMSProviderParameters] PRIMARY KEY ([ParameterId]),
        CONSTRAINT [FK_SMSProviderParameters_SMSProvider_SMSProviderProviderId] FOREIGN KEY ([SMSProviderProviderId]) REFERENCES [SMSProvider] ([ProviderId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE TABLE [BusinessSMSProviderParameters] (
        [ParameterId] bigint NOT NULL IDENTITY,
        [Key] nvarchar(max) NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Env] nvarchar(max) NOT NULL,
        [BusinessSMSProviderId] bigint NOT NULL,
        CONSTRAINT [PK_BusinessSMSProviderParameters] PRIMARY KEY ([ParameterId]),
        CONSTRAINT [FK_BusinessSMSProviderParameters_BusinessSMSProviders_BusinessSMSProviderId] FOREIGN KEY ([BusinessSMSProviderId]) REFERENCES [BusinessSMSProviders] ([BusinessSMSProviderId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetUsers_BVN] ON [AspNetUsers] ([BVN]) WHERE [BVN] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetUsers_Email] ON [AspNetUsers] ([Email]) WHERE [Email] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetUsers_NIN] ON [AspNetUsers] ([NIN]) WHERE [NIN] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE INDEX [IX_BusinessSMSProviderParameters_BusinessSMSProviderId] ON [BusinessSMSProviderParameters] ([BusinessSMSProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BusinessSMSProviders_BusinessCode_ProviderId] ON [BusinessSMSProviders] ([BusinessCode], [ProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE INDEX [IX_BusinessSMSProviders_ProviderId] ON [BusinessSMSProviders] ([ProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE INDEX [IX_BusinessWalletProviderParameters_BusinessWalletProviderId] ON [BusinessWalletProviderParameters] ([BusinessWalletProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BusinessWalletProviders_BusinessCode_ProviderId] ON [BusinessWalletProviders] ([BusinessCode], [ProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE INDEX [IX_BusinessWalletProviders_ProviderId] ON [BusinessWalletProviders] ([ProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    CREATE INDEX [IX_SMSProviderParameters_SMSProviderProviderId] ON [SMSProviderParameters] ([SMSProviderProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105941_updated wallet and sms to support multiple'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710105941_updated wallet and sms to support multiple', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719212702_input financing repayment credit'
)
BEGIN
    ALTER TABLE [OrganizationProducts] ADD [PricePerUnit] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719212702_input financing repayment credit'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD [Grade] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719212702_input financing repayment credit'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD [UnitPriceUsed] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719212702_input financing repayment credit'
)
BEGIN
    ALTER TABLE [InputPackageSubscriptions] ADD [RepaymentValue] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719212702_input financing repayment credit'
)
BEGIN
    CREATE TABLE [SubscriptionCreditBalance] (
        [Id] bigint NOT NULL IDENTITY,
        [SubscriptionId] bigint NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Reference] nvarchar(max) NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [IsConsumed] bit NOT NULL,
        [ConsumedOn] datetimeoffset NULL,
        CONSTRAINT [PK_SubscriptionCreditBalance] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719212702_input financing repayment credit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719212702_input financing repayment credit', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720192719_repayment schedule updated'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [Balance] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720192719_repayment schedule updated'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [Status] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720192719_repayment schedule updated'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260720192719_repayment schedule updated', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726073056_updated indexes on farms query'
)
BEGIN
    DROP INDEX [IX_FarmerAgents_AgentId] ON [FarmerAgents];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726073056_updated indexes on farms query'
)
BEGIN
    CREATE INDEX [IX_Farms_FarmerId] ON [Farms] ([FarmerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726073056_updated indexes on farms query'
)
BEGIN
    CREATE INDEX [IX_Farmers_OnBoardedOn] ON [Farmers] ([OnBoardedOn]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726073056_updated indexes on farms query'
)
BEGIN
    CREATE INDEX [IX_FarmerCooperatives_FarmerId] ON [FarmerCooperatives] ([FarmerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726073056_updated indexes on farms query'
)
BEGIN
    CREATE INDEX [IX_FarmerAgents_AgentId_FarmerId] ON [FarmerAgents] ([AgentId], [FarmerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726073056_updated indexes on farms query'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260726073056_updated indexes on farms query', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804173554_updated userid nulable on organization'
)
BEGIN
    DECLARE @var193 sysname;
    SELECT @var193 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Organizations]') AND [c].[name] = N'UserId');
    IF @var193 IS NOT NULL EXEC(N'ALTER TABLE [Organizations] DROP CONSTRAINT [' + @var193 + '];');
    ALTER TABLE [Organizations] ALTER COLUMN [UserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804173554_updated userid nulable on organization'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260804173554_updated userid nulable on organization', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805125939_updated profile'
)
BEGIN
    ALTER TABLE [KYCDetails] ADD [VerificationType] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805125939_updated profile'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsBvnVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805125939_updated profile'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsNinVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805125939_updated profile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805125939_updated profile', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807082355_updated kyc table'
)
BEGIN
    EXEC sp_rename N'[KYCDetails].[Nin]', N'Number', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807082355_updated kyc table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807082355_updated kyc table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807112340_added nullable provider'
)
BEGIN
    ALTER TABLE [InputPackages] DROP CONSTRAINT [FK_InputPackages_WalletProviders_WalletProviderId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807112340_added nullable provider'
)
BEGIN
    DECLARE @var194 sysname;
    SELECT @var194 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputPackages]') AND [c].[name] = N'WalletProviderId');
    IF @var194 IS NOT NULL EXEC(N'ALTER TABLE [InputPackages] DROP CONSTRAINT [' + @var194 + '];');
    ALTER TABLE [InputPackages] ALTER COLUMN [WalletProviderId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807112340_added nullable provider'
)
BEGIN
    ALTER TABLE [InputPackages] ADD CONSTRAINT [FK_InputPackages_WalletProviders_WalletProviderId] FOREIGN KEY ([WalletProviderId]) REFERENCES [WalletProviders] ([ProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807112340_added nullable provider'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807112340_added nullable provider', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811105936_repaymentschedule'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [DeletedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811105936_repaymentschedule'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [DeletedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811105936_repaymentschedule'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [DeletionReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811105936_repaymentschedule'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [GracePeriodDueDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811105936_repaymentschedule'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811105936_repaymentschedule'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [SupersededByRepaymentScheduleId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811105936_repaymentschedule'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD [SupersededByScheduleId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811105936_repaymentschedule'
)
BEGIN
    CREATE INDEX [IX_RepaymentSchedules_SupersededByRepaymentScheduleId] ON [RepaymentSchedules] ([SupersededByRepaymentScheduleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811105936_repaymentschedule'
)
BEGIN
    ALTER TABLE [RepaymentSchedules] ADD CONSTRAINT [FK_RepaymentSchedules_RepaymentSchedules_SupersededByRepaymentScheduleId] FOREIGN KEY ([SupersededByRepaymentScheduleId]) REFERENCES [RepaymentSchedules] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811105936_repaymentschedule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811105936_repaymentschedule', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817143519_updated inputfinancing'
)
BEGIN
    DECLARE @var195 sysname;
    SELECT @var195 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputSubscriptionDisbursements]') AND [c].[name] = N'RequestedByUserId');
    IF @var195 IS NOT NULL EXEC(N'ALTER TABLE [InputSubscriptionDisbursements] DROP CONSTRAINT [' + @var195 + '];');
    ALTER TABLE [InputSubscriptionDisbursements] DROP COLUMN [RequestedByUserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817143519_updated inputfinancing'
)
BEGIN
    ALTER TABLE [WalletProviders] ADD [BankName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817143519_updated inputfinancing'
)
BEGIN
    ALTER TABLE [UserWallets] ADD [BankName] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817143519_updated inputfinancing'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [RequestForUserId] nvarchar(450) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817143519_updated inputfinancing'
)
BEGIN
    CREATE INDEX [IX_InputSubscriptionDisbursements_RequestForUserId] ON [InputSubscriptionDisbursements] ([RequestForUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817143519_updated inputfinancing'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD CONSTRAINT [FK_InputSubscriptionDisbursements_AspNetUsers_RequestForUserId] FOREIGN KEY ([RequestForUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817143519_updated inputfinancing'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260817143519_updated inputfinancing', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817145134_updated inputfinancing updated'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260817145134_updated inputfinancing updated', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818090312_added rejection state to input disbursement table'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [RejectedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818090312_added rejection state to input disbursement table'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [RejectedOn] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818090312_added rejection state to input disbursement table'
)
BEGIN
    ALTER TABLE [InputSubscriptionDisbursements] ADD [RejectionReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818090312_added rejection state to input disbursement table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818090312_added rejection state to input disbursement table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818192641_updated disbursement line'
)
BEGIN
    DECLARE @var196 sysname;
    SELECT @var196 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DisbursementLineItems]') AND [c].[name] = N'LocationId');
    IF @var196 IS NOT NULL EXEC(N'ALTER TABLE [DisbursementLineItems] DROP CONSTRAINT [' + @var196 + '];');
    ALTER TABLE [DisbursementLineItems] DROP COLUMN [LocationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818192641_updated disbursement line'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818192641_updated disbursement line', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824150142_added dateofbirth to userstable'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [DateOfBirth] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824150142_added dateofbirth to userstable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824150142_added dateofbirth to userstable', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831103117_updated the activities log with other details'
)
BEGIN
    EXEC sp_rename N'[ActivityLogs].[ActivityType]', N'Status', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831103117_updated the activities log with other details'
)
BEGIN
    ALTER TABLE [ActivityLogs] ADD [ActionPerformeed] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831103117_updated the activities log with other details'
)
BEGIN
    ALTER TABLE [ActivityLogs] ADD [ActionType] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831103117_updated the activities log with other details'
)
BEGIN
    ALTER TABLE [ActivityLogs] ADD [AffectedRecord] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831103117_updated the activities log with other details'
)
BEGIN
    ALTER TABLE [ActivityLogs] ADD [Browser] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831103117_updated the activities log with other details'
)
BEGIN
    ALTER TABLE [ActivityLogs] ADD [Device] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831103117_updated the activities log with other details'
)
BEGIN
    ALTER TABLE [ActivityLogs] ADD [IpAddress] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831103117_updated the activities log with other details'
)
BEGIN
    ALTER TABLE [ActivityLogs] ADD [ModuleAccessed] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831103117_updated the activities log with other details'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831103117_updated the activities log with other details', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902042355_added logout implementation'
)
BEGIN
    CREATE TABLE [RevokedTokens] (
        [Id] int NOT NULL IDENTITY,
        [Jti] nvarchar(max) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [RevokedOn] datetimeoffset NOT NULL,
        CONSTRAINT [PK_RevokedTokens] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902042355_added logout implementation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902042355_added logout implementation', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902042930_added logout implementations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902042930_added logout implementations', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902172443_added signup method'
)
BEGIN
    DECLARE @var197 sysname;
    SELECT @var197 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InputSubscriptionDisbursements]') AND [c].[name] = N'DisbursedAmount');
    IF @var197 IS NOT NULL EXEC(N'ALTER TABLE [InputSubscriptionDisbursements] DROP CONSTRAINT [' + @var197 + '];');
    ALTER TABLE [InputSubscriptionDisbursements] DROP COLUMN [DisbursedAmount];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902172443_added signup method'
)
BEGIN
    DECLARE @var198 sysname;
    SELECT @var198 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RevokedTokens]') AND [c].[name] = N'ExpiresAt');
    IF @var198 IS NOT NULL EXEC(N'ALTER TABLE [RevokedTokens] DROP CONSTRAINT [' + @var198 + '];');
    EXEC(N'UPDATE [RevokedTokens] SET [ExpiresAt] = ''0001-01-01T00:00:00.0000000+00:00'' WHERE [ExpiresAt] IS NULL');
    ALTER TABLE [RevokedTokens] ALTER COLUMN [ExpiresAt] datetimeoffset NOT NULL;
    ALTER TABLE [RevokedTokens] ADD DEFAULT '0001-01-01T00:00:00.0000000+00:00' FOR [ExpiresAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902172443_added signup method'
)
BEGIN
    DECLARE @var199 sysname;
    SELECT @var199 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RevokedTokens]') AND [c].[name] = N'Id');
    IF @var199 IS NOT NULL EXEC(N'ALTER TABLE [RevokedTokens] DROP CONSTRAINT [' + @var199 + '];');
    ALTER TABLE [RevokedTokens] ALTER COLUMN [Id] int NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902172443_added signup method'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [SignupMethod] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902172443_added signup method'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902172443_added signup method', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903030819_added user notification preference'
)
BEGIN
    CREATE TABLE [UserNotificationPreferences] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [MappingStatusUpdates] bit NOT NULL,
        [AgentVisitReminders] bit NOT NULL,
        [MembershipAnnouncements] bit NOT NULL,
        [DocumentApprovalUpdates] bit NOT NULL,
        [GeneralPlatformUpdates] bit NOT NULL,
        [Sms] bit NOT NULL,
        [Email] bit NOT NULL,
        [InAppNotification] bit NOT NULL,
        [CreatedOn] datetimeoffset NOT NULL,
        [DateModified] datetimeoffset NULL,
        CONSTRAINT [PK_UserNotificationPreferences] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903030819_added user notification preference'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserNotificationPreferences_UserId] ON [UserNotificationPreferences] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903030819_added user notification preference'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903030819_added user notification preference', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903035630_added agent notification preference fields'
)
BEGIN
    ALTER TABLE [UserNotificationPreferences] ADD [BulkUploadCompleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903035630_added agent notification preference fields'
)
BEGIN
    ALTER TABLE [UserNotificationPreferences] ADD [NewFarmCreated] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903035630_added agent notification preference fields'
)
BEGIN
    ALTER TABLE [UserNotificationPreferences] ADD [NewFarmerAssigned] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903035630_added agent notification preference fields'
)
BEGIN
    ALTER TABLE [UserNotificationPreferences] ADD [WalletRepaymentDue] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903035630_added agent notification preference fields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903035630_added agent notification preference fields', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903042512_added user role to notification preference'
)
BEGIN
    DROP INDEX [IX_UserNotificationPreferences_UserId] ON [UserNotificationPreferences];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903042512_added user role to notification preference'
)
BEGIN
    ALTER TABLE [UserNotificationPreferences] ADD [UserRole] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903042512_added user role to notification preference'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserNotificationPreferences_UserId_UserRole] ON [UserNotificationPreferences] ([UserId], [UserRole]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903042512_added user role to notification preference'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903042512_added user role to notification preference', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908093226_renamed in app notification to push notification'
)
BEGIN
    EXEC sp_rename N'[UserNotificationPreferences].[InAppNotification]', N'PushNotification', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908093226_renamed in app notification to push notification'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260908093226_renamed in app notification to push notification', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915121150_updated forgot and reset password flow'
)
BEGIN
    CREATE TABLE [CredentialResetAttempts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [PurposeKey] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CredentialResetAttempts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CredentialResetAttempts_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915121150_updated forgot and reset password flow'
)
BEGIN
    CREATE INDEX [IX_CredentialResetAttempts_UserId_PurposeKey_CreatedAt] ON [CredentialResetAttempts] ([UserId], [PurposeKey], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915121150_updated forgot and reset password flow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915121150_updated forgot and reset password flow', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002160717_updated users table wit f m and lastname'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [FirstName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002160717_updated users table wit f m and lastname'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [LastName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002160717_updated users table wit f m and lastname'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [MiddleName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002160717_updated users table wit f m and lastname'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261002160717_updated users table wit f m and lastname', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005063054_updated membership table'
)
BEGIN
    ALTER TABLE [MemberShips] ADD [RegistrationNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005063054_updated membership table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005063054_updated membership table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005120918_updated refresh token'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [BusinessId] bigint NOT NULL,
        [TokenHash] nvarchar(64) NOT NULL,
        [FamilyId] nvarchar(32) NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [ExpiresOn] datetime2 NOT NULL,
        [RevokedOn] datetime2 NULL,
        [RevokedReason] nvarchar(max) NULL,
        [RequiresCredentialChange] bit NOT NULL,
        [CreatedByIp] nvarchar(max) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005120918_updated refresh token'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_FamilyId] ON [RefreshTokens] ([FamilyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005120918_updated refresh token'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005120918_updated refresh token'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005120918_updated refresh token'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005120918_updated refresh token', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [PK_AspNetUserRoles];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    ALTER TABLE [RefreshTokens] ADD [IsAdminSession] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    ALTER TABLE [AspNetUserRoles] ADD [BusinessId] bigint NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    ALTER TABLE [AspNetUserRoles] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN

                UPDATE ur
                SET ur.BusinessId = x.BusinessId
                FROM AspNetUserRoles ur
                INNER JOIN (
                    SELECT ub.UserId,
                           COALESCE(MIN(CASE WHEN ub.IsDefault = 1 THEN ub.BusinessId END),
                                    MIN(ub.BusinessId)) AS BusinessId
                    FROM UserBusinesses ub
                    WHERE ub.IsActive = 1
                    GROUP BY ub.UserId
                ) x ON x.UserId = ur.UserId;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN

                 INSERT INTO AspNetUserRoles (UserId, RoleId, BusinessId, IsActive)
                SELECT ur.UserId, ur.RoleId, ub.BusinessId, 1
                FROM AspNetUserRoles ur
                 JOIN UserBusinesses ub
                     ON ub.UserId = ur.UserId AND ub.IsActive = 1 AND ub.BusinessId <> ur.BusinessId
                WHERE NOT EXISTS (
                     SELECT 1 FROM AspNetUserRoles x
                     WHERE x.UserId = ur.UserId AND x.RoleId = ur.RoleId AND x.BusinessId = ub.BusinessId);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM Businesses)
    BEGIN
        INSERT INTO Businesses (BusinessCode, BusinessCodeSignage, BusinessCodeSignageSalt, BusinessName, ContactEmail, BusinessEmail, PhoneNumber, Address, BusinessStatus, Category, KycCategory, BusinessLogo, CreatedOn, IsActive)
        VALUES ('__MIGRATION_DEFAULT__','__MIG_SIGN__','__MIG_SALT__','Migration Default','migration@local','migration@local','0000000000','Migration address','Active','','','', GETDATE(), 1);
    END

    DECLARE @fallback bigint = (SELECT MIN(BusinessId) FROM Businesses);

    UPDATE AspNetUserRoles
    SET BusinessId = @fallback
    WHERE BusinessId IS NULL OR BusinessId NOT IN (SELECT BusinessId FROM Businesses);

    ALTER TABLE AspNetUserRoles ALTER COLUMN BusinessId bigint NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    ALTER TABLE [AspNetUserRoles] ADD CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId], [BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Group] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [RoleId] nvarchar(450) NOT NULL,
        [PermissionId] int NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermissions_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_BusinessId] ON [AspNetUserRoles] ([BusinessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_UserId_BusinessId_IsActive] ON [AspNetUserRoles] ([UserId], [BusinessId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Name] ON [Permissions] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    ALTER TABLE [AspNetUserRoles] ADD CONSTRAINT [FK_AspNetUserRoles_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([BusinessId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007100532_updated roles and permissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007100532_updated roles and permissions', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007191828_update roles table'
)
BEGIN
    ALTER TABLE [AspNetRoles] ADD [IsAvailableToAdmin] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007191828_update roles table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007191828_update roles table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    ALTER TABLE [Organizations] ADD [WardId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [AdminCode] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Department] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsAdminUser] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Status] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    CREATE TABLE [AdminCodeSequences] (
        [Id] bigint NOT NULL IDENTITY,
        CONSTRAINT [PK_AdminCodeSequences] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    CREATE TABLE [AdminInvitations] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [TokenHash] nvarchar(450) NOT NULL,
        [SentAt] datetimeoffset NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [AcceptedAt] datetimeoffset NULL,
        [RevokedAt] datetimeoffset NULL,
        [InvitedByUserId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AdminInvitations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    CREATE INDEX [IX_Organizations_WardId] ON [Organizations] ([WardId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetUsers_AdminCode] ON [AspNetUsers] ([AdminCode]) WHERE [AdminCode] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AdminInvitations_TokenHash] ON [AdminInvitations] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    CREATE INDEX [IX_AdminInvitations_UserId] ON [AdminInvitations] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    ALTER TABLE [Organizations] ADD CONSTRAINT [FK_Organizations_Wards_WardId] FOREIGN KEY ([WardId]) REFERENCES [Wards] ([WardId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008074616_added admin columns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008074616_added admin columns', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008111417_added gender to users table'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Gender] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008111417_added gender to users table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008111417_added gender to users table', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008173447_updated adminhistory log'
)
BEGIN
    CREATE TABLE [AdminActivityLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorUserId] nvarchar(450) NULL,
        [TargetUserId] nvarchar(450) NULL,
        [Action] nvarchar(max) NOT NULL,
        [Details] nvarchar(max) NULL,
        [OccurredAt] datetimeoffset NOT NULL,
        [IpAddress] nvarchar(max) NULL,
        CONSTRAINT [PK_AdminActivityLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008173447_updated adminhistory log'
)
BEGIN
    CREATE TABLE [AdminLoginHistories] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [LoggedInAt] datetimeoffset NOT NULL,
        [Succeeded] bit NOT NULL,
        [FailureReason] nvarchar(max) NULL,
        [IpAddress] nvarchar(max) NULL,
        [UserAgent] nvarchar(max) NULL,
        CONSTRAINT [PK_AdminLoginHistories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008173447_updated adminhistory log'
)
BEGIN
    CREATE INDEX [IX_AdminActivityLogs_ActorUserId_OccurredAt] ON [AdminActivityLogs] ([ActorUserId], [OccurredAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008173447_updated adminhistory log'
)
BEGIN
    CREATE INDEX [IX_AdminActivityLogs_TargetUserId_OccurredAt] ON [AdminActivityLogs] ([TargetUserId], [OccurredAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008173447_updated adminhistory log'
)
BEGIN
    CREATE INDEX [IX_AdminLoginHistories_UserId_LoggedInAt] ON [AdminLoginHistories] ([UserId], [LoggedInAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008173447_updated adminhistory log'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008173447_updated adminhistory log', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008185549_updated adminhistory log 2'
)
BEGIN
    ALTER TABLE [AdminActivityLogs] ADD [AffectedRecord] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008185549_updated adminhistory log 2'
)
BEGIN
    ALTER TABLE [AdminActivityLogs] ADD [Module] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008185549_updated adminhistory log 2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008185549_updated adminhistory log 2', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    DECLARE @var200 sysname;
    SELECT @var200 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AdminLoginHistories]') AND [c].[name] = N'UserAgent');
    IF @var200 IS NOT NULL EXEC(N'ALTER TABLE [AdminLoginHistories] DROP CONSTRAINT [' + @var200 + '];');
    ALTER TABLE [AdminLoginHistories] ALTER COLUMN [UserAgent] nvarchar(512) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    ALTER TABLE [AdminLoginHistories] ADD [Browser] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    ALTER TABLE [AdminLoginHistories] ADD [DeviceName] nvarchar(150) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    ALTER TABLE [AdminLoginHistories] ADD [DeviceType] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    ALTER TABLE [AdminLoginHistories] ADD [OperatingSystem] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    ALTER TABLE [AdminActivityLogs] ADD [Browser] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    ALTER TABLE [AdminActivityLogs] ADD [DeviceName] nvarchar(150) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    ALTER TABLE [AdminActivityLogs] ADD [DeviceType] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    ALTER TABLE [AdminActivityLogs] ADD [OperatingSystem] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    ALTER TABLE [AdminActivityLogs] ADD [UserAgent] nvarchar(512) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194317_added UAParser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008194317_added UAParser', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012083007_added farmer status'
)
BEGIN
    ALTER TABLE [Farms] ADD [PrimaryCrop] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012083007_added farmer status'
)
BEGIN
    DECLARE @var201 sysname;
    SELECT @var201 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'UserId');
    IF @var201 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT [' + @var201 + '];');
    ALTER TABLE [Farmers] ALTER COLUMN [UserId] nvarchar(450) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012083007_added farmer status'
)
BEGIN
    ALTER TABLE [Farmers] ADD [FarmerStatus] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012083007_added farmer status'
)
BEGIN
    CREATE INDEX [IX_Farmers_UserId] ON [Farmers] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012083007_added farmer status'
)
BEGIN
    ALTER TABLE [Farmers] ADD CONSTRAINT [FK_Farmers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012083007_added farmer status'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012083007_added farmer status', N'8.0.4');
END;
GO

COMMIT;
GO

