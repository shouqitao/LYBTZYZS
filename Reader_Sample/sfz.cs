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
    public partial class sfz : Form
    {
        public sfz()
        {
            InitializeComponent();
        }

        int nRt = -99;
        private StringBuilder dev_name = new StringBuilder("USB1");


        private void button1_Click(object sender, EventArgs e)
        {
            StringBuilder pBmpFile = new StringBuilder(200);
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
            StringBuilder pAuthorCode = new  StringBuilder(100);
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
           //int ret = dev.PICC_Reader_Read_CardID(Rhandle, uid);
            //dev.HexToStr(uid, ret, sUID);




#endif
            //该函数获取身份证信息的同时保存照片到指定路径

            nRt = dev.PICC_Reader_ReadIDMsg(Rhandle,  pBmpFile,  pName,  pSex,  pNation,  pBirth,  pAddress,  pCertNo,  pDepartment,  pEffectData,  pExpire,  pErrMsg);
            if (nRt == 0)
            {
              
                    this.textBox1.Text += "姓名：" + pName.ToString() + "\r\n";
                    this.textBox1.Text += "性别：" + pSex.ToString() + "\r\n";
                    this.textBox1.Text += "民族：" + pNation.ToString() + "\r\n";
                    this.textBox1.Text += "出生日期：" + pBirth.ToString() + "\r\n";
                    this.textBox1.Text += "住址：" + pAddress.ToString() + "\r\n";
                    this.textBox1.Text += "身份证号码：" + pCertNo.ToString() + "\r\n";
                    this.textBox1.Text += "签发机关：" + pDepartment.ToString() + "\r\n";
                    this.textBox1.Text += "有效起始日期：" + pEffectData.ToString() + "\r\n";
                    this.textBox1.Text += "有效截止日期：" + pExpire.ToString() + "\r\n";


               // this.textBox1.Text += "身份证ID：" + sUID.ToString() + "\r\n";

                this.pictureBox1.Image = Image.FromFile(pBmpFile.ToString());
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                this.pictureBox1.Show();
            }

            else
            {
                this.status.Text = "读卡失败,返回值：";
                this.status.Text += nRt.ToString();
                return;
            }



            /*
            nRt = dev.PICC_Reader_ReadIDCard(Rhandle, pErrMsg);
            if ( nRt == 0 )
            {
                //textBox1
                int icardType = -1;
                icardType = dev.GetCardType();

                if (icardType == 0)
                {
                    dev.GetName(pName);
                    dev.GetSex(pSex);
                    dev.GetNation(pNation);
                    dev.GetBirth(pBirth);
                    dev.GetCertNo(pCertNo);
                    dev.GetAddress(pAddress);
                    dev.GetDepartemt(pDepartment);
                    dev.GetEffectDate(pEffectData);
                    dev.GetExpireDate(pExpire);

                    this.textBox1.Text = "读卡成功，证件类型：居民身份证" + "\r\n";
                    this.textBox1.Text += "姓名：" + pName.ToString() + "\r\n";
                    this.textBox1.Text += "性别：" + pSex.ToString() + "\r\n";
                    this.textBox1.Text += "民族：" + pNation.ToString() + "\r\n";
                    this.textBox1.Text += "出生日期：" + pBirth.ToString() + "\r\n";
                    this.textBox1.Text += "住址：" + pAddress.ToString() + "\r\n";
                    this.textBox1.Text += "身份证号码：" + pCertNo.ToString() + "\r\n";
                    this.textBox1.Text += "签发机关：" + pDepartment.ToString() + "\r\n";
                    this.textBox1.Text += "有效起始日期：" + pEffectData.ToString() + "\r\n";
                    this.textBox1.Text += "有效截止日期：" + pExpire.ToString() + "\r\n";
                }
                if (icardType == 2)
                {
                    dev.GetName(pName);
                    dev.GetSex(pSex);

                    dev.GetBirth(pBirth);
                    dev.GetCertNo(pCertNo);
                    dev.GetAddress(pAddress);
                    dev.GetDepartemt(pDepartment);
                    dev.GetEffectDate(pEffectData);
                    dev.GetExpireDate(pExpire);

                    dev.GetTXZHM(pTXZHM);
                    dev.GetTXZQFCS(pTXZQFCS);

                    this.textBox1.Text = "读卡成功，证件类型：港澳台居民居住证" + "\r\n";
                    this.textBox1.Text += "姓名：" + pName.ToString() + "\r\n";
                    this.textBox1.Text += "性别：" + pSex.ToString() + "\r\n";

                    this.textBox1.Text += "出生日期：" + pBirth.ToString() + "\r\n";
                    this.textBox1.Text += "住址：" + pAddress.ToString() + "\r\n";
                    this.textBox1.Text += "身份证号码：" + pCertNo.ToString() + "\r\n";
                    this.textBox1.Text += "签发机关：" + pDepartment.ToString() + "\r\n";
                    this.textBox1.Text += "有效起始日期：" + pEffectData.ToString() + "\r\n";
                    this.textBox1.Text += "有效截止日期：" + pExpire.ToString() + "\r\n";
                    this.textBox1.Text += "通行证号码：" + pTXZHM.ToString() + "\r\n";
                    this.textBox1.Text += "通行证签发次数：" + pTXZQFCS.ToString() + "\r\n";
                }
                if (icardType == 1)
                {
                    dev.GetEnName(pEnName);
                    dev.GetName(pName);
                    dev.GetSex(pSex);
                    dev.GetBirth(pBirth);
                    dev.GetCertNo(pCertNo);                    
                    dev.GetEffectDate(pEffectData);
                    dev.GetExpireDate(pExpire);
                    dev.GetNationalityCode(pEnNation);

                    this.textBox1.Text = "读卡成功，证件类型：外国人永久居留证" + "\r\n";
                    this.textBox1.Text += "中文姓名：" + pName.ToString() + "\r\n";
                    this.textBox1.Text += "英文姓名：" + pEnName.ToString() + "\r\n";
                    this.textBox1.Text += "国籍代码：" + pEnNation.ToString() + "\r\n";
                    this.textBox1.Text += "出生日期：" + pBirth.ToString() + "\r\n";
                    this.textBox1.Text += "永久证号码：" + pCertNo.ToString() + "\r\n";
                    this.textBox1.Text += "有效起始日期：" + pEffectData.ToString() + "\r\n";
                    this.textBox1.Text += "有效截止日期：" + pExpire.ToString() + "\r\n";

                }

                this.textBox1.Text += "身份证ID：" + sUID.ToString() + "\r\n";

                dev.GetBmpFile(pBmpFile);
                this.pictureBox1.Image = Image.FromFile(pBmpFile.ToString());
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                this.pictureBox1.Show();


                return;
            }
            else
            {
                this.status.Text = "读卡失败,返回值：";
                this.status.Text += nRt.ToString();
                return;
            }
            */

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.textBox1.Text = "";
            this.status.Text = "";
            this.pictureBox1.Hide();
        }

        private void sfz_Load(object sender, EventArgs e)
        {

        }

        private void btnIDUID_Click(object sender, EventArgs e)
        {
            int Rhandle;
            StringBuilder sbShow = new StringBuilder(512);
            Rhandle = dev.ICC_Reader_Open(dev_name);
            if (Rhandle <= 0)
            {
                this.textBox1.Text += "设备连接失败\r\n";
                return;
            }

            //获取身份证ID
            byte[] uid = new byte[20];
            StringBuilder sUID = new StringBuilder(30);
            int ret = dev.PICC_Reader_Read_CardID(Rhandle, uid);

            dev.HexToStr(uid, ret, sbShow);
            this.textBox1.Text += "身份证ID：" + sbShow.ToString() + "\r\n";
        }
    }
}
