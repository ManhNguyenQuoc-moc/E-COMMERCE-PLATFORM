-- =====================================================================
-- UNIFIED DATABASE SEED SCRIPT (Idempotent & Production-Safe)
-- Project : E-Commerce Platform (.NET 10 / EF Core 10 / SQL Server)
-- Covers  : Admin, 4 Customer Ranks, 5 Categories, 15 Products, 2 Coupons
-- =====================================================================
-- HƯỚNG DẪN CHẠY TRÊN PRODUCTION HOẶC MÁY KHÁC:
--   1. Chạy `dotnet ef database update` trước để tạo đủ bảng & migration.
--   2. Mở file này trong SSMS / Azure Data Studio (chọn đúng Database)
--      HOẶC chạy qua sqlcmd:
--      sqlcmd -S <HOST>,<PORT> -U <USER> -P <PASS> -d <DB_NAME> -i seed-all.sql
--   3. Đổi mật khẩu tài khoản Admin ngay sau khi seed trên Production!
-- =====================================================================

-- Bỏ comment dòng dưới nếu muốn cố định chạy vào EcommercePlatformDb:
-- USE [EcommercePlatformDb];
-- GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now          DATETIMEOFFSET = SYSDATETIMEOFFSET();
    -- BCrypt hash (work factor 11) cho mật khẩu mẫu: Password@123
    DECLARE @PasswordHash NVARCHAR(MAX)  = N'$2a$11$c7FLo2elzEW8q3cP0bDaCusgS7gAHpTr8pVCqYfSY6OjjuKRpsN0m';

    -- =================================================================
    -- 1. SEED ADMIN ACCOUNT & PROFILE
    -- =================================================================
    DECLARE @AdminAccountId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
    DECLARE @AdminUserId    UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Accounts] WHERE [Email] = N'admin@ecommerce.com' OR [Username] = N'admin')
    BEGIN
        INSERT INTO [dbo].[Accounts] ([Id], [Username], [Email], [PasswordHash], [Role], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt])
        VALUES (@AdminAccountId, N'admin', N'admin@ecommerce.com', @PasswordHash, N'Admin', 1, NULL, @Now, NULL);

        INSERT INTO [dbo].[Users] ([Id], [AccountId], [FullName], [PhoneNumber], [AvatarUrl], [DefaultShippingAddress], [LoyaltyPoints], [Rank], [CreatedAt], [UpdatedAt])
        VALUES (@AdminUserId, @AdminAccountId, N'System Administrator', N'0900000000', NULL, N'Headquarters, Ho Chi Minh City', 0, N'Diamond', @Now, NULL);
    END

    -- =================================================================
    -- 2. SEED 4 SAMPLE CUSTOMERS (Member, Silver, Gold, Diamond)
    -- =================================================================
    DECLARE @Customers TABLE (
        AccountId              UNIQUEIDENTIFIER,
        UserId                 UNIQUEIDENTIFIER,
        Username               NVARCHAR(64),
        Email                  NVARCHAR(256),
        FullName               NVARCHAR(150),
        PhoneNumber            NVARCHAR(20),
        DefaultShippingAddress NVARCHAR(500),
        LoyaltyPoints          INT,
        [Rank]                 NVARCHAR(32)
    );

    INSERT INTO @Customers VALUES
        ('33333333-3333-3333-3333-333333333301', '44444444-4444-4444-4444-444444444401', N'customer_member',  N'customer.member@ecommerce.com',  N'Nguyễn Văn Member',  N'0911111111', N'10 Nguyễn Huệ, Quận 1, TP.HCM',   0,    N'Member'),
        ('33333333-3333-3333-3333-333333333302', '44444444-4444-4444-4444-444444444402', N'customer_silver',  N'customer.silver@ecommerce.com',  N'Trần Thị Silver',    N'0922222222', N'20 Lê Lợi, Quận 1, TP.HCM',       150,  N'Silver'),
        ('33333333-3333-3333-3333-333333333303', '44444444-4444-4444-4444-444444444403', N'customer_gold',    N'customer.gold@ecommerce.com',    N'Lê Hoàng Gold',      N'0933333333', N'30 Hai Bà Trưng, Quận 3, TP.HCM', 550,  N'Gold'),
        ('33333333-3333-3333-3333-333333333304', '44444444-4444-4444-4444-444444444404', N'customer_diamond', N'customer.diamond@ecommerce.com', N'Phạm Minh Diamond',  N'0944444444', N'40 Võ Văn Tần, Quận 3, TP.HCM',   1200, N'Diamond');

    INSERT INTO [dbo].[Accounts] ([Id], [Username], [Email], [PasswordHash], [Role], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt])
    SELECT c.AccountId, c.Username, c.Email, @PasswordHash, N'User', 1, NULL, @Now, NULL
    FROM @Customers c
    WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Accounts] a WHERE a.[Email] = c.Email OR a.[Username] = c.Username);

    INSERT INTO [dbo].[Users] ([Id], [AccountId], [FullName], [PhoneNumber], [AvatarUrl], [DefaultShippingAddress], [LoyaltyPoints], [Rank], [CreatedAt], [UpdatedAt])
    SELECT c.UserId, c.AccountId, c.FullName, c.PhoneNumber, NULL, c.DefaultShippingAddress, c.LoyaltyPoints, c.[Rank], @Now, NULL
    FROM @Customers c
    WHERE EXISTS (SELECT 1 FROM [dbo].[Accounts] a WHERE a.[Id] = c.AccountId)
      AND NOT EXISTS (SELECT 1 FROM [dbo].[Users] u WHERE u.[AccountId] = c.AccountId);

    -- =================================================================
    -- 3. SEED 5 CATEGORIES
    -- =================================================================
    DECLARE @CatSkincare  UNIQUEIDENTIFIER = 'A1111111-1111-1111-1111-111111111111';
    DECLARE @CatMakeup    UNIQUEIDENTIFIER = 'A2222222-2222-2222-2222-222222222222';
    DECLARE @CatSunscreen UNIQUEIDENTIFIER = 'A3333333-3333-3333-3333-333333333333';
    DECLARE @CatHaircare  UNIQUEIDENTIFIER = 'A4444444-4444-4444-4444-444444444444';
    DECLARE @CatPerfume   UNIQUEIDENTIFIER = 'A5555555-5555-5555-5555-555555555555';

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'cham-soc-da-mat')
        INSERT INTO [dbo].[Categories] ([Id], [Name], [Slug], [Description], [IsDeleted], [DeletedAt], [CreatedAt], [UpdatedAt])
        VALUES (@CatSkincare, N'Chăm sóc da mặt', N'cham-soc-da-mat', N'Sữa rửa mặt, toner, serum đặc trị và kem dưỡng ẩm chuyên sâu.', 0, NULL, @Now, NULL);
    ELSE
        SELECT @CatSkincare = [Id] FROM [dbo].[Categories] WHERE [Slug] = N'cham-soc-da-mat';

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'trang-diem')
        INSERT INTO [dbo].[Categories] ([Id], [Name], [Slug], [Description], [IsDeleted], [DeletedAt], [CreatedAt], [UpdatedAt])
        VALUES (@CatMakeup, N'Trang điểm', N'trang-diem', N'Son môi, kem nền, cushion, phấn phủ và bảng mắt chính hãng.', 0, NULL, @Now, NULL);
    ELSE
        SELECT @CatMakeup = [Id] FROM [dbo].[Categories] WHERE [Slug] = N'trang-diem';

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'chong-nang')
        INSERT INTO [dbo].[Categories] ([Id], [Name], [Slug], [Description], [IsDeleted], [DeletedAt], [CreatedAt], [UpdatedAt])
        VALUES (@CatSunscreen, N'Chống nắng', N'chong-nang', N'Kem chống nắng vật lý, hóa học và sữa chống nắng kiềm dầu.', 0, NULL, @Now, NULL);
    ELSE
        SELECT @CatSunscreen = [Id] FROM [dbo].[Categories] WHERE [Slug] = N'chong-nang';

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'cham-soc-toc')
        INSERT INTO [dbo].[Categories] ([Id], [Name], [Slug], [Description], [IsDeleted], [DeletedAt], [CreatedAt], [UpdatedAt])
        VALUES (@CatHaircare, N'Chăm sóc tóc', N'cham-soc-toc', N'Dầu gội phục hồi, dầu xả, tinh dầu dưỡng tóc suôn mượt.', 0, NULL, @Now, NULL);
    ELSE
        SELECT @CatHaircare = [Id] FROM [dbo].[Categories] WHERE [Slug] = N'cham-soc-toc';

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'nuoc-hoa')
        INSERT INTO [dbo].[Categories] ([Id], [Name], [Slug], [Description], [IsDeleted], [DeletedAt], [CreatedAt], [UpdatedAt])
        VALUES (@CatPerfume, N'Nước hoa', N'nuoc-hoa', N'Nước hoa Eau de Parfum và xịt thơm toàn thân lưu hương lâu.', 0, NULL, @Now, NULL);
    ELSE
        SELECT @CatPerfume = [Id] FROM [dbo].[Categories] WHERE [Slug] = N'nuoc-hoa';

    -- =================================================================
    -- 4. SEED 15 PRODUCTS
    -- =================================================================
    DECLARE @Products TABLE (
        [Id] UNIQUEIDENTIFIER, [CategoryId] UNIQUEIDENTIFIER, [Name] NVARCHAR(200),
        [Description] NVARCHAR(2000), [Sku] NVARCHAR(64), [Price] DECIMAL(18, 2),
        [StockQuantity] INT, [Status] NVARCHAR(32), [IsDeleted] BIT, [DeletedAt] DATETIMEOFFSET
    );

    INSERT INTO @Products VALUES
        ('B1000001-0000-0000-0000-000000000001', @CatSkincare,  N'Nước Tẩy Trang Bioderma Sensibio H2O 500ml',       N'Nước tẩy trang dịu nhẹ cho da nhạy cảm, làm sạch sâu lớp trang điểm và bụi bẩn.',   'COS-SKN-001', 425000.00,  45, N'Active',     0, NULL),
        ('B1000001-0000-0000-0000-000000000002', @CatSkincare,  N'Sữa Rửa Mặt CeraVe Foaming Cleanser 473ml',        N'Sữa rửa mặt tạo bọt cho da dầu mụn chứa Ceramide và Niacinamide.',                 'COS-SKN-002', 395000.00,  60, N'Active',     0, NULL),
        ('B1000001-0000-0000-0000-000000000003', @CatSkincare,  N'Serum The Ordinary Niacinamide 10% + Zinc 1% 30ml',N'Tinh chất giảm thâm mụn, kiểm soát dầu thừa và thu nhỏ lỗ chân lông.',             'COS-SKN-003', 245000.00,  80, N'Active',     0, NULL),
        ('B1000001-0000-0000-0000-000000000004', @CatSkincare,  N'Kem Dưỡng Ẩm La Roche-Posay Cicaplast Baume B5+',  N'Kem dưỡng phục hồi da tổn thương, làm dịu kích ứng chứa Vitamin B5.',              'COS-SKN-004', 360000.00,  35, N'Active',     0, NULL),
        ('B1000001-0000-0000-0000-000000000005', @CatSkincare,  N'Toner Klairs Supple Preparation Unscented 180ml',  N'Nước hoa hồng cấp ẩm không hương liệu giúp cân bằng độ pH cho da nhạy cảm.',       'COS-SKN-005', 310000.00,  25, N'Active',     0, NULL),
        ('B2000002-0000-0000-0000-000000000001', @CatMakeup,    N'Son Kem Lì 3CE Velvet Lip Tint #Taupe',            N'Son kem lì mịn mượt màu đỏ nâu gạch thời thượng, bám màu tốt.',                    'COS-MKP-001', 290000.00,  50, N'Active',     0, NULL),
        ('B2000002-0000-0000-0000-000000000002', @CatMakeup,    N'Phấn Nước Laneige Neo Cushion Matte #21N',         N'Cushion kiềm dầu 24h che phủ khuyết điểm mịn lì tự nhiên.',                        'COS-MKP-002', 690000.00,  20, N'Active',     0, NULL),
        ('B2000002-0000-0000-0000-000000000003', @CatMakeup,    N'Kem Nền Maybelline Fit Me Matte + Poreless 30ml',  N'Kem nền kiềm dầu che phủ lỗ chân lông phù hợp da thường đến da dầu.',              'COS-MKP-003', 255000.00,  40, N'Active',     0, NULL),
        ('B2000002-0000-0000-0000-000000000004', @CatMakeup,    N'Son Dưỡng Có Màu Dior Addict Lip Glow #001 Pink',  N'Son dưỡng cao cấp giúp môi mềm mại căng mọng với sắc hồng tự nhiên.',              'COS-MKP-004', 920000.00,   0, N'OutOfStock', 0, NULL),
        ('B3000003-0000-0000-0000-000000000001', @CatSunscreen, N'Sữa Chống Nắng Anessa Perfect UV Milk 60ml',       N'Sữa chống nắng SPF50+ PA++++ chống nước, kiềm dầu bền vững suốt ngày dài.',        'COS-SUN-001', 540000.00,  30, N'Active',     0, NULL),
        ('B3000003-0000-0000-0000-000000000002', @CatSunscreen, N'Kem Chống Nắng Skin1004 Madagascar Centella 50ml',N'Kem chống nắng rau má dịu nhẹ, nâng tông tự nhiên không gây bí da.',                'COS-SUN-002', 285000.00,  55, N'Active',     0, NULL),
        ('B4000004-0000-0000-0000-000000000001', @CatHaircare,  N'Tinh Dầu Dưỡng Tóc Moroccanoil Treatment 100ml',   N'Dầu dưỡng tóc chiết xuất Argan phục hồi tóc hư tổn, giúp tóc bóng mượt.',          'COS-HAR-001', 890000.00,  18, N'Active',     0, NULL),
        ('B4000004-0000-0000-0000-000000000002', @CatHaircare,  N'Dầu Gội Tsubaki Premium Moist & Repair 490ml',     N'Dầu gội dưỡng ẩm phục hồi tóc khô xơ chuẩn salon Nhật Bản.',                       'COS-HAR-002', 185000.00,  70, N'Active',     0, NULL),
        ('B5000005-0000-0000-0000-000000000001', @CatPerfume,   N'Nước Hoa YSL Libre Eau de Parfum 50ml',            N'Hương hoa oải hương Pháp kết hợp hoa cam Maroc sang trọng và quyến rũ.',            'COS-PRF-001', 2850000.00, 10, N'Active',     0, NULL),
        ('B5000005-0000-0000-0000-000000000002', @CatPerfume,   N'Nước Hoa Chanel Coco Mademoiselle EDP (Archived)', N'Mẫu thử nghiệm đã ngừng bán dùng để test Soft Delete & SKU Unique Filter.',          'COS-PRF-OLD', 2650000.00,  0, N'OutOfStock', 1, @Now);

    INSERT INTO [dbo].[Products] ([Id], [CategoryId], [Name], [Description], [Sku], [Price], [StockQuantity], [Status], [IsDeleted], [DeletedAt], [CreatedAt], [UpdatedAt])
    SELECT p.[Id], p.[CategoryId], p.[Name], p.[Description], p.[Sku], p.[Price], p.[StockQuantity], p.[Status], p.[IsDeleted], p.[DeletedAt], @Now, NULL
    FROM @Products p
    WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Products] existing WHERE existing.[Sku] = p.[Sku]);

    -- =================================================================
    -- 5. SEED 2 SAMPLE COUPONS
    -- =================================================================
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Coupons] WHERE [Code] = N'SALE10')
        INSERT INTO [dbo].[Coupons] ([Id], [Code], [DiscountType], [Value], [MinOrderValue], [StartDate], [EndDate], [UsageLimit], [UsedCount], [IsActive], [CreatedAt], [UpdatedAt])
        VALUES ('C1111111-1111-1111-1111-111111111111', N'SALE10', N'Percentage', 10.00, 100000.00, @Now, DATEADD(YEAR, 1, @Now), 1000, 0, 1, @Now, NULL);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Coupons] WHERE [Code] = N'GIAM50K')
        INSERT INTO [dbo].[Coupons] ([Id], [Code], [DiscountType], [Value], [MinOrderValue], [StartDate], [EndDate], [UsageLimit], [UsedCount], [IsActive], [CreatedAt], [UpdatedAt])
        VALUES ('C2222222-2222-2222-2222-222222222222', N'GIAM50K', N'FixedAmount', 50000.00, 300000.00, @Now, DATEADD(YEAR, 1, @Now), 500, 0, 1, @Now, NULL);

    COMMIT TRANSACTION;
    PRINT N'Seed dữ liệu hoàn tất thành công!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Kiểm tra nhanh số lượng bản ghi sau khi seed
SELECT N'Accounts' AS [Table], COUNT(*) AS [Count] FROM [dbo].[Accounts]
UNION ALL SELECT N'Users', COUNT(*) FROM [dbo].[Users]
UNION ALL SELECT N'Categories', COUNT(*) FROM [dbo].[Categories]
UNION ALL SELECT N'Products', COUNT(*) FROM [dbo].[Products]
UNION ALL SELECT N'Coupons', COUNT(*) FROM [dbo].[Coupons];
GO
