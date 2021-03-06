using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CEFA.Utils;
using CEFA.Model;
using CEFA.Business;
using Contract;
using System.Web.Security;
namespace CEFA
{
    public partial class frmLogin : Form
    {
        // Declare
        public string pUsername;
        public bool status = false;
        public bool LoginStatus = false;
        
        int Count = 0;
        // Load Form.
        public frmLogin()
        {
            InitializeComponent(); LoadProperty();
            Global.IsCheckLock = false;
        }

        private void frmLogin_ResizeEnd(object sender, EventArgs e)
        {
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            //pnl.Top = (this.Height - pnl.Height) / 2;
            //pnl.Left = (this.Width - pnl.Width) / 2;
            LoadLogo();
            if (Global.LockUserID==0)
            {
                lblUser.Text = "";
                txtUserName.Select(); txtUserName.Focus();
            }
            else
            {
                lblUser.Text = "Locked by: " + Global.FullName;
                txtUserName.Text = Global.LockUserName;
                txtPassword.Select(); txtPassword.Focus();
            }

        }
        // Button
        private void btnOK_Click(object sender, EventArgs e)
        {
            Process();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
            Application.Exit();
        }
        // Event
        private void frmLogin_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (status == false)
                Application.Exit();
        }
        //  UF
        private void Process()
        {
            {
                try
                {
                    Count++;
                    int LogStatus=UsersBO.ValidateUser(txtUserName.Text, SecUtils.GetPassword(txtPassword.Text));
                    if (LogStatus == -1)
                    {
                        SecUtils.InsertLogFail(txtUserName.Text);
                        if (SecUtils.CheckCountLogFail(this, txtUserName.Text) == false)
                        {
                            btnExit_Click(null, null);
                        }
                        MessageBox.Show("Sai Username hoặc Password !", TextUtils.Caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPassword.Focus();
                        return;
                    }
                    else if (LogStatus == 0)
                    {
                        MessageBox.Show("User đang bị khóa !", TextUtils.Caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    else
                    {
                        if (SecUtils.CheckCountLogFail(this, txtUserName.Text) == false)
                        {
                            btnExit_Click(null, null);
                        }
                        if (Global.LockUserID==0)
                        {
                            UsersModel mU = (UsersModel)UsersBO.Instance.FindByAttribute("LoginName", txtUserName.Text)[0];

                            Global.UserID = mU.ID;
                            Global.UserGroupID = mU.UserGroupID;
                            Global.UserName = txtUserName.Text.Trim();
                            Global.IsFullView = mU.IsFullView;

                            Global.FullName = mU.FirstName + " " + mU.MiddleName + " " + mU.LastName;
                            Global.ShiftID = 0;
                            Global.CashierNo = "";

                            status = true; LoginStatus = true;

                            // Gán thông tin lúc đăng nhập
                            Global.LockUserID = 0;
                            Global.LockUserName = "";
                            Global.DoTime = DateTime.Now;
                            Global.IsCheckLock = true;
                            this.Close();  
                        }
                        else
                        {
                            UsersModel mU = (UsersModel)UsersBO.Instance.FindByAttribute("LoginName", txtUserName.Text)[0];
                            if (mU.ID==Global.LockUserID)
                            {
                                Global.UserID = mU.ID;
                                Global.UserGroupID = mU.UserGroupID;
                                Global.UserName = txtUserName.Text.Trim();
                                Global.IsFullView = mU.IsFullView;

                                Global.FullName = mU.FirstName + " " + mU.MiddleName + " " + mU.LastName;
                                Global.ShiftID = 0;
                                Global.CashierNo = "";

                                status = true; LoginStatus = true;
                                Global.LockUserID = 0;
                                Global.LockUserName = "";
                                // Gán thông tin lúc đăng nhập
                                Global.LockUserID = 0;
                                Global.LockUserName = "";
                                Global.DoTime = DateTime.Now;
                                Global.IsCheckLock = true;
                                this.Close();
                            }  
                            else
                            {
                                MessageBox.Show(string.Format("Chương trình đang được sử dụng bởi [{0}] !",Global.LockUserName), "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private int Find(string[] Input, string element)
        {
            if ((Input == null) || (Input.Length == 0))
            {
                return -1;
            }
            else
            {
                for (int i = 0; i < Input.Length; i++)
                {
                    if (Input[i].ToUpper().Trim() == element.ToUpper().Trim())
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        private void LoadProperty()
        {
            try
            {
                DataTable dt = TextUtils.Select("Select top 1 ID,Name from Project");
                Miscellaneous.PopulateCombo(cboProperty,dt,"Name","ID");
                if (cboProperty.Items.Count>0)
                {
                    cboProperty.SelectedIndex = 0;
                    Global.ProjectID = dt.Rows[0]["ID"].ToString();
                    Global.ProjectName = dt.Rows[0]["Name"].ToString();
                }
            }
            catch (System.Exception ex)
            {
            	MessageBox.Show(ex.Message, "Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }
        }

        private void txtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
                Process();
        }

        private void LoadLogo()
        {
            try
            {
                DataTable dt = TextUtils.Select("select KeyValue from ConfigSystem where KeyName='Logo'");
                if (dt.Rows.Count>0)
                {
                    string filename = Application.StartupPath + "\\_Logo\\" + dt.Rows[0][0].ToString();
                    if (System.IO.File.Exists(filename))
                    {
                        pictureBox1.BackgroundImage=Image.FromFile(filename);
                        pictureBox1.BackgroundImageLayout=ImageLayout.Stretch;
                    }
                }
            }
            catch (System.Exception ex)
            {
                return;
            }
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            btnExit_Click(null, null);
        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }
}