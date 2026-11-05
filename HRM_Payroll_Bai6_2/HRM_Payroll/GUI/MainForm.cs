using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using HRM_Payroll.BLL;
using HRM_Payroll.DAL;
using HRM_Payroll.Models;

namespace HRM_Payroll.GUI
{
    public class MainForm : Form
    {
        private readonly NhanVienBLL _nhanVienBLL = new NhanVienBLL();
        private readonly PhongBanDAL _phongBanDAL = new PhongBanDAL();
        private readonly ChamCongDAL _chamCongDAL = new ChamCongDAL();
        private readonly ChamCongBLL _chamCongBLL;
        private readonly BangLuongBLL _bangLuongBLL = new BangLuongBLL();

        private ComboBox cboFilterPB;
        private ComboBox cboEditPB;
        private TextBox txtSearch;
        private TextBox txtHoTen;
        private TextBox txtCCCD;
        private TextBox txtLuong;
        private TextBox txtHeSo;
        private DateTimePicker dtpNgaySinh;
        private DataGridView gridNhanVien;
        private int _selectedMaNV;
        private bool _suspendEmployeeSelection;

        private DateTimePicker dtpChamCong;
        private DataGridView gridChamCong;
        private Button btnSaveChamCong;
        private Label lblChamCongStatus;
        private DataTable _attendanceTable;

        private DateTimePicker dtpBangLuong;
        private DataGridView gridBangLuong;
        private Label lblBangLuongStatus;

        public MainForm()
        {
            _chamCongBLL = new ChamCongBLL(_chamCongDAL);
            Text = "HRM & Payroll System - Quản lý nhân sự và tính lương";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1050, 680);
            Size = new Size(1280, 820);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            BackColor = Color.WhiteSmoke;
            BuildInterface();
            Load += MainForm_Load;
            FormClosing += delegate { _chamCongDAL.Dispose(); };
        }

