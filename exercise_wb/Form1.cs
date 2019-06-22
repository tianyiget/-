using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace exercise_wb
{
    public partial class Form1 : Form
    {
        public bool isConnect;
        public Product pdt;

        


        public Form1()
        {
            InitializeComponent();
            pdt = new Product();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!isConnect)
            {
                try
                {
                    pdt.Open();
                    button1.Text = "关闭设备";
                    button1.BackColor = Color.Yellow;
                    isConnect = true;
                    listBox1.Items.Clear();
                    listBox1.Items.Add("设备显示");//测试list的显示
                    listBox1.Items.Add("附加");//自动换行
                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}", "Error", MessageBoxButtons.OK);
                }
            }
            else
            {
                DialogResult rst = MessageBox.Show("是否停止", "提示", MessageBoxButtons.OKCancel);
                if (rst != DialogResult.OK)
                    return;

                pdt.Close();
                button1.Text = "启动设备";
                button1.BackColor = Color.Lime;
                isConnect = false;
            }
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            
            led1.Value = pdt.ctr.gear == Gear.P;
            led2.Value = pdt.ctr.gear == Gear.R;
            led3.Value = pdt.ctr.gear == Gear.N;
            led4.Value = pdt.ctr.gear == Gear.D;
            led5.Value = pdt.ctr.gear == Gear.Error;

            while(!pdt.infos.IsEmpty)
            {
                
                string info=pdt.infos.Dequeue();
                listBox1.Items.Add(info);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            }

            
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // pdt.ctr.SendGBC(pdt.ctr.P);
            pdt.ctr.ChangeEMS(Gear.P);
        }

        private void button3_Click(object sender, EventArgs e)
        {
           // pdt.ctr.SendGBC(pdt.ctr.R);
            pdt.ctr.ChangeEMS(Gear.R);
        }

        private void button4_Click(object sender, EventArgs e)
        {
          // pdt.ctr.SendGBC(pdt.ctr.N);
            pdt.ctr.ChangeEMS(Gear.N);
        }

        private void button5_Click(object sender, EventArgs e)
        {
          // pdt.ctr.SendGBC(pdt.ctr.D);
            pdt.ctr.ChangeEMS(Gear.D);
        }
    }
}
