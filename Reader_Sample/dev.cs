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
        [DllImport("SSSE32.dll", EntryPoint = "ICC_Reader_Open")]
        public static extern int ICC_Reader_Open(StringBuilder dev_Name);

        //关闭端口
        [DllImport("SSSE32.dll", EntryPoint = "ICC_Reader_Close")]
        public static extern int ICC_Reader_Close(int ReaderHandle);

        //读磁条卡
        [DllImport("SSSE32.dll", EntryPoint = "Rcard")]
        public static extern int Rcard(int ReaderHandle, byte ctime, int track, byte[] rlen, StringBuilder data);

        //扫码
        [DllImport("SSSE32.dll", EntryPoint = "ICC_ScanCodeSM")]
        public static extern int ICC_ScanCodeSM(int ReaderHandle, byte ctime, StringBuilder QRCodeInfo,int WFlag);

        //设置扫码状态
        [DllImport("SSSE32.dll", EntryPoint = "ICC_CtrScanCode")]
        public static extern int ICC_CtrScanCode(int ReaderHandle, int inFlae, int WFlag);
        /*
         * inFlae 参数01表示主动扫码模拟键盘输出，参数02表示指令控制扫码
         * 
         */

        [DllImport("SSSE32.dll", EntryPoint = "ICC_ScanCodeStar")]
        public static extern int ICC_ScanCodeStar(int ReaderHandle, StringBuilder QRCodeInfo, int WFlag);


        [DllImport("SSSE32.dll", EntryPoint = "ICC_ScanCodeStop")]
        public static extern int ICC_ScanCodeStop(int ReaderHandle, StringBuilder QRCodeInfo, int WFlag);

        [DllImport("SSSE32.dll", EntryPoint = "ICC_ScanCodeRead")]
        public static extern int ICC_ScanCodeRead(int ReaderHandle, StringBuilder QRCodeInfo);



        [DllImport("SSSE32.dll", EntryPoint = "ICC_PosBeep")]
        public static extern int ICC_PosBeep(int ReaderHandle, byte time);/*蜂鸣*/

        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_SetTypeA")]
        public static extern int PICC_Reader_SetTypeA(int ReaderHandle);//设置读typeA

        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_Select")]
        public static extern int PICC_Reader_Select(int ReaderHandle, byte cardtype);//选择卡片，41为typea,M1 42为typeb,TypeB卡片需先上电后选卡

        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_Request")]
        public static extern int PICC_Reader_Request(int ReaderHandle);//typea & M1 请求卡片

        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_anticoll")]
        public static extern int PICC_Reader_anticoll(int ReaderHandle, byte[] uid);//防碰撞 typea M1卡片

        //注意：输入的是12位的密钥，例如12个f，但是password必须是6个字节的密钥，需要用StrToHex函数处理。
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_Authentication_Pass")]
        public static extern int PICC_Reader_Authentication_Pass(int ReaderHandle,
                                                                    byte Mode,
                                                                    byte SecNr,
                                                                    byte[] PassWord);
        //读卡
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_Read")]
        public static extern int PICC_Reader_Read(int ReaderHandle, byte Addr,
                                                        byte[] Data);
        //写卡
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_Write")]
        public static extern int PICC_Reader_Write(int ReaderHandle, byte Addr,
                                                        byte[] Data);

        //将字符命令流转为16进制流
        [DllImport("SSSE32.dll", EntryPoint = "StrToHex")]
        public static extern int StrToHex(StringBuilder strIn, int len, Byte[] HexOut);

        //将16进制流命令转为字符流
        [DllImport("SSSE32.dll", EntryPoint = "HexToStr")]
        public static extern int HexToStr(Byte[] strIn, int inLen,
                                                StringBuilder strOut);

        
        //接触CPU
        [DllImport("SSSE32.dll", EntryPoint = "ICC_Reader_pre_PowerOn")]
        public static extern int ICC_Reader_pre_PowerOn(int ReaderHandle, byte SLOT, byte[] Response);//上电 返回数据长度 失败小于0

        [DllImport("SSSE32.dll", EntryPoint = "ICC_Reader_Application")]
        public static extern int ICC_Reader_Application(int ReaderHandle, byte SLOT, int Lenth_of_Command_APDU, byte[] Command_APDU, byte[] Response_APDU);  //type a/b执行apdu命令 返回数据长度 失败小于0

        //非接CPU
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_PowerOnTypeA")]
        public static extern int PICC_Reader_PowerOnTypeA(int ReaderHandle, byte[] Response);//上电 返回数据长度 失败小于0

        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_Application")]
        public static extern int PICC_Reader_Application(int ReaderHandle, int Lenth_of_Command_APDU, byte[] Command_APDU, byte[] Response_APDU);  //type a/b执行apdu命令 返回数据长度 失败小于0


        //社保卡
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_SSCardInfo1")]
        public static extern int PICC_Reader_SSCardInfo1(int ReaderHandle, StringBuilder pSSCardID,
                                                                        StringBuilder pIDNum,StringBuilder pName, byte[] pSex, byte[] pBorn);

        //银行卡
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_CardInfo")]
        public static extern int PICC_Reader_CardInfo(int ReaderHandle, byte[] sn,
                                                                        byte[] date,
                                                                        byte[] kh,
                                                                        byte[] kh_len,
                                                                        int iType);


        [DllImport("SSSE32.dll", EntryPoint = "ICC_GetBankCardNo")]
        //int nType,byte[] bankCardNo,int[] sss
        public static extern int ICC_GetBankCardNo(int ReaderHandle, int nType,
                                                                byte[] bankCardNo,                                                     
                                                                byte[] kh_len);



        //身份证
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_ReadIDMsg")]
        public static extern int PICC_Reader_ReadIDMsg(int RHandle, StringBuilder pBmpFile, StringBuilder pName, StringBuilder pSex, StringBuilder pNation, StringBuilder pBirth, StringBuilder pAddress, StringBuilder pCertNo, StringBuilder pDepartment, StringBuilder pEffectData, StringBuilder pExpire, StringBuilder pErrMsg);


        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_Read_CardID")]
        public static extern int PICC_Reader_Read_CardID(int ReaderHandle, byte[] UID);//上电 返回数据长度 失败小于0

        [DllImport("SSSE32.dll", EntryPoint = "ICC_SelscetScan")]
        public static extern int ICC_SelscetScan(int ReaderHandle, byte[] pCodeInfo, ref int blent);//


        [DllImport("SSSE32.dll", EntryPoint = "ICC_StopSelscetScan")]
        public static extern int ICC_StopSelscetScan(int ReaderHandle);


        [DllImport("SSSE32.dll", EntryPoint = "iReadCardBas")]
        public static extern int iReadCardBas(int iType, byte[] pOutInfo);



        //15693
        //寻卡
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_Inventory",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int PICC_Reader_Inventory(int Rhandle, byte[] resp);

        //读卡
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_15693_Read",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int PICC_Reader_15693_Read(int Rhandle, byte blockAddr, byte[] resp);

        //写卡
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_15693_Write",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int PICC_Reader_15693_Write(int Rhandle, byte blockAddr, byte[] data, byte[] resp);

        //AFI
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_AFI",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int PICC_Reader_AFI(int Rhandle, byte[] data, byte[] resp);

        //DSFID
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_DSFID",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int PICC_Reader_DSFID(int Rhandle, byte[] data, byte[] resp);

        //卡片信息
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_SystemInfor",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int PICC_Reader_SystemInfor(int Rhandle, byte[] resp);

        //锁块
        [DllImport("SSSE32.dll", EntryPoint = "PICC_Reader_LockDataBlock",
        CallingConvention = CallingConvention.StdCall)]
        public static extern int PICC_Reader_LockDataBlock(int Rhandle, byte blockAddr, byte[] resp);
        
    }
}