        private void BuildInterface()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.FromArgb(31, 78, 121) };
            Label title = new Label
            {
                Text = "HRM & PAYROLL SYSTEM",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(22, 15)
            };
            Label subtitle = new Label
            {
                Text = "Quản lý hồ sơ nhân sự • Chấm công • Tính lương",
                ForeColor = Color.FromArgb(220, 232, 245),
                AutoSize = true,
                Location = new Point(360, 26)
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            TabControl tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 6) };
            tabs.TabPages.Add(BuildEmployeeTab());
            tabs.TabPages.Add(BuildAttendanceTab());
            tabs.TabPages.Add(BuildPayrollTab());
            Controls.Add(tabs);
            Controls.Add(header);
        }

        private TabPage BuildEmployeeTab()
        {
            TabPage page = new TabPage("Hồ sơ nhân viên");
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(8)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 164));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            FlowLayoutPanel searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(4, 9, 4, 2),
                AutoScroll = true
            };
            searchPanel.Controls.Add(MakeLabel("Tìm kiếm:"));
            txtSearch = new TextBox { Width = 205, Height = 28 };
            txtSearch.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) SearchEmployees(); };
            searchPanel.Controls.Add(txtSearch);
            searchPanel.Controls.Add(MakeLabel("Phòng ban:"));
            cboFilterPB = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            searchPanel.Controls.Add(cboFilterPB);
            searchPanel.Controls.Add(MakeButton("Tìm", SearchEmployees));
            searchPanel.Controls.Add(MakeButton("Tải lại", delegate { txtSearch.Clear(); cboFilterPB.SelectedIndex = 0; SearchEmployees(); }));
            searchPanel.Controls.Add(MakeButton("Thêm", AddEmployee));
            searchPanel.Controls.Add(MakeButton("Cập nhật", UpdateEmployee));
            searchPanel.Controls.Add(MakeButton("Xóa", DeleteEmployee));
            searchPanel.Controls.Add(MakeButton("Mới", ClearEmployeeInputs));
            searchPanel.Controls.Add(MakeButton("Thêm phòng ban", AddDepartment));

            GroupBox inputGroup = new GroupBox { Text = "Thông tin nhân viên (chọn một dòng bên dưới để sửa / chuyển phòng ban)", Dock = DockStyle.Fill, Padding = new Padding(8) };
            FlowLayoutPanel fields = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(8, 12, 8, 4),
                AutoScroll = true
            };
            txtHoTen = new TextBox { Width = 190 };
            dtpNgaySinh = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
            txtCCCD = new TextBox { Width = 150 };
            cboEditPB = new ComboBox { Width = 175, DropDownStyle = ComboBoxStyle.DropDownList };
            txtLuong = new TextBox { Width = 125 };
            txtHeSo = new TextBox { Width = 95, Text = "1.0" };
            fields.Controls.Add(Field("Họ và tên", txtHoTen));
            fields.Controls.Add(Field("Ngày sinh", dtpNgaySinh));
            fields.Controls.Add(Field("CCCD", txtCCCD));
            fields.Controls.Add(Field("Phòng ban", cboEditPB));
            fields.Controls.Add(Field("Lương cơ bản", txtLuong));
            fields.Controls.Add(Field("Hệ số lương", txtHeSo));
            inputGroup.Controls.Add(fields);

            gridNhanVien = CreateGrid();
            gridNhanVien.SelectionChanged += GridNhanVien_SelectionChanged;
            gridNhanVien.DataError += Grid_DataError;

            layout.Controls.Add(searchPanel, 0, 0);
            layout.Controls.Add(inputGroup, 0, 1);
            layout.Controls.Add(gridNhanVien, 0, 2);
            page.Controls.Add(layout);
            return page;
        }

        private TabPage BuildAttendanceTab()
        {
            TabPage page = new TabPage("Chấm công (Disconnected Model)");
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(8) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            FlowLayoutPanel toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(5, 9, 4, 2), AutoScroll = true };
            toolbar.Controls.Add(MakeLabel("Tháng/năm:"));
            dtpChamCong = CreateMonthPicker();
            toolbar.Controls.Add(dtpChamCong);
            toolbar.Controls.Add(MakeButton("Nạp nhân viên", LoadAttendance));
            btnSaveChamCong = MakeButton("Lưu chấm công", SaveAttendance);
            toolbar.Controls.Add(btnSaveChamCong);
            lblChamCongStatus = new Label { AutoSize = true, Padding = new Padding(12, 7, 0, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            toolbar.Controls.Add(lblChamCongStatus);

            gridChamCong = CreateGrid();
            gridChamCong.AllowUserToAddRows = false;
            gridChamCong.AllowUserToDeleteRows = false;
            gridChamCong.DataError += Grid_DataError;
            gridChamCong.CellValidating += Attendance_CellValidating;
            layout.Controls.Add(toolbar, 0, 0);
            layout.Controls.Add(gridChamCong, 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        private TabPage BuildPayrollTab()
        {
            TabPage page = new TabPage("Tính lương & chốt lương");
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(8) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            FlowLayoutPanel toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(5, 9, 4, 2), AutoScroll = true };
            toolbar.Controls.Add(MakeLabel("Tháng/năm:"));
            dtpBangLuong = CreateMonthPicker();
            toolbar.Controls.Add(dtpBangLuong);
            toolbar.Controls.Add(MakeButton("Xem / tính thử", LoadPayroll));
            toolbar.Controls.Add(MakeButton("Chốt lương tháng", ClosePayroll));
            lblBangLuongStatus = new Label { AutoSize = true, Padding = new Padding(12, 7, 0, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            toolbar.Controls.Add(lblBangLuongStatus);

            gridBangLuong = CreateGrid();
            gridBangLuong.AllowUserToAddRows = false;
            gridBangLuong.ReadOnly = true;
            gridBangLuong.DataError += Grid_DataError;
            layout.Controls.Add(toolbar, 0, 0);
            layout.Controls.Add(gridBangLuong, 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                LoadDepartments();
                SearchEmployees();
                LoadAttendance();
                LoadPayroll();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadDepartments()
        {
            DataTable departments = _phongBanDAL.GetAll();
            List<DepartmentOption> filterOptions = new List<DepartmentOption>();
            filterOptions.Add(new DepartmentOption(null, "Tất cả phòng ban"));
            foreach (DataRow row in departments.Rows)
                filterOptions.Add(new DepartmentOption(Convert.ToInt32(row["MaPB"]), Convert.ToString(row["TenPB"])));

            cboFilterPB.DataSource = filterOptions;
            cboFilterPB.DisplayMember = "Name";
            cboFilterPB.SelectedIndex = 0;

            cboEditPB.DataSource = departments;
            cboEditPB.DisplayMember = "TenPB";
            cboEditPB.ValueMember = "MaPB";
            if (departments.Rows.Count > 0) cboEditPB.SelectedIndex = 0;
        }

        private void SearchEmployees()
        {
            DepartmentOption option = cboFilterPB == null ? null : cboFilterPB.SelectedItem as DepartmentOption;
            DataTable table = _nhanVienBLL.Search(txtSearch == null ? string.Empty : txtSearch.Text,
                option == null ? (int?)null : option.Id);
            gridNhanVien.DataSource = table;
            ConfigureEmployeeGrid();
        }

        private void ConfigureEmployeeGrid()
        {
            if (gridNhanVien.Columns.Count == 0) return;
            SetColumn(gridNhanVien, "MaNV", "Mã NV", 65);
            SetColumn(gridNhanVien, "HoTen", "Họ tên", 180);
            SetColumn(gridNhanVien, "NgaySinh", "Ngày sinh", 105);
            SetColumn(gridNhanVien, "CCCD", "CCCD", 125);
            SetColumn(gridNhanVien, "MaPB", "Mã PB", 65);
            SetColumn(gridNhanVien, "TenPB", "Phòng ban", 150);
            SetColumn(gridNhanVien, "LuongCoBan", "Lương cơ bản", 120);
            SetColumn(gridNhanVien, "HeSoLuong", "Hệ số", 75);
            gridNhanVien.Columns["LuongCoBan"].DefaultCellStyle.Format = "N0";
            gridNhanVien.Columns["HeSoLuong"].DefaultCellStyle.Format = "0.##";
            gridNhanVien.Columns["NgaySinh"].DefaultCellStyle.Format = "dd/MM/yyyy";
        }

        private void AddEmployee()
        {
            try
            {
                _nhanVienBLL.Add(ReadEmployeeInput());
                SearchEmployees();
                ClearEmployeeInputs();
                MessageBox.Show("Thêm nhân viên thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void UpdateEmployee()
        {
            try
            {
                if (_selectedMaNV <= 0) throw new InvalidOperationException("Vui lòng chọn nhân viên trong danh sách.");
                NhanVien nv = ReadEmployeeInput();
                nv.MaNV = _selectedMaNV;
                _nhanVienBLL.Update(nv);
                SearchEmployees();
                ClearEmployeeInputs();
                MessageBox.Show("Cập nhật nhân viên thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void DeleteEmployee()
        {
            try
            {
                if (_selectedMaNV <= 0) throw new InvalidOperationException("Vui lòng chọn nhân viên cần xóa.");
                DialogResult confirm = MessageBox.Show("Bạn chắc chắn muốn xóa nhân viên này?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;
                _nhanVienBLL.Delete(_selectedMaNV);
                SearchEmployees();
                ClearEmployeeInputs();
                MessageBox.Show("Đã xóa nhân viên.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private NhanVien ReadEmployeeInput()
        {
            decimal salary;
            double coefficient;
            if (!decimal.TryParse(txtLuong.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out salary))
                throw new ArgumentException("Lương cơ bản phải là số hợp lệ.");
            if (!double.TryParse(txtHeSo.Text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out coefficient))
                throw new ArgumentException("Hệ số lương phải là số hợp lệ.");
            if (cboEditPB.SelectedValue == null) throw new ArgumentException("Chưa có phòng ban. Hãy thêm phòng ban trước.");

            return new NhanVien
            {
                HoTen = txtHoTen.Text.Trim(),
                NgaySinh = dtpNgaySinh.Checked ? (DateTime?)dtpNgaySinh.Value.Date : null,
                CCCD = txtCCCD.Text.Trim(),
                MaPB = Convert.ToInt32(cboEditPB.SelectedValue),
                LuongCoBan = salary,
                HeSoLuong = coefficient
            };
        }

        private void GridNhanVien_SelectionChanged(object sender, EventArgs e)
        {
            if (_suspendEmployeeSelection) return;
            if (gridNhanVien.CurrentRow == null || gridNhanVien.CurrentRow.DataBoundItem == null) return;
            DataRowView row = gridNhanVien.CurrentRow.DataBoundItem as DataRowView;
            if (row == null) return;
            try
            {
                _selectedMaNV = Convert.ToInt32(row["MaNV"]);
                txtHoTen.Text = Convert.ToString(row["HoTen"]);
                txtCCCD.Text = Convert.ToString(row["CCCD"]);
                txtLuong.Text = Convert.ToDecimal(row["LuongCoBan"]).ToString("0.##", CultureInfo.CurrentCulture);
                txtHeSo.Text = Convert.ToDouble(row["HeSoLuong"]).ToString("0.##", CultureInfo.CurrentCulture);
                if (row["NgaySinh"] == DBNull.Value) dtpNgaySinh.Checked = false;
                else { dtpNgaySinh.Value = Convert.ToDateTime(row["NgaySinh"]); dtpNgaySinh.Checked = true; }
                cboEditPB.SelectedValue = Convert.ToInt32(row["MaPB"]);
            }
            catch { /* bỏ qua khi grid đang thay đổi nguồn dữ liệu */ }
        }

        private void ClearEmployeeInputs()
        {
            _suspendEmployeeSelection = true;
            _selectedMaNV = 0;
            txtHoTen.Clear();
            txtCCCD.Clear();
            txtLuong.Clear();
            txtHeSo.Text = "1.0";
            dtpNgaySinh.Checked = false;
            if (cboEditPB.Items.Count > 0) cboEditPB.SelectedIndex = 0;
            if (gridNhanVien != null) gridNhanVien.ClearSelection();
            _suspendEmployeeSelection = false;
        }

        private void AddDepartment()
        {
            string name;
            string description;
            if (!ShowDepartmentDialog(out name, out description)) return;
            try
            {
                if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên phòng ban không được để trống.");
                _phongBanDAL.Insert(name.Trim(), description.Trim());
                LoadDepartments();
                MessageBox.Show("Đã thêm phòng ban.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private static bool ShowDepartmentDialog(out string name, out string description)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "Thêm phòng ban";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(380, 170);
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                Label nameLabel = new Label { Text = "Tên phòng ban", Left = 14, Top = 17, Width = 100 };
                TextBox nameBox = new TextBox { Left = 120, Top = 13, Width = 235 };
                Label descLabel = new Label { Text = "Mô tả", Left = 14, Top = 55, Width = 100 };
                TextBox descBox = new TextBox { Left = 120, Top = 51, Width = 235 };
                Button ok = new Button { Text = "Lưu", Left = 190, Top = 105, Width = 75, DialogResult = DialogResult.OK };
                Button cancel = new Button { Text = "Hủy", Left = 280, Top = 105, Width = 75, DialogResult = DialogResult.Cancel };
                dialog.Controls.AddRange(new Control[] { nameLabel, nameBox, descLabel, descBox, ok, cancel });
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                bool result = dialog.ShowDialog() == DialogResult.OK;
                name = nameBox.Text;
                description = descBox.Text;
                return result;
            }
        }

        private void LoadAttendance()
        {
            try
            {
                string month = dtpChamCong.Value.ToString("MM/yyyy", CultureInfo.InvariantCulture);
                _attendanceTable = _chamCongBLL.LoadMonth(month);
                gridChamCong.DataSource = _attendanceTable;
                ConfigureAttendanceGrid();
                bool closed = _chamCongBLL.IsMonthClosed(month);
                gridChamCong.ReadOnly = closed;
                btnSaveChamCong.Enabled = !closed;
                lblChamCongStatus.Text = closed ? "THÁNG ĐÃ CHỐT - ĐÃ KHÓA" : "Chưa chốt - có thể chỉnh sửa";
                lblChamCongStatus.ForeColor = closed ? Color.DarkRed : Color.DarkGreen;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void ConfigureAttendanceGrid()
        {
            if (gridChamCong.Columns.Count == 0) return;
            SetColumn(gridChamCong, "MaCong", "Mã công", 70);
            SetColumn(gridChamCong, "MaNV", "Mã NV", 70);
            SetColumn(gridChamCong, "HoTen", "Họ tên", 220);
            SetColumn(gridChamCong, "TenPB", "Phòng ban", 180);
            SetColumn(gridChamCong, "SoNgayCong", "Số ngày công", 125);
            SetColumn(gridChamCong, "SoGioOT", "Số giờ OT", 115);
            if (gridChamCong.Columns.Contains("ThangNam")) gridChamCong.Columns["ThangNam"].Visible = false;
            if (gridChamCong.Columns.Contains("DaChot")) gridChamCong.Columns["DaChot"].Visible = false;
            gridChamCong.Columns["MaCong"].Visible = false;
            gridChamCong.Columns["HoTen"].ReadOnly = true;
            gridChamCong.Columns["TenPB"].ReadOnly = true;
            gridChamCong.Columns["MaNV"].ReadOnly = true;
            gridChamCong.Columns["SoNgayCong"].DefaultCellStyle.Format = "0.##";
            gridChamCong.Columns["SoGioOT"].DefaultCellStyle.Format = "0.##";
        }

        private void SaveAttendance()
        {
            try
            {
                gridChamCong.EndEdit();
                BindingContext[gridChamCong.DataSource].EndCurrentEdit();
                string month = dtpChamCong.Value.ToString("MM/yyyy", CultureInfo.InvariantCulture);
                int count = _chamCongBLL.SaveMonth(_attendanceTable, month);
                LoadAttendance();
                MessageBox.Show("Đã đồng bộ " + count + " dòng chấm công về SQL Server.",
                    "Lưu chấm công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void Attendance_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            string column = gridChamCong.Columns[e.ColumnIndex].Name;
            if (column != "SoNgayCong" && column != "SoGioOT") return;
            double value;
            if (!double.TryParse(Convert.ToString(e.FormattedValue), NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.CurrentCulture, out value) || value < 0 || (column == "SoNgayCong" && value > 31))
            {
                e.Cancel = true;
                MessageBox.Show(column == "SoNgayCong"
                    ? "Số ngày công phải từ 0 đến 31."
                    : "Số giờ OT phải là số không âm.", "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadPayroll()
        {
            try
            {
                string month = dtpBangLuong.Value.ToString("MM/yyyy", CultureInfo.InvariantCulture);
                DataTable table = _bangLuongBLL.GetMonth(month);
                gridBangLuong.DataSource = table;
                ConfigurePayrollGrid();
                bool closed = table.Rows.Count > 0 && Convert.ToString(table.Rows[0]["TrangThai"]) == "Đã chốt";
                lblBangLuongStatus.Text = closed ? "ĐÃ CHỐT LƯƠNG" : "Bảng xem trước - chưa chốt";
                lblBangLuongStatus.ForeColor = closed ? Color.DarkGreen : Color.DarkOrange;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void ConfigurePayrollGrid()
        {
            if (gridBangLuong.Columns.Count == 0) return;
            SetColumn(gridBangLuong, "MaNV", "Mã NV", 65);
            SetColumn(gridBangLuong, "HoTen", "Họ tên", 170);
            SetColumn(gridBangLuong, "TenPB", "Phòng ban", 145);
            SetColumn(gridBangLuong, "ThangNam", "Tháng", 85);
            SetColumn(gridBangLuong, "LuongCoBan", "Lương cơ bản", 115);
            SetColumn(gridBangLuong, "HeSoLuong", "Hệ số", 75);
            SetColumn(gridBangLuong, "SoNgayCong", "Ngày công", 85);
            SetColumn(gridBangLuong, "SoGioOT", "Giờ OT", 75);
            SetColumn(gridBangLuong, "ThucLinh", "Thực lĩnh", 125);
            SetColumn(gridBangLuong, "TrangThai", "Trạng thái", 105);
            gridBangLuong.Columns["LuongCoBan"].DefaultCellStyle.Format = "N0";
            gridBangLuong.Columns["ThucLinh"].DefaultCellStyle.Format = "N0";
            gridBangLuong.Columns["HeSoLuong"].DefaultCellStyle.Format = "0.##";
            gridBangLuong.Columns["SoNgayCong"].DefaultCellStyle.Format = "0.##";
            gridBangLuong.Columns["SoGioOT"].DefaultCellStyle.Format = "0.##";
        }

        private void ClosePayroll()
        {
            string month = dtpBangLuong.Value.ToString("MM/yyyy", CultureInfo.InvariantCulture);
            DialogResult confirm = MessageBox.Show(
                "Chốt lương tháng " + month + "? Sau khi chốt, dữ liệu chấm công của tháng này sẽ bị khóa và không thể sửa.",
                "Xác nhận chốt lương", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                int count = _bangLuongBLL.CloseMonth(month);
                LoadPayroll();
                if (dtpChamCong.Value.ToString("MM/yyyy", CultureInfo.InvariantCulture) == month)
                    LoadAttendance();
                MessageBox.Show("Đã chốt lương cho " + count + " nhân viên. Dữ liệu chấm công đã được khóa.",
                    "Chốt lương thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private static DataGridView CreateGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = true,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(224, 235, 246),
                    ForeColor = Color.FromArgb(35, 55, 75),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(247, 250, 253) }
            };
        }

        private static DateTimePicker CreateMonthPicker()
        {
            return new DateTimePicker
            {
                Width = 112,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MM/yyyy",
                ShowUpDown = true,
                Value = DateTime.Today
            };
        }

        private static Label MakeLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Padding = new Padding(2, 7, 2, 0) };
        }

        private static Button MakeButton(string text, Action action)
        {
            Button button = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 242, 249),
                Margin = new Padding(4, 1, 4, 1),
                Padding = new Padding(8, 2, 8, 2)
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(180, 198, 215);
            button.Click += delegate { action(); };
            return button;
        }

        private static Control Field(string label, Control input)
        {
            FlowLayoutPanel field = new FlowLayoutPanel
            {
                Width = Math.Max(input.Width + 8, 160),
                Height = 62,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(4, 0, 10, 0)
            };
            field.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Color.FromArgb(65, 75, 85) });
            field.Controls.Add(input);
            return field;
        }

        private static void SetColumn(DataGridView grid, string name, string header, int minimumWidth)
        {
            if (!grid.Columns.Contains(name)) return;
            grid.Columns[name].HeaderText = header;
            grid.Columns[name].MinimumWidth = Math.Min(minimumWidth, 60);
            grid.Columns[name].FillWeight = minimumWidth;
        }

        private static void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            MessageBox.Show("Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.", "Lỗi dữ liệu",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static void ShowError(Exception ex)
        {
            string message = ex.Message;
            if (ex is System.Data.SqlClient.SqlException)
            {
                System.Data.SqlClient.SqlException sqlException = (System.Data.SqlClient.SqlException)ex;
                if (sqlException.Number == 2627 || sqlException.Number == 2601)
                    message = "CCCD hoặc dữ liệu tháng đã tồn tại. " + sqlException.Message;
                else if (sqlException.Number == 547)
                    message = "Không thể xóa/thay đổi dữ liệu vì đang được bảng chấm công hoặc bảng lương tham chiếu. " + sqlException.Message;
            }
            MessageBox.Show(message, "Có lỗi xảy ra", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private sealed class DepartmentOption
        {
            public int? Id { get; private set; }
            public string Name { get; private set; }
            public DepartmentOption(int? id, string name) { Id = id; Name = name; }
        }
    }
}
