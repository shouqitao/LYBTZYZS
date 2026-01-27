using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using System.Threading;
using Reader_Sample;

namespace Reader_Sample
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            m1 formM1 = new m1();
            formM1.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            sfz formSFZ = new sfz();
            formSFZ.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {

            CPU formcpu = new CPU();
            formcpu.ShowDialog();
        }

        private void btn15693_Click(object sender, EventArgs e)
        {
            _15693 _15693 = new _15693();
            _15693.ShowDialog();
        }


        private void button4_Click(object sender, EventArgs e)
        {
            StringBuilder dev_name = new StringBuilder("USB1");
            byte[] sn = new byte[4];
            byte[] date = new byte[10];
            byte[] kh = new byte[255]; //这里是完整的二磁道数据，只需要卡号的自行截取 = 号之前的数据即可
            byte[] kh_len = new byte[2];
            int iType = 1;
            int RHandle = -1;

            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                textBox2.Text += "设备未连接\r\n";
                return;
            }
            int nRt = dev.ICC_GetBankCardNo(RHandle, 0,  kh, kh_len);
            if (nRt != 0)
            {
                textBox2.Text += "读卡失败\r\n";
                return;
            }

            textBox2.Text = System.Text.Encoding.Default.GetString(kh);

            dev.ICC_Reader_Close(RHandle);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            StringBuilder dev_name = new StringBuilder("USB1");
            StringBuilder pName = new StringBuilder(100);
            StringBuilder pSex1 = new StringBuilder(100);
            StringBuilder pNation = new StringBuilder(100);
            StringBuilder pBirth1 = new StringBuilder(100);
            StringBuilder pCertNo = new StringBuilder(100);
            StringBuilder pEffectData = new StringBuilder(100);
            StringBuilder pExpire = new StringBuilder(100);
            StringBuilder pCardNo = new StringBuilder(19);
            StringBuilder pErrMsg = new StringBuilder(100);

            byte[] pSex = new byte[255];
            byte[] pBirth = new byte[255];


            int iType = 1;
            int RHandle = -1;

            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                textBox2.Text += "设备未连接\r\n";
                return;
            }
            int nRt = dev.PICC_Reader_SSCardInfo1(RHandle, pCardNo, pCertNo,pName, pSex,  pBirth);
            if (nRt != 0)
            {
                textBox2.Text += "读卡失败" + pErrMsg.ToString() + "\r\n";
                return;
            }
            dev.HexToStr(pBirth, 4, pBirth1);
            dev.HexToStr(pSex, 1, pSex1);

            this.textBox2.Text = "";
            this.textBox2.Text += pCardNo.ToString();
            this.textBox2.Text += "|" + pName.ToString();
            this.textBox2.Text += "|" + pSex1.ToString();
            this.textBox2.Text += "|" + pBirth1.ToString();
            this.textBox2.Text += "|" + pCertNo.ToString();
         

            dev.ICC_Reader_Close(RHandle);
        }

        private void btn_magcard_Click(object sender, EventArgs e)
        {
            int RHandle = -1;
            StringBuilder dev_name = new StringBuilder("USB1");
            StringBuilder cardInfo = new StringBuilder(100);

            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                textBox2.Text += "设备未连接\r\n";
                return;
            }
            byte ctime = 10; //10秒
            byte track = 2;//2轨
            byte[] rlen = new byte[2];
            int nRt = dev.Rcard(RHandle, ctime, track, rlen, cardInfo);
            if (nRt != 0)
            {
                textBox2.Text += "读磁卡失败\r\n";
                return;
            }
            textBox2.Text += "读卡成功" + cardInfo.ToString() + "\r\n";
        }

        private void btnScanQRCode_Click(object sender, EventArgs e)
        {
            int RHandle = -1;
            StringBuilder dev_name = new StringBuilder("USB1");
            StringBuilder cardInfo = new StringBuilder(1000);

            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                textBox2.Text += "设备未连接\r\n";
                return;
            }

            int re = dev.ICC_CtrScanCode(RHandle,2,0);

            int nRt = dev.ICC_ScanCodeSM(RHandle, 10, cardInfo,0);
            if (nRt < 0)
            {
                textBox2.Text += "扫码失败\r\n";
                return;
            }
            textBox2.Text += "扫码成功:" + cardInfo.ToString() + "\r\n";
        }

        /// <summary>
        /// 接触式银行卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            lb_银行卡.Text = "";
            string T1 = DateTime.Now.ToLongTimeString();
            StringBuilder dev_name = new StringBuilder("USB1");
            //StringBuilder dev_name = new StringBuilder("USB1");
            byte[] sn = new byte[4];
            byte[] date = new byte[10];
            byte[] kh = new byte[255]; //这里是完整的二磁道数据，只需要卡号的自行截取 = 号之前的数据即可
            byte[] kh_len = new byte[2];
            int iType = 0;
            int RHandle = -1;

            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                lb_银行卡.Text += "设备未连接\r\n";
                return;
            }
            int nRt = dev.ICC_GetBankCardNo(RHandle, iType, kh, kh_len);
            if (nRt != 0)
            {
                lb_银行卡.Text += "读卡失败\r\n";
                return;
            }

            string T2 = DateTime.Now.ToLongTimeString();
            string YHK = System.Text.Encoding.Default.GetString(kh).Split('=')[0];
            lb_银行卡.Text = "接触卡：按下：" + T1 + "返回值：" + T2 + "银行卡号：" + YHK;
            tbx_银行卡.Text = "接触卡"+YHK;
            dev.ICC_Reader_Close(RHandle);
        }
        /// <summary>
        /// 刷磁条卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            int RHandle = -1;
            StringBuilder dev_name = new StringBuilder("USB1");
            StringBuilder cardInfo = new StringBuilder(100);

            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                lb_银行卡.Text += "设备未连接\r\n";
                return;
            }
            byte ctime = 10; //10秒
            byte track = 2;//2轨
            byte[] rlen = new byte[2];
            int nRt = dev.Rcard(RHandle, ctime, track, rlen, cardInfo);
            if (nRt != 0)
            {
                lb_银行卡.Text += "读磁卡失败\r\n";
                return;
            }
            lb_银行卡.Text += "读卡成功" + cardInfo.ToString() + "\r\n";
            tbx_银行卡.Text = "磁条卡刷卡：" + cardInfo.ToString().Split('=')[0];
        }

        private int Rhandle = -99;
        private int ret;
        private StringBuilder devname = new StringBuilder("USB1");
        private StringBuilder Key = new StringBuilder(50);

        private byte[] UID = new byte[5];// 物理卡号，需要时可在此处获得
        private byte secNr;

        private void button9_Click(object sender, EventArgs e)
        {
            //连接读卡器
            Rhandle = dev.ICC_Reader_Open(devname);
            if (Rhandle < 0)
            {
                lb_蚕农卡.Text = "读卡器连接失败";
                return;
            }
            //非接基本函数
            ret = dev.PICC_Reader_SetTypeA(Rhandle);
            if (ret != 0)
            {
                lb_蚕农卡.Text = "设置为A卡模式失败";
                return;
            }
            ret = dev.PICC_Reader_Request(Rhandle);
            if (ret != 0)
            {
                lb_蚕农卡.Text = "请求卡片失败";
                return;
            }

            ret = dev.PICC_Reader_anticoll(Rhandle, UID);
            if (ret != 0)
            {
                lb_蚕农卡.Text = "防碰撞失败";
                return;
            }
            ret = dev.PICC_Reader_Select(Rhandle, 0x41);
            if (ret != 0)
            {
                lb_蚕农卡.Text = "选卡失败";
                return;
            }

            StringBuilder strBuider = new StringBuilder();
            for (int index = 0; index < 4; index++)
            {
                strBuider.Append(((int)UID[index]).ToString("X2"));
            }
            
            long a = Convert.ToInt64(strBuider.ToString(), 16);


            //StringBuilder sbData = new StringBuilder(8);

            //dev.HexToStr(UID, 10, sbData);

            //MessageBox.Show(a);
            lb_蚕农卡.Text = "寻卡成功:"+"16进制卡号："+ strBuider.ToString() + " 转数值：" +a;
            tbx_蚕农卡.Text = a.ToString();
            //System.Text.Encoding.ASCII.GetString(UID)



            //第二步 加载密钥



        }

        //读取身份证


        int nRt = -99;
        private StringBuilder dev_name = new StringBuilder("USB1");
        private void button10_Click(object sender, EventArgs e)
        {
            StringBuilder pBmpFile = new StringBuilder(100);
            StringBuilder pFingerData = new StringBuilder(1025);
            StringBuilder pBmpData = new StringBuilder(77725);
            StringBuilder pBase64Data = new StringBuilder(6025);
            StringBuilder pName = new StringBuilder(100);
            StringBuilder pSex = new StringBuilder(100);
            StringBuilder pNation = new StringBuilder(100);
            StringBuilder pBirth = new StringBuilder(100);
            StringBuilder pAddress = new StringBuilder(100);
            StringBuilder pCertNo = new StringBuilder(100);
            StringBuilder pDepartment = new StringBuilder(100);
            StringBuilder pEffectData = new StringBuilder(100);
            StringBuilder pExpire = new StringBuilder(100);
            StringBuilder pData = new StringBuilder(100);
            StringBuilder pErrMsg = new StringBuilder(100);
            StringBuilder pTXZHM = new StringBuilder(100);
            StringBuilder pTXZQFCS = new StringBuilder(100);

            StringBuilder pEnName = new StringBuilder(200);
            StringBuilder pEnNation = new StringBuilder(100);
            StringBuilder pAuthorCode = new StringBuilder(100);
            StringBuilder pCardVersion = new StringBuilder(100);

            string str = System.Environment.CurrentDirectory;
            pBmpFile.Append(str);
            pBmpFile.Append(@"\zp.bmp");


            int Rhandle;
            Rhandle = dev.ICC_Reader_Open(dev_name);
            if (Rhandle <= 0)
            {
                return;
            }
#if true
            //获取身份证ID
            byte[] uid = new byte[20];
            StringBuilder sUID = new StringBuilder(30);
            int ret = dev.PICC_Reader_Read_CardID(Rhandle, uid);
            dev.HexToStr(uid, ret, sUID);


#endif
            //该函数获取身份证信息的同时保存照片到指定路径
            nRt = dev.PICC_Reader_ReadIDMsg(Rhandle, pBmpFile, pName, pSex, pNation, pBirth, pAddress, pCertNo, pDepartment, pEffectData, pExpire, pErrMsg);
  
               if (nRt == 0)
            {
              

                    this.tbx_身份证.Text = "读卡成功，证件类型：居民身份证" + "\r\n";
                    this.tbx_身份证.Text += "姓名：" + pName.ToString() + "\r\n";
                    this.tbx_身份证.Text += "性别：" + pSex.ToString() + "\r\n";
                    this.tbx_身份证.Text += "民族：" + pNation.ToString() + "\r\n";
                    this.tbx_身份证.Text += "出生日期：" + pBirth.ToString() + "\r\n";
                    this.tbx_身份证.Text += "住址：" + pAddress.ToString() + "\r\n";
                    this.tbx_身份证.Text += "身份证号码：" + pCertNo.ToString() + "\r\n";
                    this.tbx_身份证.Text += "签发机关：" + pDepartment.ToString() + "\r\n";
                    this.tbx_身份证.Text += "有效起始日期：" + pEffectData.ToString() + "\r\n";
                    this.tbx_身份证.Text += "有效截止日期：" + pExpire.ToString() + "\r\n";
          

                this.tbx_身份证.Text += "身份证ID：" + sUID.ToString() + "\r\n";

                this.pictureBox1.Image = Image.FromFile(pBmpFile.ToString());
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                this.pictureBox1.Show();


                return;
            }
            else
            {
                this.lb_身份证.Text = "读卡失败,返回值：";
                this.lb_身份证.Text += nRt.ToString();
                return;
            }

        }



        /// <summary>
        /// 非接银行卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        //private int Rhandle = -99;
        //int nRt;
        byte slot;
        //StringBuilder dev_name = new StringBuilder("USB1");
        private void button7_Click(object sender, EventArgs e)
        {

            lb_银行卡.Text = "";
            string T1 = DateTime.Now.ToLongTimeString();
            StringBuilder dev_name = new StringBuilder("USB1");
            //StringBuilder dev_name = new StringBuilder("USB1");
            byte[] sn = new byte[4];
            byte[] date = new byte[10];
            byte[] kh = new byte[255]; //这里是完整的二磁道数据，只需要卡号的自行截取 = 号之前的数据即可
            byte[] kh_len = new byte[2];
            int iType = 1;
            int RHandle = -1;

            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                lb_银行卡.Text += "设备未连接\r\n";
                return;
            }
            int nRt = dev.ICC_GetBankCardNo(RHandle, iType, kh, kh_len);
            if (nRt != 0)
            {
                lb_银行卡.Text += "读卡失败\r\n";
                return;
            }

            string T2 = DateTime.Now.ToLongTimeString();
            string YHK = System.Text.Encoding.Default.GetString(kh).Split('=')[0];
            lb_银行卡.Text = "非接触卡：按下：" + T1 + "返回值：" + T2 + "银行卡号：" + YHK;
            tbx_银行卡.Text = "非接触卡" + YHK;
            dev.ICC_Reader_Close(RHandle);

            string BankCode = null;
            BankCode = YHK.Substring(0, 32);
            BankCode = BankCode.Split('D')[0];

            tbx_银行卡.Text = "非接刷卡银行卡号：" + BankCode;


        }

        private string SendApDU(string APDU)
        {
            byte[] resp = new byte[255];
            byte[] cmd = new byte[255];

            StringBuilder sbShow = new StringBuilder(512);
            StringBuilder sbCmd = new StringBuilder(APDU);
            int cmdlen = APDU.Length / 2;

            dev.StrToHex(sbCmd, cmdlen * 2, cmd);
            if (slot != 6)
            {
                nRt = dev.ICC_Reader_Application(Rhandle, slot, cmdlen, cmd, resp);
                if (nRt < 2)
                {
                    lb_银行卡.Text = "APDU命令执行失败";
                    return "";
                }
            }
            else
            {
                nRt = dev.PICC_Reader_Application(Rhandle, cmdlen, cmd, resp);
                if (nRt < 2)
                {
                    lb_银行卡.Text = "APDU命令执行失败";
                    return "";
                }
            }

            dev.HexToStr(resp, nRt, sbShow);
            return sbShow.ToString();
            //this.showBox.Text += "响应：";
            //this.showBox.Text += sbShow.ToString();
            //this.showBox.Text += "\r\n";
            //this.showBox.SelectionStart = this.showBox.Text.Length; //设定光标位置
            //this.showBox.ScrollToCaret(); //滚动到光标处
        }

        private void button11_Click(object sender, EventArgs e)
        {

            int RHandle = -1;
            StringBuilder dev_name = new StringBuilder("USB1");
            StringBuilder cardInfo = new StringBuilder(1000);

            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                textBox2.Text += "设备未连接\r\n";
                return;
            }

            int re = dev.ICC_CtrScanCode(RHandle, 2, 0);

            int nRt = dev.ICC_ScanCodeStar(RHandle, cardInfo, 0);
            if ((nRt < 0 )&& (nRt != -16) )
            {
                textBox2.Text += "扫码失败\r\n";
                return;
            }
            if (nRt == -16)
            {
                //BackGroundTest background = new BackGroundTest(RHandle);//创建前台线程
                //Thread fThread = new Thread(new ThreadStart(background.RunLoop));

                //fThread.Start();

                new Thread(() =>
                {

                    int ret = -16; ;
                    StringBuilder str = new StringBuilder(1000);


                    while (ret == -16)
                    {
                        ret = dev.ICC_ScanCodeRead(RHandle, str);
                    }
                    if (ret < 0)
                    {
                        textBox2.Text += "扫码失败\r\n";
                        return;
                    }
                    textBox2.Text += "扫描到的内容：" + str.ToString();
                }).Start();

            }

            else
            {
                textBox2.Text += "扫码成功:" + cardInfo.ToString() + "\r\n";
            }


        }
        int RHandle = -1;
        private void button12_Click(object sender, EventArgs e)
        {

            byte[] resp = new byte[3000];
            int ret;
            //byte[] lent = new byte[2];
            int lent=-1;
            byte[] pSex = new byte[255];
            byte[] pBirth = new byte[255];
           
            StringBuilder pName = new StringBuilder(100);
            StringBuilder pSex1 = new StringBuilder(100);
            StringBuilder pNation = new StringBuilder(100);
            StringBuilder pBirth1 = new StringBuilder(100);
            StringBuilder pCertNo = new StringBuilder(100);
            StringBuilder pEffectData = new StringBuilder(100);
            StringBuilder pExpire = new StringBuilder(100);
            StringBuilder pCardNo = new StringBuilder(19);
            StringBuilder pErrMsg = new StringBuilder(100);
            StringBuilder dev_name = new StringBuilder("USB1");
            StringBuilder ID  = new StringBuilder(100);
            StringBuilder pBmpFile = new StringBuilder(100);
            StringBuilder pAddress = new StringBuilder(100);
            StringBuilder pDepartment = new StringBuilder(100);
            int nRt=-1;
            string str = System.Environment.CurrentDirectory;
            pBmpFile.Append(str);
            pBmpFile.Append(@"\zp.bmp");

            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                textBox2.Text += "设备未连接\r\n";
                return;
            }



            //while (true)
            //{


                 ret = dev.ICC_SelscetScan(RHandle, resp, ref lent);
                if (ret < 0)
                {
                    textBox2.Text += "寻卡失败\r\n";
                    return;
                }
                switch (ret)
                {
                    case 1:
                        nRt = dev.PICC_Reader_SSCardInfo1(RHandle, pCardNo, pCertNo, pName, pSex, pBirth);
                        if (nRt != 0)
                        {
                            textBox2.Text += "读卡失败" + pErrMsg.ToString() + "\r\n";
                            //return;
                        }
                        dev.HexToStr(pBirth, 4, pBirth1);
                        dev.HexToStr(pSex, 1, pSex1);

                        this.textBox2.Text = "社保卡：";
                        this.textBox2.Text += pCardNo.ToString();
                        this.textBox2.Text += "|" + pName.ToString();
                        this.textBox2.Text += "|" + pSex1.ToString();
                        this.textBox2.Text += "|" + pBirth1.ToString();
                        this.textBox2.Text += "|" + pCertNo.ToString();
                        break;

                    case 2:
                        dev.HexToStr(resp, lent, ID);
                        this.textBox2.Text = "M1卡号：";
                        this.textBox2.Text += ID.ToString() + "\r\n";
                        break;
                    case 3:
                        dev.HexToStr(resp, lent, ID);
                        this.textBox2.Text = "磁条号：";
                        this.textBox2.Text += ID.ToString() + "\r\n";
                        break;
                    case 4:
                        //dev.HexToStr(resp, lent, ID);
                        this.textBox2.Text = "二维码：";
                        this.textBox2.Text += System.Text.Encoding.UTF8.GetString(resp) + "\r\n";
                        break;
                    case 5:
                        nRt = dev.PICC_Reader_ReadIDMsg(RHandle, pBmpFile, pName, pSex1, pNation, pBirth1, pAddress, pCertNo, pDepartment, pEffectData, pExpire, pErrMsg);

                        if (nRt == 0)
                        {


                            this.textBox2.Text = "读卡成功，证件类型：居民身份证" + "\r\n";
                            this.textBox2.Text += "姓名：" + pName.ToString() + "\r\n";
                            this.textBox2.Text += "性别：" + pSex1.ToString() + "\r\n";
                            this.textBox2.Text += "民族：" + pNation.ToString() + "\r\n";
                            this.textBox2.Text += "出生日期：" + pBirth1.ToString() + "\r\n";
                            this.textBox2.Text += "住址：" + pAddress.ToString() + "\r\n";
                            this.textBox2.Text += "身份证号码：" + pCertNo.ToString() + "\r\n";
                            this.textBox2.Text += "签发机关：" + pDepartment.ToString() + "\r\n";
                            this.textBox2.Text += "有效起始日期：" + pEffectData.ToString() + "\r\n";
                            this.textBox2.Text += "有效截止日期：" + pExpire.ToString() + "\r\n";


                            this.pictureBox1.Image = Image.FromFile(pBmpFile.ToString());
                            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                            this.pictureBox1.Show();

                        }
                        else
                        {
                            this.lb_身份证.Text = "读卡失败,返回值：";
                            this.lb_身份证.Text += nRt.ToString() + "\r\n";
                            //return;
                        }
                        break;
                    case 6:
                        byte[] date = new byte[10];
                        byte[] kh = new byte[255]; //这里是完整的二磁道数据，只需要卡号的自行截取 = 号之前的数据即可
                        byte[] kh_len = new byte[2];

                        //nRt = dev.ICC_GetBankCardNo(RHandle, 1, kh, kh_len);
                        //if (nRt != 0)
                        //{
                        //    textBox2.Text += "读卡失败\r\n";
                        //    //return;
                        //}
                        //this.textBox2.Text += "银行卡号：\r\n";
                        //textBox2.Text += System.Text.Encoding.Default.GetString(kh) + "\r\n";

                        break;


                }
            //}



        }

        private void button13_Click(object sender, EventArgs e)
        {
            int ret = dev.ICC_StopSelscetScan(RHandle);
            textBox2.Text += "读卡失败\r\n";
        }

        private void button14_Click(object sender, EventArgs e)
        {

            StringBuilder dev_name = new StringBuilder("USB1");
            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                textBox2.Text += "设备未连接\r\n";
                return;
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            StringBuilder dev_name = new StringBuilder("USB1");
       
            byte[] pOutInfo = new byte[1024]; 
       

            RHandle = dev.ICC_Reader_Open(dev_name);
            if (RHandle <= 0)
            {
                textBox2.Text += "设备未连接\r\n";
                return;
            }
            int nRt = dev.iReadCardBas(3, pOutInfo);
            if (nRt != 0)
            {
                textBox2.Text += System.Text.Encoding.Default.GetString(pOutInfo); 
                return;
            }

            textBox2.Text = System.Text.Encoding.Default.GetString(pOutInfo);

            dev.ICC_Reader_Close(RHandle);
        }
    }

    
        
    }




