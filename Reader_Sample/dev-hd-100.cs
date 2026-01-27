using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Reader_Sample
{
    class dev
    {

        //打开端口 
        [DllImport("HDSSSE32.dll", EntryPoint = "ICC_Reader_Open")]
        public static extern int ICC_Reader_Open(StringBuilder dev_Name);

        //关闭端口
        [DllImport("HDSSSE32.dll", EntryPoint = "ICC_Reader_Close")]
        public static extern int ICC_Reader_Close(int ReaderHandle);

        //读磁条卡
        [DllImport("HDSSSE32.dll", EntryPoint = "Rcard")]
        public static extern int Rcard(int ReaderHandle, byte ctime, int track, byte[] rlen, StringBuilder data);

        [DllImport("HDSSSE32.dll", EntryPoint = "ICC_PosBeep")]
        public static extern int ICC_PosBeep(int ReaderHandle, byte time);/*蜂鸣*/

        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_SetTypeA")]
        public static extern int PICC_Reader_SetTypeA(int ReaderHandle);//设置读typeA

        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_Select")]
        public static extern int PICC_Reader_Select(int ReaderHandle, byte cardtype);//选择卡片，41为typea,M1 42为typeb,TypeB卡片需先上电后选卡

        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_Request")]
        public static extern int PICC_Reader_Request(int ReaderHandle);//typea & M1 请求卡片

        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_anticoll")]
        public static extern int PICC_Reader_anticoll(int ReaderHandle, byte[] uid);//防碰撞 typea M1卡片

        //注意：输入的是12位的密钥，例如12个f，但是password必须是6个字节的密钥，需要用StrToHex函数处理。
        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_Authentication_Pass")]
        public static extern int PICC_Reader_Authentication_Pass(int ReaderHandle,
                                                                    byte Mode,
                                                                    byte SecNr,
                                                                    byte[] PassWord);
        //读卡
        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_Read")]
        public static extern int PICC_Reader_Read(int ReaderHandle, byte Addr,
                                                        byte[] Data);
        //写卡
        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_Write")]
        public static extern int PICC_Reader_Write(int ReaderHandle, byte Addr,
                                                        byte[] Data);

        //将字符命令流转为16进制流
        [DllImport("HDSSSE32.dll", EntryPoint = "StrToHex")]
        public static extern int StrToHex(StringBuilder strIn, int len, Byte[] HexOut);

        //将16进制流命令转为字符流
        [DllImport("HDSSSE32.dll", EntryPoint = "HexToStr")]
        public static extern int HexToStr(Byte[] strIn, int inLen,
                                                StringBuilder strOut);

        
        //接触CPU
        [DllImport("HDSSSE32.dll", EntryPoint = "ICC_Reader_pre_PowerOn")]
        public static extern int ICC_Reader_pre_PowerOn(int ReaderHandle, byte SLOT, byte[] Response);//上电 返回数据长度 失败小于0

        [DllImport("HDSSSE32.dll", EntryPoint = "ICC_Reader_Application")]
        public static extern int ICC_Reader_Application(int ReaderHandle, byte SLOT, int Lenth_of_Command_APDU, byte[] Command_APDU, byte[] Response_APDU);  //type a/b执行apdu命令 返回数据长度 失败小于0

        //非接CPU
        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_PowerOnTypeA")]
        public static extern int PICC_Reader_PowerOnTypeA(int ReaderHandle, byte[] Response);//上电 返回数据长度 失败小于0

        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_Application")]
        public static extern int PICC_Reader_Application(int ReaderHandle, int Lenth_of_Command_APDU, byte[] Command_APDU, byte[] Response_APDU);  //type a/b执行apdu命令 返回数据长度 失败小于0

        //身份证
        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_ReadIDMsg")]
        public static extern int PICC_Reader_ReadIDMsg(int RHandle, StringBuilder pBmpFile, StringBuilder pName, StringBuilder pSex, StringBuilder pNation, StringBuilder pBirth, StringBuilder pAddress, StringBuilder pCertNo, StringBuilder pDepartment, StringBuilder pEffectData, StringBuilder pExpire, StringBuilder pErrMsg);


        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_ID_ReadUID")]
        public static extern int PICC_Reader_ID_ReadUID(int ReaderHandle, StringBuilder UID);//上电 返回数据长度 失败小于0


        // 读身份证 
        [DllImport("HDSSSE32.dll", EntryPoint = "PICC_Reader_ReadIDCard")]
        public static extern int PICC_Reader_ReadIDCard(int ReaderHandle, StringBuilder err);

        // 获取证件类型
        [DllImport("HDSSSE32.dll", EntryPoint = "GetCardType",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetCardType();

        // 姓名(类型为1时表示：外国人中文姓名)
        [DllImport("HDSSSE32.dll", EntryPoint = "GetName",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetName(StringBuilder name);

        // 性别
        [DllImport("HDSSSE32.dll", EntryPoint = "GetSex",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetSex(StringBuilder sex);

        // 民族
        [DllImport("HDSSSE32.dll", EntryPoint = "GetNation",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetNation(StringBuilder Nation);

        // 出生日期
        [DllImport("HDSSSE32.dll", EntryPoint = "GetBirth",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetBirth(StringBuilder Birth);

        // 住址
        [DllImport("HDSSSE32.dll", EntryPoint = "GetAddress",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetAddress(StringBuilder Address);

        // 公民身份证号码(类型为1时表示：外国人居留证号码)
        [DllImport("HDSSSE32.dll", EntryPoint = "GetCertNo",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetCertNo(StringBuilder CertNo);

        // 签发机关
        [DllImport("HDSSSE32.dll", EntryPoint = "GetDepartemt",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetDepartemt(StringBuilder Departemt);

        // 有效起始日期
        [DllImport("HDSSSE32.dll", EntryPoint = "GetEffectDate",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetEffectDate(StringBuilder EffectDate);

        // 有效截止日期
        [DllImport("HDSSSE32.dll", EntryPoint = "GetExpireDate",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetExpireDate(StringBuilder ExpireDate);

   
        // 生成照片
        [DllImport("HDSSSE32.dll", EntryPoint = "GetBmpFile",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetBmpFile(StringBuilder pBmpfilepath);


        // 外国人英文姓名
        [DllImport("HDSSSE32.dll", EntryPoint = "GetEnName",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetEnName(StringBuilder EnName);


        // 外国人国籍代码 符合GB/T2659-2000规定
        [DllImport("HDSSSE32.dll", EntryPoint = "GetNationalityCode",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetNationalityCode(StringBuilder NationalityCode);

        // 港澳台通行证号码
        [DllImport("HDSSSE32.dll", EntryPoint = "GetTXZHM",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTXZHM(StringBuilder txzhm);

        // 港澳台通行证签发次数
        [DllImport("HDSSSE32.dll", EntryPoint = "GetTXZQFCS",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTXZQFCS(StringBuilder txzqfcs);
        
    }
}
