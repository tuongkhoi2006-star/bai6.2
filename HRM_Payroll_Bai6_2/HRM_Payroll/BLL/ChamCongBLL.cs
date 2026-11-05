using System;
using System.Data;
using System.Globalization;
using HRM_Payroll.DAL;

namespace HRM_Payroll.BLL
{
    public class ChamCongBLL
    {
        private readonly ChamCongDAL _dal;

        public ChamCongBLL(ChamCongDAL dal)
        {
            _dal = dal;
        }

        public DataTable LoadMonth(string month)
        {
            ValidateMonth(month);
            return _dal.LoadMonth(month);
        }

        public int SaveMonth(DataTable table, string month)
        {
            ValidateMonth(month);
            if (table == null) throw new ArgumentNullException("table");
            return _dal.SaveMonth(table, month);
        }

        public bool IsMonthClosed(string month)
        {
            ValidateMonth(month);
            return _dal.IsMonthClosed(month);
        }

        internal static void ValidateMonth(string month)
        {
            DateTime parsed;
            if (!DateTime.TryParseExact(month, "MM/yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out parsed))
                throw new ArgumentException("Tháng/năm không đúng định dạng MM/yyyy.");
        }
    }
}
