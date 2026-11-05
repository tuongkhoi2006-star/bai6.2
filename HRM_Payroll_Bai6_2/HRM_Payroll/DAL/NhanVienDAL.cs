using System;
using System.Data;
using System.Data.SqlClient;
using HRM_Payroll.Models;

namespace HRM_Payroll.DAL
{
    public class NhanVienDAL
    {
        private const string SelectSql = @"
SELECT nv.MaNV, nv.HoTen, nv.NgaySinh, nv.CCCD, nv.MaPB,
       pb.TenPB, nv.LuongCoBan, nv.HeSoLuong
FROM dbo.NhanVien nv
INNER JOIN dbo.PhongBan pb ON pb.MaPB = nv.MaPB";

        public DataTable Search(string keyword, int? maPB)
        {
            string sql = SelectSql + @"
WHERE (@Keyword = N'' OR nv.HoTen LIKE @LikeKeyword OR nv.CCCD LIKE @LikeKeyword
       OR CONVERT(VARCHAR(20), nv.MaNV) LIKE @LikeKeyword)
  AND (@MaPB IS NULL OR nv.MaPB = @MaPB)
ORDER BY nv.MaNV DESC;";

            using (SqlConnection conn = Db.CreateConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
            {
                string safeKeyword = (keyword ?? string.Empty).Trim();
                cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar, 100).Value = safeKeyword;
                cmd.Parameters.Add("@LikeKeyword", SqlDbType.NVarChar, 120).Value = "%" + safeKeyword + "%";
                SqlParameter departmentParameter = cmd.Parameters.Add("@MaPB", SqlDbType.Int);
                departmentParameter.Value = maPB.HasValue ? (object)maPB.Value : DBNull.Value;

                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public int Insert(NhanVien nv)
        {
            const string sql = @"INSERT INTO dbo.NhanVien
(HoTen, NgaySinh, CCCD, MaPB, LuongCoBan, HeSoLuong)
OUTPUT INSERTED.MaNV
VALUES(@HoTen, @NgaySinh, @CCCD, @MaPB, @LuongCoBan, @HeSoLuong);";
            using (SqlConnection conn = Db.CreateConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                AddParameters(cmd, nv);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Update(NhanVien nv)
        {
            const string sql = @"UPDATE dbo.NhanVien
SET HoTen=@HoTen, NgaySinh=@NgaySinh, CCCD=@CCCD, MaPB=@MaPB,
    LuongCoBan=@LuongCoBan, HeSoLuong=@HeSoLuong
WHERE MaNV=@MaNV;";
            using (SqlConnection conn = Db.CreateConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                AddParameters(cmd, nv);
                cmd.Parameters.Add("@MaNV", SqlDbType.Int).Value = nv.MaNV;
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException("Không tìm thấy nhân viên cần cập nhật.");
            }
        }

        public void Delete(int maNV)
        {
            const string sql = "DELETE FROM dbo.NhanVien WHERE MaNV=@MaNV;";
            using (SqlConnection conn = Db.CreateConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@MaNV", SqlDbType.Int).Value = maNV;
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException("Không tìm thấy nhân viên cần xóa.");
            }
        }

        private static void AddParameters(SqlCommand cmd, NhanVien nv)
        {
            cmd.Parameters.Add("@HoTen", SqlDbType.NVarChar, 100).Value = nv.HoTen;
            cmd.Parameters.Add("@NgaySinh", SqlDbType.DateTime).Value = nv.NgaySinh.HasValue
                ? (object)nv.NgaySinh.Value : DBNull.Value;
            cmd.Parameters.Add("@CCCD", SqlDbType.VarChar, 20).Value = nv.CCCD;
            cmd.Parameters.Add("@MaPB", SqlDbType.Int).Value = nv.MaPB;
            SqlParameter salary = cmd.Parameters.Add("@LuongCoBan", SqlDbType.Decimal);
            salary.Precision = 18;
            salary.Scale = 2;
            salary.Value = nv.LuongCoBan;
            cmd.Parameters.Add("@HeSoLuong", SqlDbType.Float).Value = nv.HeSoLuong;
        }
    }
}
