SET NOCOUNT ON;

DECLARE @Now DATETIME2 = GETUTCDATE();
DECLARE @AdminEmail NVARCHAR(256) = 'admin@test.com';
DECLARE @AdminPasswordHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEJGWuQgwdOk3hmDUCL1CbYuzJR5quRQMMazJfjR9tNjJHENeA2pHyjvXD8xljhSpcA==';

IF EXISTS (SELECT 1 FROM [Users] WHERE [Email] = @AdminEmail)
BEGIN
    UPDATE [Users]
    SET
        [Name] = 'System Admin',
        [PasswordHash] = @AdminPasswordHash,
        [Role] = 1,
        [Location] = 'Head Office',
        [UpdatedAt] = @Now
    WHERE [Email] = @AdminEmail;
END
ELSE
BEGIN
    INSERT INTO [Users] ([Id], [Name], [Email], [PasswordHash], [Role], [Location], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'System Admin', @AdminEmail, @AdminPasswordHash, 1, 'Head Office', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = 'john.doe@example.com')
BEGIN
    INSERT INTO [Users] ([Id], [Name], [Email], [PasswordHash], [Role], [Location], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'John Doe', 'john.doe@example.com', 'seeded-password-hash', 1, 'Rabat', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = 'sara.ben@example.com')
BEGIN
    INSERT INTO [Users] ([Id], [Name], [Email], [PasswordHash], [Role], [Location], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'Sara Ben', 'sara.ben@example.com', 'seeded-password-hash', 0, 'Casablanca', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = 'youssef.el@example.com')
BEGIN
    INSERT INTO [Users] ([Id], [Name], [Email], [PasswordHash], [Role], [Location], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'Youssef El', 'youssef.el@example.com', 'seeded-password-hash', 0, 'Tangier', @Now, @Now);
END;

DECLARE @JohnId UNIQUEIDENTIFIER = (SELECT TOP (1) [Id] FROM [Users] WHERE [Email] = 'john.doe@example.com');
DECLARE @SaraId UNIQUEIDENTIFIER = (SELECT TOP (1) [Id] FROM [Users] WHERE [Email] = 'sara.ben@example.com');
DECLARE @YoussefId UNIQUEIDENTIFIER = (SELECT TOP (1) [Id] FROM [Users] WHERE [Email] = 'youssef.el@example.com');

IF NOT EXISTS (SELECT 1 FROM [Devices] WHERE [Tag] = 'DV-0001')
BEGIN
    INSERT INTO [Devices] ([Id], [Tag], [Name], [Manufacturer], [Type], [OperatingSystem], [OSVersion], [Processor], [RamAmount], [Description], [AssignedUserId], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'DV-0001', 'iPhone 13', 'Apple', 0, 'iOS', '17.4', 'A15 Bionic', '4GB', 'Team phone for QA testing', @SaraId, @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Devices] WHERE [Tag] = 'DV-0002')
BEGIN
    INSERT INTO [Devices] ([Id], [Tag], [Name], [Manufacturer], [Type], [OperatingSystem], [OSVersion], [Processor], [RamAmount], [Description], [AssignedUserId], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'DV-0002', 'Galaxy S23', 'Samsung', 0, 'Android', '14', 'Snapdragon 8 Gen 2', '8GB', 'Android baseline phone', @YoussefId, @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Devices] WHERE [Tag] = 'DV-0003')
BEGIN
    INSERT INTO [Devices] ([Id], [Tag], [Name], [Manufacturer], [Type], [OperatingSystem], [OSVersion], [Processor], [RamAmount], [Description], [AssignedUserId], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'DV-0003', 'Pixel 8', 'Google', 0, 'Android', '14', 'Tensor G3', '8GB', 'Reference Android device', NULL, @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Devices] WHERE [Tag] = 'DV-0004')
BEGIN
    INSERT INTO [Devices] ([Id], [Tag], [Name], [Manufacturer], [Type], [OperatingSystem], [OSVersion], [Processor], [RamAmount], [Description], [AssignedUserId], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'DV-0004', 'iPad Air', 'Apple', 1, 'iPadOS', '17.4', 'M1', '8GB', 'Tablet for design reviews', @JohnId, @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Devices] WHERE [Tag] = 'DV-0005')
BEGIN
    INSERT INTO [Devices] ([Id], [Tag], [Name], [Manufacturer], [Type], [OperatingSystem], [OSVersion], [Processor], [RamAmount], [Description], [AssignedUserId], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'DV-0005', 'Galaxy Tab S9', 'Samsung', 1, 'Android', '14', 'Snapdragon 8 Gen 2', '12GB', 'High-end tablet for testing', NULL, @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Devices] WHERE [Tag] = 'DV-0006')
BEGIN
    INSERT INTO [Devices] ([Id], [Tag], [Name], [Manufacturer], [Type], [OperatingSystem], [OSVersion], [Processor], [RamAmount], [Description], [AssignedUserId], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'DV-0006', 'Redmi Note 13', 'Xiaomi', 0, 'Android', '14', 'Snapdragon 685', '6GB', 'Budget Android handset', NULL, @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Devices] WHERE [Tag] = 'DV-0007')
BEGIN
    INSERT INTO [Devices] ([Id], [Tag], [Name], [Manufacturer], [Type], [OperatingSystem], [OSVersion], [Processor], [RamAmount], [Description], [AssignedUserId], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'DV-0007', 'iPhone SE', 'Apple', 0, 'iOS', '17.4', 'A15 Bionic', '4GB', 'Compact iOS regression device', NULL, @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Devices] WHERE [Tag] = 'DV-0008')
BEGIN
    INSERT INTO [Devices] ([Id], [Tag], [Name], [Manufacturer], [Type], [OperatingSystem], [OSVersion], [Processor], [RamAmount], [Description], [AssignedUserId], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), 'DV-0008', 'Lenovo Tab M10', 'Lenovo', 1, 'Android', '13', 'MediaTek Helio G80', '4GB', 'Tablet pool spare', NULL, @Now, @Now);
END;