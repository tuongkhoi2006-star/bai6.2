using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace HRM_Payroll.DAL
{
    public class ChamCongDAL : IDisposable
    {
        private SqlConnection _currentConnection;
        private SqlDataAdapter _currentAdapter;
        private string _currentMonth;

        public bool IsMonthClosed(string month)
        {
            const string sql = @"SELECT CASE WHEN
                EXISTS(SELECT 1 FROM dbo.BangLuong WHERE ThangNam=@ThangNam)
                OR EXISTS(SELECT 1 FROM dbo.BangChamCong WHERE ThangNam=@ThangNam AND DaChot=1)
                THEN 1 ELSE 0 END;";
            using (SqlConnection conn = Db.CreateConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@ThangNam", SqlDbType.VarChar, 7).Value = month;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
        }

        public DataTable LoadMonth(string month)
        {
            DisposeCurrentAdapter();
            _currentMonth = month;
            _currentConnection = Db.CreateConnection();

            const string attendanceSql = @"SELECT MaCong, MaNV, ThangNam, SoNgayCong, SoGioOT, DaChot
FROM dbo.BangChamCong WHERE ThangNam=@ThangNam;";
            SqlCommand selectCommand = new SqlCommand(attendanceSql, _currentConnection);
            selectCommand.Parameters.Add("@ThangNam", SqlDbType.VarChar, 7).Value = month;
            _currentAdapter = new SqlDataAdapter(selectCommand);
            _currentAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

            DataTable table = new DataTable("BangChamCong");
            _currentAdapter.FillSchema(table, SchemaType.Source);
            _currentAdapter.Fill(table);

            /* Nạp toàn bộ nhân viên bằng SqlDataAdapter để tạo đủ dòng chấm công,
               kể cả nhân viên chưa có bản ghi trong tháng đang chọn. */
            DataTable employees = new DataTable("NhanVien");
            const string employeeSql = @"SELECT nv.MaNV, nv.HoTen, pb.TenPB
FROM dbo.NhanVien nv INNER JOIN dbo.PhongBan pb ON pb.MaPB=nv.MaPB
ORDER BY nv.HoTen;";
            using (SqlDataAdapter employeeAdapter = new SqlDataAdapter(employeeSql, _currentConnection))
            {
                employeeAdapter.Fill(employees);
            }

            table.Columns.Add("HoTen", typeof(string));
            table.Columns.Add("TenPB", typeof(string));

            Dictionary<int, DataRow> employeeMap = new Dictionary<int, DataRow>();
            foreach (DataRow employee in employees.Rows)
                employeeMap[Convert.ToInt32(employee["MaNV"])] = employee;

            HashSet<int> existingEmployeeIds = new HashSet<int>();
            foreach (DataRow attendance in table.Rows)
            {
                int maNV = Convert.ToInt32(attendance["MaNV"]);
                existingEmployeeIds.Add(maNV);
                DataRow employee;
                if (employeeMap.TryGetValue(maNV, out employee))
                {
                    attendance["HoTen"] = employee["HoTen"];
                    attendance["TenPB"] = employee["TenPB"];
                }
                else
                {
                    attendance["HoTen"] = "(Nhân viên không còn hoạt động)";
                    attendance["TenPB"] = string.Empty;
                }
            }

            foreach (DataRow employee in employees.Rows)
            {
                int maNV = Convert.ToInt32(employee["MaNV"]);
                if (existingEmployeeIds.Contains(maNV)) continue;

                DataRow newRow = table.NewRow();
                newRow["MaNV"] = maNV;
                newRow["ThangNam"] = month;
                newRow["SoNgayCong"] = 0.0;
                newRow["SoGioOT"] = 0.0;
                newRow["DaChot"] = false;
                newRow["HoTen"] = employee["HoTen"];
                newRow["TenPB"] = employee["TenPB"];
                table.Rows.Add(newRow);
            }

            return table;
        }

        public int SaveMonth(DataTable table, string month)
        {
            if (_currentAdapter == null || _currentMonth != month)
                throw new InvalidOperationException("Hãy nạp lại bảng chấm công trước khi lưu.");
            if (IsMonthClosed(month))
                throw new InvalidOperationException("Tháng này đã chốt lương, không thể lưu chấm công.");

            foreach (DataRow row in table.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;
                double days = Convert.ToDouble(row["SoNgayCong"]);
                double overtime = Convert.ToDouble(row["SoGioOT"]);
                if (days < 0 || days > 31)
                    throw new InvalidOperationException("Số ngày công phải nằm trong khoảng 0 đến 31.");
                if (overtime < 0)
                    throw new InvalidOperationException("Số giờ OT không được âm.");
            }

            /* CommandBuilder tự sinh INSERT/UPDATE/DELETE từ SELECT một bảng.
               HoTen và TenPB là cột hiển thị bổ sung, không được gửi xuống SQL. */
            using (SqlCommandBuilder builder = new SqlCommandBuilder(_currentAdapter))
            {
                _currentAdapter.InsertCommand = builder.GetInsertCommand();
                _currentAdapter.UpdateCommand = builder.GetUpdateCommand();
                _currentAdapter.DeleteCommand = builder.GetDeleteCommand();
                return _currentAdapter.Update(table);
            }
        }

        private void DisposeCurrentAdapter()
        {
            if (_currentAdapter != null)
            {
                _currentAdapter.Dispose();
                _currentAdapter = null;
            }
            if (_currentConnection != null)
            {
                _currentConnection.Dispose();
                _currentConnection = null;
            }
        }

        public void Dispose()
        {
            DisposeCurrentAdapter();
        }
    }
}
