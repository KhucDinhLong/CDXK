using BMS.Business;
using BMS.Model;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using BMS.Utils;
using System.IO;
using Vila.Extensions;
using JR.Utils.GUI.Forms;
using Forms;

namespace BMS
{
    public partial class frmProductCheckHistory1 : _Forms
    {
        public long _WorkerID = 0;
        string _order = "";
        string _productCode = "";
        string _tienTo = "";
        string _stt = "";
        Color _colorEmpty;
        string _GroupCode;

        int oldHeight = 0;
        int oldHeightGrid = 0;
        int _productID = 0;
        ASCIIEncoding _encoding = new ASCIIEncoding();

        string _step;

        DataTable _dtData = new DataTable();

        ArrayList lstWorkingName;

        string OrderInfoFolder = string.Empty;

        public frmProductCheckHistory1()
        {
            InitializeComponent();
        }
        private void frmProductCheckHistory1_Load(object sender, EventArgs e)
        {
            ToolTip toolTip1 = new ToolTip();
            toolTip1.ShowAlways = true;
            toolTip1.SetToolTip(this.btnSave, "F12");

            _colorEmpty = Color.FromArgb(255, 192, 255);
            initBackColor();
            oldHeight = this.Height;
            oldHeightGrid = grdData.Height;

        }

        private void LoadListWorkingName()
        {
            lstWorkingName = new ArrayList();
            for (int i = 0; i < grvData.RowCount; i++)
            {
                string[] workingName = grvData.GetRowCellValue(i, colStandardValue).ToString().Split(',');
                lstWorkingName.Add(workingName[0]);
            }
        }

        /// <summary>
        /// Set focus vào cell trên grid
        /// </summary>
        /// <param name="indexRow"></param>
        /// <param name="indexColum"></param>
        /// 
        void resetControl()
        {
            ///*
            // * reset lại tiêu đề cột, các kết quả check
            // */
            for (int i = 3; i < 9; i++)
            {
                grvData.Columns["RealValue" + (i - 2)].Caption = "#";
            }
        }

        void initBackColor()
        {
            if (cboWorkingStep.SelectedIndex > 0)
            {
                cboWorkingStep.BackColor = Color.White;
            }
            else
            {
                cboWorkingStep.BackColor = _colorEmpty;
            }

            if (string.IsNullOrWhiteSpace(txtQRCode.Text))
            {
                txtQRCode.BackColor = _colorEmpty;
            }
            else
            {
                txtQRCode.BackColor = Color.White;
            }

            if (string.IsNullOrWhiteSpace(txtWorker.Text))
            {
                txtWorker.BackColor = _colorEmpty;
            }
            else
            {
                txtWorker.BackColor = Color.White;
            }

            if (string.IsNullOrWhiteSpace(txtOrder.Text))
            {
                txtOrder.BackColor = _colorEmpty;
            }
            else
            {
                txtOrder.BackColor = Color.White;
            }
        }

