using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using WorkShop;

namespace Adamma
{
    public partial class GoToBug : Form
    {
        public GoToBug()
        {
            InitializeComponent();
        }

        private void GoToBug_Load(object sender, EventArgs e)
        {
            comboBoxProduct.Items.Add(QueryProduct.DAXSE);
            comboBoxProduct.Items.Add(QueryProduct.AXSE);
            comboBoxProduct.Items.Add(QueryProduct.AX6);
            comboBoxProduct.Text = GlobalValue.historyQueryProduct.ToString();

            if (GlobalValue.curSelectedTFSID != "" && GlobalValue.historyQueryProduct == QueryProduct.DAXSE)
            {
                textBoxBugID.Text = GlobalValue.curSelectedTFSID;
            }
            else if (GlobalValue.curSelectedPSID != "" && (GlobalValue.historyQueryProduct == QueryProduct.AXSE || GlobalValue.historyQueryProduct == QueryProduct.AX6)) 
            {
                textBoxBugID.Text = GlobalValue.curSelectedPSID;
            }

            textBoxBugID.Focus();
            GlobalValue.curSelectedTFSID = "";
            GlobalValue.curSelectedPSID = "";
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.NavigateBug();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        ErrorProvider errMain = new ErrorProvider();
        Boolean validId = true;
        private void textBoxBugID_Leave(object sender, EventArgs e)
        {
            if (!IsDigitsOnly(textBoxBugID.Text.Trim()))
            {
                errMain.SetError(sender as TextBox, "Invalid Bug ID");
                validId = false;
            }
            else
            {
                errMain.Clear();
                validId = true;
            }
        }

        private bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Enter))
            {
                this.NavigateBug();
                return true;
            }
            else if (keyData == Keys.Escape)
            {
                this.Close();
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void NavigateBug()
        {
            if (!validId || !IsDigitsOnly(textBoxBugID.Text.Trim()))
                return;

            string Id = textBoxBugID.Text.Trim().ToString();

            if (comboBoxProduct.Text == QueryProduct.AXSE.ToString())
            {                    
                BugsNavigator.NavigateToPSBug(Id, false);
            }
            else if (comboBoxProduct.Text == QueryProduct.AX6.ToString())
            {
                BugsNavigator.NavigateToPSBug(Id, true);
            }
            else
            {
                BugsNavigator.NavigateToTFSBug(Id);
            }

            this.Close();
        }

        /// <summary>
        /// Save the user setup value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoToBug_FormClosing(object sender, FormClosingEventArgs e)
        {
            GlobalValue.historyQueryProduct = CommonUtilities.ParseNum<QueryProduct>(comboBoxProduct.Text.ToString());

            if (GlobalValue.historyQueryProduct == QueryProduct.DAXSE)
            {
                GlobalValue.curSelectedTFSID = textBoxBugID.Text.Trim();
            }
            else
            {
                GlobalValue.curSelectedPSID = textBoxBugID.Text.Trim();
            }
        }

    }
}
