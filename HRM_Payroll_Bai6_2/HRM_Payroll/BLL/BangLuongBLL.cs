using System;
using System.Data;
using HRM_Payroll.DAL;

namespace HRM_Payroll.BLL
{
    public class BangLuongBLL
    {
        private readonly BangLuongDAL _dal = new BangLuongDAL();

        public DataTable GetMonth(string month)
        {
            ChamCongBLL.ValidateMonth(month);
            return _dal.GetMonth(month);
        }

        public int CloseMonth(string month)
        {
            ChamCongBLL.ValidateMonth(month);
            return _dal.CloseMonth(month);
        }
    }
}