        void loadComboStep(string productCode)
        {
            DataTable dt = TextUtils.LoadDataFromSP("spGetProductStep_ByProductCode", "A"
                , new string[1] { "@ProductCode" }
                , new object[1] { productCode });
            DataRow dr = dt.NewRow();
            dr["ID"] = 0;
            dr["ProductStepCode"] = "";
            dt.Rows.InsertAt(dr, 0);

            cboWorkingStep.DataSource = dt;
            cboWorkingStep.DisplayMember = "ProductStepCode";
            cboWorkingStep.ValueMember = "ID";
            if(dt.Rows.Count > 1)
            {
                for (int i = 1; i < dt.Rows.Count; i++)
                {
                    if (dt.Rows[i]["ProductStepCode"].ToString().Trim().Equals("CDXK"))
                    {
                        cboWorkingStep.SelectedIndex = i;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Load danh sách công đoạn kiểm tra
        /// </summary>
        void loadDataWorkingStep()
        {
            if (!string.IsNullOrWhiteSpace(txtQRCode.Text.Trim()))
            {
                string orderCode = txtQRCode.Text.Trim();
                string[] arr = orderCode.Split(' ');
                if (arr.Length > 1)
                {
                    loadComboStep(arr[1]);
                }
                else
                {
                    cboWorkingStep.DataSource = null;
                }
            }
            else
            {
                cboWorkingStep.DataSource = null;
            }
        }

        int getSumHeightRows()
        {
            int total = 0;
            GridViewInfo vi = grvData.GetViewInfo() as GridViewInfo;
            for (int i = 0; i < grvData.RowCount; i++)
            {
                GridRowInfo ri = vi.RowsInfo.FindRow(i);
                if (ri != null)
                    total += ri.Bounds.Height;
            }

            return total;
        }


        /// <summary>
        /// Set caption cho các cột nhập giá trị kiểm tra
        /// Cái này làm cho dự án thêm hạng mục kiểm tra order sản phẩm
        /// </summary>
        void setCaptionGridColumn()
        {
            for (int i = 0; i < 6; i++)
            {
                grvData.Columns["RealValue" + (i + 1)].Caption = (int.Parse(_stt) + i).ToString();
            }
        }

        bool _isStartColor = false;
        private void cboWorkingStep_SelectedIndexChanged(object sender, EventArgs e)
        {
            _step = cboWorkingStep.Text.Trim();

            _isStartColor = false;
            int workingStepID = TextUtils.ToInt(cboWorkingStep.SelectedValue);
            string qrCode = txtQRCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(qrCode)) return;

            if (cboWorkingStep.SelectedIndex > 0)
            {
                cboWorkingStep.BackColor = Color.White;
            }
            else
            {
                cboWorkingStep.BackColor = _colorEmpty;
            }

            resetControl();

            if (workingStepID == 0)
            {
                _dtData = null;
                grdData.DataSource = null;
                txtStepName.Text = "";
                return;
            }

            //Sinh ra file lưu tên công đoạn
            File.WriteAllText(Application.StartupPath + "\\CD.txt", cboWorkingStep.Text.Trim());

            /*
             * Tách chuỗi QrCode
             */
            string orderCode = txtQRCode.Text.Trim();
            string[] arr1 = orderCode.Split(' ');
            if (arr1.Length > 1)
            {
                _order = arr1[0];
                _productCode = arr1[1].Trim();
                string[] arr;
                if (_order.Contains("-"))
                {
                    arr = _order.Split('-');
                    _tienTo = (arr[0] + "-" + arr[1] + "-");
                    _stt = arr[2];
                }
                else
                {
                    arr = Regex.Split(_order, @"\D+");
                    _stt = arr[arr.Length - 1];
                    _tienTo = _order.Substring(0, _order.IndexOf(_stt));
                }
            }

            string stepCode = cboWorkingStep.Text.Trim();

            //Gán dữ liệu vào grid
            DataSet ds = ProductCheckHistoryDetailBO.Instance.GetDataSet("spGetWorkingByProduct",
                new string[] { "@WorkingStepID", "@WorkingStepCode", "@ProductCode" },
                new object[] { workingStepID, stepCode, arr1[1].ToString() });
            int x = 0;
            _dtData = ds.Tables[0];
            grdData.DataSource = _dtData;
            txtStepName.Text = TextUtils.ToString(ds.Tables[1].Rows.Count > 0 ? ds.Tables[1].Rows[0][0] : "");

            _GroupCode = TextUtils.ToString(ds.Tables[2].Rows.Count > 0 ? ds.Tables[2].Rows[0]["ProductGroupCode"] : "");
            txtName.Text = TextUtils.ToString(ds.Tables[2].Rows.Count > 0 ? ds.Tables[2].Rows[0]["ProductName"] : "");
            txtMo.Text = TextUtils.ToString(ds.Tables[2].Rows.Count > 0 ? ds.Tables[2].Rows[0]["LoaiMo"] : "");
            txtGoal.Text = TextUtils.ToString(ds.Tables[2].Rows.Count > 0 ? ds.Tables[2].Rows[0]["Goal"] : "");

            string gunNumber = TextUtils.ToString(ds.Tables[2].Rows.Count > 0 ? ds.Tables[2].Rows[0]["GunNumber"] : "");
            string jobNumber = TextUtils.ToString(ds.Tables[2].Rows.Count > 0 ? ds.Tables[2].Rows[0]["JobNumber"] : "");
            string qtyOcBanGa = TextUtils.ToString(ds.Tables[2].Rows.Count > 0 ? ds.Tables[2].Rows[0]["QtyOcBanGa"] : "");
            string qtyOcBanThat = TextUtils.ToString(ds.Tables[2].Rows.Count > 0 ? ds.Tables[2].Rows[0]["QtyOcBanThat"] : "");
            _productID = TextUtils.ToInt(ds.Tables[2].Rows.Count > 0 ? ds.Tables[2].Rows[0]["ID"] : "");

            string file = string.Format("{0};{1};{2};{3}", gunNumber, jobNumber, qtyOcBanGa, qtyOcBanThat);
            try
            {
                System.IO.File.WriteAllText(Application.StartupPath + "\\SettingsTouque.txt", file);
            }
            catch
            {
            }

            // Set lại chiều cao của dòng
            if (grvData.RowCount > 0)
            {
                grvData.RowHeight = -1;
                int totalHeightRow = this.getSumHeightRows();
                if ((oldHeightGrid - grvData.ColumnPanelRowHeight - 30) > totalHeightRow)
                {
                    grvData.RowHeight = (oldHeightGrid - grvData.ColumnPanelRowHeight - 30) / grvData.RowCount;
                }
            }

            _isStartColor = true;

            setCaptionGridColumn();

            LoadListWorkingName();

            txtData.Focus();

        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!PreSaveCheck())
            {
                MessageBox.Show("Số lượng sản phẩm không giống nhau!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtData.Text = string.Empty;
                txtData.Focus();
                return;
            }
            if (grvData.RowCount == 0)
            {
                return;
            }

            int productID = _productID;
            int stepID = TextUtils.ToInt(cboWorkingStep.SelectedValue);
            if (productID == 0)
            {
                MessageBox.Show(string.Format("Không tồn tại sản phẩm có mã [{0}]!", _productCode.Trim()), "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (stepID == 0)
            {
                MessageBox.Show(string.Format("Bạn chưa chọn công đoạn nào!", _productCode.Trim()), "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(txtWorker.Text.Trim()))
            {
                MessageBox.Show(string.Format("Bạn chưa điền người làm!", _productCode.Trim()), "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtWorker.Focus();
                return;
            }

            DialogResult result = MessageBox.Show("Bạn có chắc muốn cất dữ liệu?", "Cất?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No) return;

            if (OrderInfoFolder == string.Empty || !Directory.Exists(OrderInfoFolder))
            {
                FolderBrowserDialog od = new FolderBrowserDialog();
                od.Description = "Chọn thư mục lưu thông tin Order";
                if (od.ShowDialog() == DialogResult.OK)
                {
                    OrderInfoFolder = od.SelectedPath;
                }
                else
                {
                    return;
                }
            }

            int count = _dtData.Rows.Count;
            int countProductInThisOrder = 0;
            for (int i = 3; i < 9; i++)
            {
                string columnCaption = grvData.Columns[i].Caption;
                string qrCode = _tienTo + columnCaption + " " + _productCode; ;

                /*
                 * Xóa các giá trị đã lưu cũ
                 */
                // ProductCheckHistoryDetailBO.Instance.DeleteByExpression(new Utils.Expression("QRCode", qrCode).And(new Utils.Expression("ProductStepID", stepID)));
                /*
                 * Insert lại dữ liệu kiểm tra vào bảng
                 */
                for (int j = 0; j < count; j++)
                {
                    ProductCheckHistoryDetailModel cModel = new ProductCheckHistoryDetailModel();
                    cModel.ProductStepID = stepID;
                    cModel.ProductStepCode = cboWorkingStep.Text.Trim();
                    cModel.ProductStepName = txtStepName.Text.Trim();
                    cModel.SSortOrder = TextUtils.ToInt(grvData.GetRowCellValue(j, colSSortOrder));

                    cModel.ProductWorkingID = TextUtils.ToInt(grvData.GetRowCellValue(j, colWorkingID));
                    cModel.ProductWorkingName = TextUtils.ToString(grvData.GetRowCellValue(j, colProductWorkingName));
                    cModel.WSortOrder = TextUtils.ToInt(grvData.GetRowCellValue(j, colSortOrder));

                    cModel.WorkerCode = txtWorker.Text.Trim();
                    cModel.StandardValue = TextUtils.ToString(grvData.GetRowCellValue(j, colStandardValue));
                    cModel.RealValue = TextUtils.ToString(grvData.GetRowCellValue(j, "RealValue" + (i - 2)));
                    if (j == 0 && !string.IsNullOrEmpty(cModel.RealValue.Trim()))
                    {
                        countProductInThisOrder++;
                    }
                    cModel.ValueType = TextUtils.ToInt(grvData.GetRowCellValue(j, colValueType));
                    cModel.ValueTypeName = cModel.ValueType == 1 ? "Giá trị\n数値" : "Check mark";
                    cModel.EditValue1 = "";
                    cModel.EditValue2 = "";
                    cModel.StatusResult = 1;
                    cModel.ProductID = productID;
                    cModel.QRCode = qrCode;
                    cModel.OrderCode = string.IsNullOrWhiteSpace(txtOrder.Text.Trim()) ?
                        (_tienTo.Contains("-") ? _tienTo.Substring(0, _tienTo.Length - 1) : _order) :
                        txtOrder.Text.Trim();
                    cModel.PackageNumber = _tienTo.Contains("-") ? _tienTo.Split('-')[1] : "";
                    cModel.QtyInPackage = columnCaption;
                    cModel.Approved = "";
                    cModel.Monitor = "";
                    cModel.DateLR = DateTime.Now;
                    cModel.EditContent = "";
                    cModel.EditDate = DateTime.Now;
                    cModel.ProductCode = _productCode;

                    cModel.ProductOrder = _order;

                    ProductCheckHistoryDetailBO.Instance.Insert(cModel);
                }
            }

            string OderInfoFilePath = OrderInfoFolder + "\\" + "CDXK-Order.txt";
            File.WriteAllText(OderInfoFilePath, txtOrder.Text + "-" + countProductInThisOrder);

            grdData.DataSource = null;
            txtQRCode.Text = "";
            txtOrder.Text = "";
            cboWorkingStep.DataSource = null;
            lstWorkingName.Clear();
            resetControl();
            txtQRCode.Focus();
        }

        private bool PreSaveCheck()
        {
            int FirstNumOfColHasVal = 0;
            for (int i = 0; i < grvData.RowCount; i++)
            {
                int NumOfColHasVal = 0;
                for (int j = 0; j < grvData.VisibleColumns.Count; j++)
                {
                    if (!string.IsNullOrEmpty(grvData.GetRowCellValue(i, grvData.VisibleColumns[j].FieldName).ToString().Trim())) NumOfColHasVal++;
                }
                if (NumOfColHasVal != 0)
                {
                    if (FirstNumOfColHasVal == 0)
                    {
                        FirstNumOfColHasVal = NumOfColHasVal;
                    }
                    else
                    {
                        if (NumOfColHasVal != FirstNumOfColHasVal) return false;
                    }
                }
            }
            return true;
        }

        private void txtQRCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtOrder.Focus();
            }
        }
        private void txtOrder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                cboWorkingStep.Focus();
                loadDataWorkingStep();
            }
        }
        private void frmProductCheckHistory_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void grvData_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (!_isStartColor) return;
            if (e.RowHandle < 0) return;

            if (e.Column.VisibleIndex < 2) return;

            string value = TextUtils.ToString(grvData.GetRowCellValue(e.RowHandle, e.Column));
            string standardValue = TextUtils.ToString(grvData.GetRowCellValue(e.RowHandle, colStandardValue)).Split(',')[0];

            if (!string.IsNullOrWhiteSpace(value.Trim()) && !string.IsNullOrWhiteSpace(standardValue.Trim()))
            {
                /*
                 * Kiểm tra ghi chép dạng giá trị, nhưng giá trị tiêu chuẩn dạng text chứ không phải dạng số
                 */
                string[] arr = value.Split(',');
                if (arr.Length > 0)
                {
                    if (arr[0].ToLower() != standardValue.ToLower())
                    {
                        e.Appearance.BackColor = Color.Red;
                    }
                    else
                    {
                        e.Appearance.BackColor = Color.FromArgb(102, 255, 255);
                    }
                }
                else
                {
                    e.Appearance.BackColor = _colorEmpty;
                }
                return;
            }
            if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(standardValue.Trim()))
            {
                e.Appearance.BackColor = _colorEmpty;
            }
            else
            {
                e.Appearance.BackColor = Color.White;
            }



            //102, 255, 255
        }
        private void txt_TextChanged(object sender, EventArgs e)
        {
            TextBox txt = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(txt.Text.Trim()))
            {
                txt.BackColor = _colorEmpty;
            }
            else
            {
                txt.BackColor = Color.White;
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnSave_Click(null, null);
        }

        private void txtData_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (e.KeyCode == Keys.Enter)
            {
                if (!string.IsNullOrEmpty(txtData.Text) && lstWorkingName != null)
                {
                    string InputWorkingName = txtData.Text.Split(',')[0];
                    for (int i = 0; i < lstWorkingName.Count; i++)
                    {
                        if (lstWorkingName[i].ToString().Trim().ToLower().Equals(InputWorkingName.Trim().ToLower())) 
                        {
                            for (int j = 2; j < grvData.VisibleColumns.Count; j++)
                            {
                                if (txtData.Text.ToLower().Equals(grvData.GetRowCellValue(i, grvData.VisibleColumns[j].FieldName).ToString().Trim().ToLower()))
                                {
                                    txtData.Text = string.Empty;
                                    txtData.Focus();
                                    return;
                                }
                                    
                                if (string.IsNullOrEmpty(grvData.GetRowCellValue(i, grvData.VisibleColumns[j].FieldName).ToString()))
                                {
                                    grvData.SetRowCellValue(i, grvData.VisibleColumns[j].FieldName.ToString(), txtData.Text);
                                    txtData.Text = string.Empty;
                                    txtData.Focus();
                                    return;
                                }
                            }
                        }
                    }
                }
                WarningForm frmWarning = new WarningForm();
                frmWarning.ShowDialog();
                txtData.Text = string.Empty;
                txtData.Focus();
            }
        }

    }
}
