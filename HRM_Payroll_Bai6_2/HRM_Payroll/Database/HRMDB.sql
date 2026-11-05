/* ============================================================
   HRMDB - He thong quan ly nhan su va tinh luong
   Chay file nay trong SQL Server Management Studio (SSMS).
   ============================================================ */
IF DB_ID(N'HRMDB') IS NULL
BEGIN
    CREATE DATABASE HRMDB;
END
GO

USE HRMDB;
GO

IF OBJECT_ID(N'dbo.PhongBan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PhongBan
    (
        MaPB INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PhongBan PRIMARY KEY,
        TenPB NVARCHAR(100) NOT NULL,
        Mota NVARCHAR(255) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.NhanVien', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NhanVien
    (
        MaNV INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NhanVien PRIMARY KEY,
        HoTen NVARCHAR(100) NOT NULL,
        NgaySinh DATETIME NULL,
        CCCD VARCHAR(20) NOT NULL,
        MaPB INT NOT NULL,
        LuongCoBan DECIMAL(18,2) NOT NULL,
        HeSoLuong FLOAT NOT NULL CONSTRAINT DF_NhanVien_HeSoLuong DEFAULT (1.0),
        CONSTRAINT UQ_NhanVien_CCCD UNIQUE (CCCD),
        CONSTRAINT CK_NhanVien_LuongCoBan CHECK (LuongCoBan > 0),
        CONSTRAINT CK_NhanVien_HeSoLuong CHECK (HeSoLuong > 0),
        CONSTRAINT FK_NhanVien_PhongBan FOREIGN KEY (MaPB) REFERENCES dbo.PhongBan(MaPB)
    );
END
GO

IF OBJECT_ID(N'dbo.BangChamCong', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BangChamCong
    (
        MaCong INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BangChamCong PRIMARY KEY,
        MaNV INT NOT NULL,
        ThangNam VARCHAR(7) NOT NULL,
        SoNgayCong FLOAT NOT NULL CONSTRAINT DF_BangChamCong_SoNgayCong DEFAULT (0),
        SoGioOT FLOAT NOT NULL CONSTRAINT DF_BangChamCong_SoGioOT DEFAULT (0),
        DaChot BIT NOT NULL CONSTRAINT DF_BangChamCong_DaChot DEFAULT (0),
        CONSTRAINT CK_BangChamCong_SoNgayCong CHECK (SoNgayCong >= 0 AND SoNgayCong <= 31),
        CONSTRAINT CK_BangChamCong_SoGioOT CHECK (SoGioOT >= 0),
        CONSTRAINT UQ_BangChamCong_NhanVien_Thang UNIQUE (MaNV, ThangNam),
        CONSTRAINT FK_BangChamCong_NhanVien FOREIGN KEY (MaNV) REFERENCES dbo.NhanVien(MaNV)
    );
END
GO

IF OBJECT_ID(N'dbo.BangLuong', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BangLuong
    (
        MaLuong INT IDENTITY(1,1) NOT NULL,
        MaNV INT NOT NULL,
        ThangNam VARCHAR(7) NOT NULL,
        ThucLinh DECIMAL(18,2) NOT NULL,
        /* De bai co 2 yeu cau PK mau thuan:
           MaLuong Identity va PK(MaNV, ThangNam).
           Uu tien PK kep theo dong quy dinh cuoi; MaLuong van Identity + UNIQUE. */
        CONSTRAINT PK_BangLuong PRIMARY KEY (MaNV, ThangNam),
        CONSTRAINT UQ_BangLuong_MaLuong UNIQUE (MaLuong),
        CONSTRAINT CK_BangLuong_ThucLinh CHECK (ThucLinh >= 0),
        CONSTRAINT FK_BangLuong_NhanVien FOREIGN KEY (MaNV) REFERENCES dbo.NhanVien(MaNV)
    );
END
GO

/* Du lieu phong ban mau de co the chay ung dung ngay. */
IF NOT EXISTS (SELECT 1 FROM dbo.PhongBan)
BEGIN
    INSERT INTO dbo.PhongBan(TenPB, Mota) VALUES
    (N'Ban Giám đốc', N'Quản lý và điều hành'),
    (N'Phòng Nhân sự', N'Quản lý hồ sơ và chính sách nhân sự'),
    (N'Phòng Kế toán', N'Kế toán và tiền lương'),
    (N'Phòng Công nghệ', N'Phát triển và vận hành công nghệ');
END
GO

/* Ngăn thêm chấm công cho tháng đã chốt lương. */
IF OBJECT_ID(N'dbo.TR_BangChamCong_BlockInsertClosedMonth', N'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_BangChamCong_BlockInsertClosedMonth;
GO
CREATE TRIGGER dbo.TR_BangChamCong_BlockInsertClosedMonth
ON dbo.BangChamCong
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.BangLuong bl
            ON bl.MaNV = i.MaNV AND bl.ThangNam = i.ThangNam
    )
    BEGIN
        THROW 51001, N'Tháng này đã chốt lương, không thể thêm chấm công.', 1;
    END
END
GO

/* Khi DaChot=1, không cho sửa/xóa dữ liệu chấm công của dòng đó.
   Việc chuyển DaChot từ 0 sang 1 để chốt lương vẫn được phép. */
IF OBJECT_ID(N'dbo.TR_BangChamCong_BlockUpdateDeleteClosed', N'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_BangChamCong_BlockUpdateDeleteClosed;
GO
CREATE TRIGGER dbo.TR_BangChamCong_BlockUpdateDeleteClosed
ON dbo.BangChamCong
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS
    (
        SELECT 1
        FROM deleted d
        LEFT JOIN inserted i ON i.MaCong = d.MaCong
        WHERE d.DaChot = 1
          AND
          (
              i.MaCong IS NULL
              OR ISNULL(i.MaNV, -1) <> ISNULL(d.MaNV, -1)
              OR ISNULL(i.ThangNam, '') <> ISNULL(d.ThangNam, '')
              OR ISNULL(i.SoNgayCong, -1) <> ISNULL(d.SoNgayCong, -1)
              OR ISNULL(i.SoGioOT, -1) <> ISNULL(d.SoGioOT, -1)
              OR ISNULL(i.DaChot, 0) <> ISNULL(d.DaChot, 0)
          )
    )
    BEGIN
        THROW 51002, N'Dữ liệu chấm công đã khóa do tháng đã chốt lương.', 1;
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_BangChamCong_ThangNam' AND object_id=OBJECT_ID(N'dbo.BangChamCong'))
    CREATE INDEX IX_BangChamCong_ThangNam ON dbo.BangChamCong(ThangNam);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_NhanVien_MaPB' AND object_id=OBJECT_ID(N'dbo.NhanVien'))
    CREATE INDEX IX_NhanVien_MaPB ON dbo.NhanVien(MaPB);
GO

PRINT N'Tao CSDL HRMDB thanh cong.';
GO
