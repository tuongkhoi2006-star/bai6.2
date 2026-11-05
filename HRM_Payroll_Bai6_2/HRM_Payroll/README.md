# HRM & Payroll System - Bài 6.2

Bộ source mẫu C# Windows Forms + SQL Server, tổ chức theo mô hình 3 tầng (GUI / BLL / DAL).

## 1. Công cụ cần có

- Visual Studio 2019 hoặc 2022, cài workload **.NET desktop development**.
- .NET Framework 4.8 Developer Pack.
- SQL Server (Express/Developer) và SQL Server Management Studio (SSMS).

## 2. Tạo cơ sở dữ liệu

1. Mở SSMS và kết nối tới SQL Server của bạn.
2. Mở file `Database/HRMDB.sql`.
3. Nhấn **Execute** (F5). Script tạo CSDL `HRMDB`, 4 bảng chính, các khóa/ràng buộc, trigger khóa chấm công và 4 phòng ban mẫu.

## 3. Cấu hình kết nối

Mở file `App.config`, sửa `Data Source` cho đúng SQL Server trên máy:

```xml
<add name="HRMDB"
     connectionString="Data Source=.;Initial Catalog=HRMDB;Integrated Security=True;TrustServerCertificate=True;"
     providerName="System.Data.SqlClient" />
```

Ví dụ:

- SQL Server mặc định: `Data Source=.`
- SQL Server Express: `Data Source=.\\SQLEXPRESS`
- LocalDB: `Data Source=(LocalDB)\\MSSQLLocalDB`

Nếu dùng SQL Authentication, thay `Integrated Security=True` bằng `User ID=...;Password=...`.

## 4. Mở và chạy chương trình

1. Mở `HRM_Payroll.sln` bằng Visual Studio.
2. Kiểm tra `App.config` đã trỏ đúng SQL Server.
3. Chọn **Build > Build Solution**.
4. Nhấn **F5** để chạy.

Giao diện gồm 3 tab:

- **Hồ sơ nhân viên:** thêm, sửa, xóa, tìm theo họ tên/CCCD/mã nhân viên, lọc phòng ban, chuyển phòng ban. Có thể thêm phòng ban bằng nút `Thêm phòng ban`.
- **Chấm công:** chọn tháng/năm, nạp toàn bộ nhân viên, nhập ngày công và giờ OT trực tiếp trên DataGridView, lưu bằng `SqlCommandBuilder` và `SqlDataAdapter.Update()`.
- **Tính lương:** xem số liệu lương dự kiến; bấm `Chốt lương tháng` để lưu toàn bộ lương trong một `SqlTransaction`, đồng thời khóa chấm công của tháng.

## 5. Công thức lương

```text
ThucLinh = (LuongCoBan * HeSoLuong / 22) * SoNgayCong
           + (SoGioOT * 50,000)
```

Phần xem bảng lương hiển thị số tiền dự kiến trước khi chốt. Sau khi chốt, giá trị được lưu cố định vào `BangLuong`.

## 6. Những điểm cần chú ý khi bảo vệ bài

- CRUD nhân viên nằm ở `NhanVienDAL.cs` và `NhanVienBLL.cs`; các câu truy vấn đều dùng parameter.
- `ChamCongDAL.LoadMonth()` dùng `SqlDataAdapter` nạp bảng chấm công hiện có, đồng thời dùng một `SqlDataAdapter` khác nạp toàn bộ nhân viên để bổ sung dòng chưa có chấm công vào `DataTable`.
- `ChamCongDAL.SaveMonth()` dùng `SqlCommandBuilder` sinh các lệnh và gọi `SqlDataAdapter.Update(DataTable)` để đồng bộ dữ liệu.
- `BangLuongDAL.CloseMonth()` dùng `SqlTransaction` để kiểm tra, khóa chấm công và ghi bảng lương trong cùng giao dịch; có rollback nếu xảy ra lỗi.
- CSDL bổ sung cột `DaChot` cho `BangChamCong` và trigger để ngăn sửa/xóa dữ liệu đã chốt, đồng thời ngăn thêm chấm công vào tháng đã có bảng lương.

### Ghi chú về khóa chính BangLuong

Đề bài ghi đồng thời `MaLuong` là Identity/Primary Key và yêu cầu khóa chính `(MaNV, ThangNam)`. Hai yêu cầu này không thể cùng là khóa chính của một bảng. Script ưu tiên yêu cầu khóa chính kép `(MaNV, ThangNam)` để bảo đảm mỗi nhân viên chỉ có một bảng lương trong một tháng; `MaLuong` vẫn là Identity và có ràng buộc UNIQUE.

## 7. Dữ liệu demo gợi ý

Sau khi chạy chương trình, thêm vài nhân viên ở tab **Hồ sơ nhân viên**. Sau đó chuyển sang **Chấm công**, chọn tháng, nhập số ngày công/OT và bấm lưu. Cuối cùng sang **Tính lương**, xem bảng dự kiến rồi chốt tháng để kiểm tra trạng thái khóa.
