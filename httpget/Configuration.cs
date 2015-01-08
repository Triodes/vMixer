using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace httpget
{
    public partial class Configuration : Form
    {
        public Configuration()
        {
            InitializeComponent();

            this.AcceptButton = save;

            LoadData();
        }

        void LoadData()
        {
            dataGridView1.BackgroundColor = Color.White;
            this.dataGridView1.Columns.Add("Key", "Key");
            this.dataGridView1.Columns.Add("Value", "Value");
            dataGridView1.Columns[0].ReadOnly = true;
            dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (SettingsProperty e in Settings.Default.Properties)
            {
                this.dataGridView1.Rows.Add(e.Name, Settings.Default[e.Name]);
            }
            dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Ascending);
        }

        private void save_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow item in dataGridView1.Rows)
            {
                if (item.Cells[0].Value != null)
                {
                    string key = item.Cells[0].Value.ToString();
                    string value = item.Cells[1].Value.ToString();
                    Settings.Default[key] = value;
                }
            }
            Settings.Default.Save();
            this.Close();
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void defaults_Click(object sender, EventArgs e)
        {
            dataGridView1.Columns.Clear();
            Settings.Default.Reset();
            LoadData();
        }
    }
}
