using System;

namespace HRM_Payroll.Models
{
    public class NhanVien
    {
        public int MaNV { get; set; }
        public string HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string CCCD { get; set; }
        public int MaPB { get; set; }
        public decimal LuongCoBan { get; set; }
        public double HeSoLuong { get; set; }
    }
}
