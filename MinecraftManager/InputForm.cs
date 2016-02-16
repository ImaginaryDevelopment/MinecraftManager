using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MinecraftManager
{
    public partial class InputForm : Form
    {
        public static string ShowDialog(string title, string caption, string currentText = null,Icon icon=null)
        {
            Debug.Assert(caption != null);
            Debug.Assert(title != null);

            using (var iForm = new InputForm())
            {
                iForm.lblCaption.Text = caption;
                iForm.Text = title;
                iForm.Icon = icon;
                iForm.ShowDialog();
                return iForm.textBox1.Text;
            }
        }
        private InputForm()
        {
            InitializeComponent();
        }
    }
}
