using System;
using System.Data;
using System.Data.SqlClient;

namespace HRM_Payroll.DAL
{
    public class BangLuongDAL
    {
        public DataTable GetMonth(string month)
        {
            const string sql = @"
SELECT nv.MaNV, nv.HoTen, pb.TenPB, cc.ThangNam,
       nv.LuongCoBan, nv.HeSoLuong, cc.SoNgayCong, cc.SoGioOT,
       CAST(COALESCE(bl.ThucLinh,
           ((nv.LuongCoBan * nv.HeSoLuong / 22.0) * cc.SoNgayCong)
           + (cc.SoGioOT * 50000.0)) AS DECIMAL(18,2)) AS ThucLinh,
       CASE WHEN bl.MaNV IS NULL THEN N'Chưa chốt' ELSE N'Đã chốt' END AS TrangThai
FROM dbo.BangChamCong cc
INNER JOIN dbo.NhanVien nv ON nv.MaNV=cc.MaNV
INNER JOIN dbo.PhongBan pb ON pb.MaPB=nv.MaPB
LEFT JOIN dbo.BangLuong bl ON bl.MaNV=cc.MaNV AND bl.ThangNam=cc.ThangNam
WHERE cc.ThangNam=@ThangNam
ORDER BY nv.MaNV;";
            using (SqlConnection conn = Db.CreateConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@ThangNam", SqlDbType.VarChar, 7).Value = month;
                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public int CloseMonth(string month)
        {
            using (SqlConnection conn = Db.CreateConnection())
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        using (SqlCommand check = new SqlCommand(
                            "SELECT COUNT(*) FROM dbo.BangLuong WHERE ThangNam=@ThangNam;", conn, transaction))
                        {
                            check.Parameters.Add("@ThangNam", SqlDbType.VarChar, 7).Value = month;
                            if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                                throw new InvalidOperationException("Tháng này đã được chốt lương trước đó.");
                        }

                        int attendanceCount;
                        using (SqlCommand checkAttendance = new SqlCommand(
                            "SELECT COUNT(*) FROM dbo.BangChamCong WHERE ThangNam=@ThangNam;", conn, transaction))
                        {
                            checkAttendance.Parameters.Add("@ThangNam", SqlDbType.VarChar, 7).Value = month;
                            attendanceCount = Convert.ToInt32(checkAttendance.ExecuteScalar());
                        }
                        if (attendanceCount == 0)
                            throw new InvalidOperationException("Chưa có dữ liệu chấm công cho tháng này.");

                        using (SqlCommand checkLocked = new SqlCommand(
                            "SELECT COUNT(*) FROM dbo.BangChamCong WHERE ThangNam=@ThangNam AND DaChot=1;", conn, transaction))
                        {
                            checkLocked.Parameters.Add("@ThangNam", SqlDbType.VarChar, 7).Value = month;
                            if (Convert.ToInt32(checkLocked.ExecuteScalar()) > 0)
                                throw new InvalidOperationException("Dữ liệu chấm công tháng này đã bị khóa.");
                        }

                        /* Khóa chấm công trong cùng transaction với việc ghi bảng lương. */
                        using (SqlCommand lockAttendance = new SqlCommand(
                            "UPDATE dbo.BangChamCong SET DaChot=1 WHERE ThangNam=@ThangNam;", conn, transaction))
                        {
                            lockAttendance.Parameters.Add("@ThangNam", SqlDbType.VarChar, 7).Value = month;
                            lockAttendance.ExecuteNonQuery();
                        }

                        const string insertSql = @"
INSERT INTO dbo.BangLuong(MaNV, ThangNam, ThucLinh)
SELECT nv.MaNV, cc.ThangNam,
       CAST(((nv.LuongCoBan * nv.HeSoLuong / 22.0) * cc.SoNgayCong)
            + (cc.SoGioOT * 50000.0) AS DECIMAL(18,2))
FROM dbo.BangChamCong cc
INNER JOIN dbo.NhanVien nv ON nv.MaNV=cc.MaNV
WHERE cc.ThangNam=@ThangNam;";
                        int inserted;
                        using (SqlCommand insert = new SqlCommand(insertSql, conn, transaction))
                        {
                            insert.Parameters.Add("@ThangNam", SqlDbType.VarChar, 7).Value = month;
                            inserted = insert.ExecuteNonQuery();
                        }
                        if (inserted != attendanceCount)
                            throw new InvalidOperationException("Số dòng bảng lương tạo ra không khớp số dòng chấm công.");

                        transaction.Commit();
                        return inserted;
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { /* giữ nguyên lỗi gốc */ }
                        throw;
                    }
                }
            }
        }
    }
}
