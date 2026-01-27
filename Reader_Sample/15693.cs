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
    public partial class _15693 : Form
    {
        int nRt = -1;
        int Rhandle = -1;
        byte[] resp = new byte[50];
        byte[] data = new byte[50];
        byte[] hexData = new byte[100];
        byte blockAddr = 0x00;
        StringBuilder strOut = new StringBuilder(100);
        StringBuilder dev_name = new StringBuilder("USB1");

        public _15693()
        {
            InitializeComponent();
            Rhandle = dev.ICC_Reader_Open(dev_name);
        }


        private void btnsearch_Click(object sender, EventArgs e)
        {
            if (Rhandle <= 0)
            {
                textBox15693show.Text += "设备未连接\r\n";
                return;
            }
            nRt = dev.PICC_Reader_Inventory(Rhandle, resp);
            if (nRt != 0)
            {
                textBox15693show.Text += "寻卡失败\r\n";
                return;
            }
            //成功  返回8字节数据
            dev.HexToStr(resp, 8, strOut);
            textBox15693show.Text += "读卡成功：" + strOut.ToString() + "\r\n";
        }

        private void btn15693cinfo_Click(object sender, EventArgs e)
        {
            if (Rhandle <= 0)
            {
                textBox15693show.Text += "设备未连接\r\n";
                return;
            }
            nRt = dev.PICC_Reader_SystemInfor(Rhandle, resp);
            if (nRt != 0)
            {
                textBox15693show.Text += "获取卡信息失败\r\n";
                return;
            }
            //成功  返回14字节数据
            dev.HexToStr(resp, 14, strOut);
            textBox15693show.Text += "读卡成功：" + strOut.ToString() + "\r\n";
        }

        private void btn15693read_Click(object sender, EventArgs e)
        {
            if (Rhandle <= 0)
            {
                textBox15693show.Text += "设备未连接\r\n";
                return;
            }

            int blk_addr = int.Parse(textBox_15693addr.Text);
            
            nRt = dev.PICC_Reader_15693_Read(Rhandle, (byte)blk_addr, resp);
            if (nRt != 0)
            {
                textBox15693show.Text += "读卡失败\r\n";
                return;
            }
            //成功  返回4字节数据
            dev.HexToStr(resp, 4, strOut);
            textBox15693show.Text += "读卡成功：" + strOut.ToString() + "\r\n";
        }

        private void btn15693write_Click(object sender, EventArgs e)
        {
            if (Rhandle <= 0)
            {
                textBox15693show.Text += "设备未连接\r\n";
                return;
            }

            int blk_addr = int.Parse(textBox_15693addr.Text);
            StringBuilder strData = new StringBuilder(textBox15693data.Text.ToString());
            dev.StrToHex(strData, 8, hexData);

            nRt = dev.PICC_Reader_15693_Write(Rhandle, (byte)blk_addr, hexData, resp);
            if (nRt != 0)
            {
                textBox15693show.Text += "写卡失败\r\n";
                return;
            }
            textBox15693show.Text += "写卡成功" + "\r\n";
        }

        private void btn15693laddr_Click(object sender, EventArgs e)
        {
            if (Rhandle <= 0)
            {
                textBox15693show.Text += "设备未连接\r\n";
                return;
            }

            int blk_addr = int.Parse(textBox_15693addr.Text);

            nRt = dev.PICC_Reader_LockDataBlock(Rhandle, (byte)blk_addr, resp);
            if (nRt != 0)
            {
                textBox15693show.Text += "锁块失败\r\n";
                return;
            }
            textBox15693show.Text += "锁块成功" + "\r\n";
        }

        private void btn15693wafi_Click(object sender, EventArgs e)
        {
            if (Rhandle <= 0)
            {
                textBox15693show.Text += "设备未连接\r\n";
                return;
            }

            byte[] afi = new byte[2];
            StringBuilder strData = new StringBuilder(textBox15693data.Text.ToString());
            dev.StrToHex(strData, 2, hexData); //AFI只有一字节
            afi[0] = 0x00;
            afi[1] = hexData[0];
            nRt = dev.PICC_Reader_AFI(Rhandle, afi, resp);
            if (nRt != 0)
            {
                textBox15693show.Text += "写AFI失败\r\n";
                return;
            }
            textBox15693show.Text += "写AFI成功" + "\r\n";
        }

        private void btn15693lafi_Click(object sender, EventArgs e)
        {
            if (Rhandle <= 0)
            {
                textBox15693show.Text += "设备未连接\r\n";
                return;
            }

            byte[] afi = new byte[2];
            afi[0] = 0x01; //AFI[0] 为 1 即为锁定
  
            nRt = dev.PICC_Reader_AFI(Rhandle, afi, resp);
            if (nRt != 0)
            {
                textBox15693show.Text += "锁AFI失败\r\n";
                return;
            }
            textBox15693show.Text += "锁AFI成功" + "\r\n";
        }

        private void btn15693wdsfid_Click(object sender, EventArgs e)
        {
            if (Rhandle <= 0)
            {
                textBox15693show.Text += "设备未连接\r\n";
                return;
            }

            byte[] dsfid = new byte[2];
            StringBuilder strData = new StringBuilder(textBox15693data.Text.ToString());
            dev.StrToHex(strData, 2, hexData); //DSFID只有一字节
            dsfid[0] = 0x00;
            dsfid[1] = hexData[0];
            nRt = dev.PICC_Reader_DSFID(Rhandle, dsfid, resp);
            if (nRt != 0)
            {
                textBox15693show.Text += "写DSFID失败\r\n";
                return;
            }
            textBox15693show.Text += "写DSFID成功" + "\r\n";
        }

        private void btn15693ldsfid_Click(object sender, EventArgs e)
        {
            if (Rhandle <= 0)
            {
                textBox15693show.Text += "设备未连接\r\n";
                return;
            }

            byte[] dsfid = new byte[2];
            dsfid[0] = 0x01; //DSFID[0] 为 1 即为锁定

            nRt = dev.PICC_Reader_DSFID(Rhandle, dsfid, resp);
            if (nRt != 0)
            {
                textBox15693show.Text += "锁DSFID失败\r\n";
                return;
            }
            textBox15693show.Text += "锁DSFID成功" + "\r\n";
        }
    }
}
