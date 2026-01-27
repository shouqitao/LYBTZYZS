using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Reader_Sample
{
    public partial class CPU : Form
    {
        public CPU()
        {
            InitializeComponent();
        }

        private int Rhandle = -99;
        int nRt;
        byte slot;
        StringBuilder dev_name = new StringBuilder("USB1");


        private void button1_Click(object sender, EventArgs e)
        {
            byte[] ATR = new byte[50];
            StringBuilder sbShow = new StringBuilder(100);

            slot = (byte)this.SlotBox.SelectedIndex;
            if ( slot == 0)
            {
                slot = 0x01; //大卡座
            }
            else if ( slot == 1 )
            {
                slot = 0x02; //副卡座
            }
            else if ( slot > 1 && slot < 6)
            {
                slot = (byte)(0x0f + slot); //SAM卡座
            }
            else if ( slot == 6 )
            {
                slot = 6;
            }
            else
            {
                slot = 0x01;
            }


            Rhandle = dev.ICC_Reader_Open(dev_name);
            if ( Rhandle <= 0 )
            {
                this.status.Text = "读卡器连接失败";
                return;
            }


            if ( slot != 6 )
            {
                nRt = dev.ICC_Reader_pre_PowerOn(Rhandle, slot, ATR);
                if (nRt <= 0)
                {
                    this.status.Text = "接触上电失败";
                    return;
                }
            }
            else
            {
                //非接基本函数
                nRt = dev.PICC_Reader_SetTypeA(Rhandle);
                if (nRt != 0)
                {
                    this.status.Text = "设置为A卡模式失败";
                    return;
                }
                nRt = dev.PICC_Reader_Request(Rhandle);
                if (nRt != 0)
                {
                    this.status.Text = "请求卡片失败";
                    return;
                }
                byte[] UID = new byte[10];
                nRt = dev.PICC_Reader_anticoll(Rhandle, UID);
                if (nRt != 0)
                {
                    this.status.Text = "防碰撞失败";
                    return;
                }
                nRt = dev.PICC_Reader_Select(Rhandle, 0x41);
                if (nRt != 0)
                {
                    this.status.Text = "选卡失败";
                    return;
                }

                //nRt = dev.PICC_Reader_PowerOnTypeA(Rhandle, ATR);
                //if ( nRt <= 0 )
                //{
                //    this.status.Text = "非接上电失败";
                //    return;
                //}
            }
            

            dev.HexToStr(ATR, nRt, sbShow);
            this.showBox.Text += "复位信息：";
            this.showBox.Text += sbShow.ToString();
            this.showBox.Text += "\r\n";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            byte[] resp = new byte[255];
            byte[] cmd  = new byte[255];

            StringBuilder sbShow = new StringBuilder(512);
            StringBuilder sbCmd = new StringBuilder(this.apduBox.Text.ToString());
            int cmdlen = this.apduBox.Text.ToString().Length / 2;

            dev.StrToHex(sbCmd, cmdlen*2, cmd);
            if ( slot != 6 )
            {
                nRt = dev.ICC_Reader_Application(Rhandle, slot, cmdlen, cmd, resp);
                if (nRt < 2)
                {
                    this.status.Text = "APDU命令执行失败";
                    return;
                }
            }
            else
            {
                nRt = dev.PICC_Reader_Application(Rhandle, cmdlen, cmd, resp);
                if (nRt < 2)
                {
                    this.status.Text = "APDU命令执行失败";
                    return;
                }
            }

            dev.HexToStr(resp, nRt, sbShow);
            this.showBox.Text += "响应：";
            this.showBox.Text += sbShow.ToString();
            this.showBox.Text += "\r\n";
            this.showBox.SelectionStart = this.showBox.Text.Length; //设定光标位置
            this.showBox.ScrollToCaret(); //滚动到光标处
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // private int Rhandle = -99;
            byte[] ATR = new byte[50];
            StringBuilder sbShow = new StringBuilder(100);

            nRt = dev.PICC_Reader_PowerOnTypeA(Rhandle, ATR);
            if (nRt <= 0)
            {
                this.status.Text = "非接上电失败";
                return;
            }
       


        dev.HexToStr(ATR, nRt, sbShow);
            this.showBox.Text += "复位信息：";
            this.showBox.Text += sbShow.ToString();
            this.showBox.Text += "\r\n";
        }
    }
}
