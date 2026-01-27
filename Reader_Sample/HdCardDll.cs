using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows.Forms;

namespace ATMS
{
	
	
	//-----------------调用方法---------------------------
	
	    [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        private void readCert()
        {
            yes = true;
            do
            {
                Application.DoEvents(); 
                int port = 1001;
                //初始化设备
                port = HdCardDll.HD_InitComm(port);
                if (port < 0)
                {
                    // MessageBox.Show("没打开" + port);
                    HdCardDll.HD_CloseComm();
                }
                port = HdCardDll.HD_Authenticate(1);
                //选卡鉴权  
                if (port != 0)
                {
                    HdCardDll.HD_CloseComm();
                }
                else
                {
					//单独获取模式
                    long b = HdCardDll.HD_ReadCard(); 
                    try
                    {
                        string name = HdCardDll.GetName();//获取姓名
                        string certno = HdCardDll.GetCertNo();//获取身份证号
						MessageBox.Show(name);
                        MessageBox.Show(certno); 
                        yes = false;
                        HdCardDll.HD_CloseComm();
                        break;
                    }
                    catch (AccessViolationException ex)
                    {
                        HdCardDll.HD_CloseComm();
                    }
                }
                Application.DoEvents();

            } while (yes);
           
        }
	
	    //一次获取全部信息
		    StringBuilder pBmpData = new StringBuilder();
            StringBuilder pName = new StringBuilder();
            StringBuilder pSex = new StringBuilder();
            StringBuilder pNation = new StringBuilder();
            StringBuilder pBirth = new StringBuilder();
            StringBuilder pAddress = new StringBuilder();
            StringBuilder pCertNo = new StringBuilder();
            StringBuilder pDepartment = new StringBuilder();
            StringBuilder pEffectData = new StringBuilder();
            StringBuilder pExpire = new StringBuilder();
	         
	         pBmpData.Append("d:\\a.bmp");
			 HdCardDll.HD_InitComm(1001);
			 HdCardDll.HD_Authenticate(1);
             int c = HdCardDll.HD_Read_BaseMsg(pBmpData,  pName,  pSex,  pNation,  pBirth,  pAddress,  pCertNo,  pDepartment,  pEffectData,  pExpire);		
             HdCardDll.HD_CloseComm();		
             MessageBox.Show(pName.toString());			 
	//------------------------------------------------------
     
    public class HdCardDll
    {
        /// <summary>
        ///  根据指定端口初始化设备通讯。
        /// </summary>
        /// <param name="iPort">整数，表示端口号; 1-16，表示串口;1001，表示USB。</param>
        /// <returns>1		成功 其他  失败（具体含义参见返回码表）</returns>
        [DllImport("HDstdapi.dll")]
        public static extern int HD_InitComm(int iPort); 

        /// <summary>
        /// 关闭设备
        /// </summary>
        /// <returns>成功返回1</returns>
        [DllImport("HDstdapi.dll")]
        public static extern int HD_CloseComm();
         
 

        [DllImport("HDstdapi.dll")]
        public static extern int HD_Authenticate(int iType);
 


        /// <summary>
        /// 读卡
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern long  HD_ReadCard();

		
		
        /// <summary>
        /// 获取身份证全部信息
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern int HD_Read_BaseMsg(StringBuilder pBmpData, StringBuilder pName, StringBuilder pSex, StringBuilder pNation, StringBuilder pBirth, StringBuilder pAddress, StringBuilder pCertNo, StringBuilder pDepartment, StringBuilder pEffectData, StringBuilder pExpire);

        /// <summary>
        /// 获取姓名
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern string GetName();


        /// <summary>
        /// 获取身份证号
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern string GetCertNo();


        /// <summary>
        /// 获取身份证类型  0：居民身份证 1：外国人永久居留证 2：港澳台居民居住证
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern int GetCardType();

        /// <summary>
        /// 获取性别
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern string GetSex();


        /// <summary>
        /// 获取民族
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern string GetNation();

        /// <summary>
        /// 获取出生日期
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern string GetBirth();

        /// <summary>
        /// 获取住址
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern string GetAddress();

        /// <summary>
        /// 获取签发机关
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern string GetDepartemt();

        /// <summary>
        /// 获取有效期起
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern string GetEffectDate();


        /// <summary>
        /// 获取有效期止
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern string GetExpireDate();

        /// <summary>
        /// bmp格式照片数据
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern string GetBmpFileData();



        /// <summary>
        /// 生成照片 传入路径 例:D:\
        /// </summary>
        /// <returns></returns>
        [DllImport("HDstdapi.dll")]
        public static extern int  GetBmpFile(string pBmpfilepath);

        //-----------其他-----
//        int WINAPI IsFingerExist(); 是否含存在指纹信息：
//存在时返回512或者1024
//不存在时返回0
//int WINAPI GetFingerprint(unsigned char* fpInfo); 获取指纹数据：
//成功时返回获取到的字节长度
//char* WINAPI GetEnName(); 外国人英文姓名
//char* WINAPI GetNationalityCode(); 外国人国籍代码，符合GB/T2659-2000规定
//char* WINAPI GetTXZHM(); 港澳台通行证号码
//char* WINAPI GetTXZQFCS(); 港澳台通行证签发次数


    }

}
