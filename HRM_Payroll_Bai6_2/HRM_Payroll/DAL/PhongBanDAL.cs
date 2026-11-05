using System.Data;
using System.Data.SqlClient;

namespace HRM_Payroll.DAL
{
    public class PhongBanDAL
    {
        public DataTable GetAll()
        {
            const string sql = "SELECT MaPB, TenPB FROM dbo.PhongBan ORDER BY TenPB;";
            using (SqlConnection conn = Db.CreateConnection())
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, conn))
            {
                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public void Insert(string tenPB, string moTa)
        {
            const string sql = "INSERT INTO dbo.PhongBan(TenPB, Mota) VALUES(@TenPB, @Mota);";
            using (SqlConnection conn = Db.CreateConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@TenPB", SqlDbType.NVarChar, 100).Value = tenPB;
                cmd.Parameters.Add("@Mota", SqlDbType.NVarChar, 255).Value = (object)moTa ?? System.DBNull.Value;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
