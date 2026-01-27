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
    public partial class m1 : Form
    {
        public m1()
        {
            InitializeComponent();
        }


        private int Rhandle = -99;
        private int ret;
        private StringBuilder devname = new StringBuilder("USB1");
        private StringBuilder Key = new StringBuilder(50);

        private byte[] UID = new byte[5];// 物理卡号，需要时可在此处获得
        private byte secNr;

        private void btn_FindCard_Click(object sender, EventArgs e)
        {
            //M1卡操作示例
            
            
            //连接读卡器
            Rhandle = dev.ICC_Reader_Open(devname);
            if (Rhandle < 0)
            {
                this.status.Text = "读卡器连接失败";
                return;
            }
            //非接基本函数
            ret = dev.PICC_Reader_SetTypeA(Rhandle);
            if (ret != 0)
            {
                this.status.Text = "设置为A卡模式失败";
                return;
            }
            ret = dev.PICC_Reader_Request(Rhandle);
            if (ret != 0)
            {
                this.status.Text = "请求卡片失败";
                return;
            }

            ret = dev.PICC_Reader_anticoll(Rhandle, UID);
            if (ret != 0)
            {
                this.status.Text = "防碰撞失败";
                return;
            }
            ret = dev.PICC_Reader_Select(Rhandle, 0x41);
            if (ret != 0)
            {
                this.status.Text = "选卡失败";
                return;
            }

            this.status.Text = "寻卡成功";
            
        }

        private void btn_VerifyKey_Click(object sender, EventArgs e)
        {
            byte mode = 0x60;//认证KeyA时为0x60，认证KeyB时为0x61，此处测试用了KeyA
            byte[] password = new byte[7];

            Key.Clear();
            Key.Append(this.KeyABox.Text.ToString());
            dev.StrToHex(Key, 12, password);

            secNr = (byte)this.secnrBox.SelectedIndex;

            ret = dev.PICC_Reader_Authentication_Pass(Rhandle, mode, secNr, password);
            if (ret != 0)
            {
                Key.Clear();
                Key.Append(this.KeyBBox.Text.ToString());
                dev.StrToHex(Key, 12, password);

                ret = dev.PICC_Reader_Authentication_Pass(Rhandle, (byte)0x61, secNr, password);
                if (ret != 0)
                {
                    this.status.Text = "秘钥认证失败";
                    return;
                }
            }
            this.status.Text = "秘钥认证成功";
        }

        private void btn_readcard_Click(object sender, EventArgs e)
        {
            byte Addr = (byte)((secNr * 4) + this.addrBox.SelectedIndex); 

            byte[] hexData = new byte[17];
            StringBuilder sbData = new StringBuilder(50);

            ret = dev.PICC_Reader_Read(Rhandle, Addr, hexData);
            if (ret != 0)
            {
                this.status.Text = "读卡失败";
                return;
            }
            dev.HexToStr(hexData, 16, sbData);

            
            this.textBox1.Text = "读卡成功\r\n";
            this.textBox1.Text += "hexstr:";
            this.textBox1.Text += sbData.ToString();
            this.textBox1.Text += "\r\nASC:";
            this.textBox1.Text += System.Text.Encoding.Default.GetString(hexData);
            
        }

        private void btn_writecard_Click(object sender, EventArgs e)
        {
            byte Addr = (byte)((secNr * 4) + this.addrBox.SelectedIndex);
            byte[] hexData = new byte[17];


            //以下两种方式根据自身需求选用
#if false
            StringBuilder sbData = new StringBuilder(this.textBox2.Text.ToString());//1
            hd100.StrToHex(sbData, sbData.Length, hexData);
#else
            String sData = this.textBox2.Text.ToString();
            hexData = Encoding.GetEncoding("GB2312").GetBytes(sData.ToCharArray());//2
#endif

            ret = dev.PICC_Reader_Write(Rhandle, Addr, hexData);
            if (ret != 0)
            {
                this.status.Text = "写卡失败";
                return;
            }
       
            this.textBox1.Text = "写卡成功\r\n";
            
        }
    }
}
