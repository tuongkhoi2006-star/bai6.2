using System;
using System.Data;
using HRM_Payroll.DAL;
using HRM_Payroll.Models;

namespace HRM_Payroll.BLL
{
    public class NhanVienBLL
    {
        private readonly NhanVienDAL _dal = new NhanVienDAL();

        public DataTable Search(string keyword, int? maPB)
        {
            return _dal.Search(keyword, maPB);
        }

        public void Add(NhanVien nv)
        {
            Validate(nv);
            _dal.Insert(nv);
        }

        public void Update(NhanVien nv)
        {
            if (nv.MaNV <= 0) throw new ArgumentException("Mã nhân viên không hợp lệ.");
            Validate(nv);
            _dal.Update(nv);
        }

        public void Delete(int maNV)
        {
            if (maNV <= 0) throw new ArgumentException("Vui lòng chọn nhân viên cần xóa.");
            _dal.Delete(maNV);
        }

        private static void Validate(NhanVien nv)
        {
            if (nv == null) throw new ArgumentNullException("nv");
            if (string.IsNullOrWhiteSpace(nv.HoTen)) throw new ArgumentException("Họ tên không được để trống.");
            if (string.IsNullOrWhiteSpace(nv.CCCD)) throw new ArgumentException("CCCD không được để trống.");
            if (nv.CCCD.Trim().Length > 20) throw new ArgumentException("CCCD tối đa 20 ký tự.");
            if (nv.MaPB <= 0) throw new ArgumentException("Vui lòng chọn phòng ban.");
            if (nv.LuongCoBan <= 0) throw new ArgumentException("Lương cơ bản phải lớn hơn 0.");
            if (nv.HeSoLuong <= 0) throw new ArgumentException("Hệ số lương phải lớn hơn 0.");
            if (nv.NgaySinh.HasValue && nv.NgaySinh.Value.Date > DateTime.Today)
                throw new ArgumentException("Ngày sinh không được ở tương lai.");
        }
    }
}
