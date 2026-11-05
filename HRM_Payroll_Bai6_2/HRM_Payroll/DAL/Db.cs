using System.Configuration;
using System.Data.SqlClient;

namespace HRM_Payroll.DAL
{
    public static class Db
    {
        public static SqlConnection CreateConnection()
        {
            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["HRMDB"];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
                throw new System.InvalidOperationException("Chưa cấu hình connection string HRMDB trong App.config.");
            return new SqlConnection(setting.ConnectionString);
        }
    }
}
