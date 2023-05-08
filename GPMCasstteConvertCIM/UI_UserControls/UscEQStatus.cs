﻿using GPMCasstteConvertCIM.CasstteConverter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GPMCasstteConvertCIM.UI_UserControls
{
    public partial class UscEQStatus : UserControl
    {


        public BindingList<clsConverterPort> BindingPorts;
        /// <summary>
        /// 
        /// </summary>
        public UscEQStatus()
        {
            InitializeComponent();
            dataGridView1.CellFormatting += DataGridView1_CellFormatting;

        }

        private void DataGridView1_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex >= 2 && e.RowIndex != -1)
            {
                clsConverterPort data = (clsConverterPort)dataGridView1.Rows[e.RowIndex].DataBoundItem;

                bool state_to_change = false;

                bool load_request = data.LoadRequest;
                bool unload_request = data.UnloadRequest;

                if (e.ColumnIndex == 2)
                    state_to_change = data.LoadRequest;
                if (e.ColumnIndex == 3)
                    state_to_change = data.UnloadRequest;
                if (e.ColumnIndex == 4)
                    state_to_change = data.PortExist;
                if (e.ColumnIndex == 5)
                    state_to_change = data.LD_UP_POS;
                if (e.ColumnIndex == 6)
                    state_to_change = data.LD_DOWN_POS;
                if (e.ColumnIndex == 7)
                    state_to_change = data.PortStatusDown;

                dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = state_to_change ? Color.Lime : Color.White;

            }
        }

        internal void BindData(List<clsConverterPort> allEqPortList)
        {
            BindingPorts = new BindingList<clsConverterPort>(allEqPortList);
            dataGridView1.DataSource = BindingPorts;
        }

        ///
        //uscAlarmTable1.BindData(AlarmManager.AlarmsList);
        //    AlarmManager.onAlarmAdded += (sender, arg) => { uscAlarmTable1.alarmListBinding.ResetBindings(); };
    }
}
