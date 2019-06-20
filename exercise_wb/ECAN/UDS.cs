using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECAN
{
    /*
     * 17/11/25 新版本. 软件分层, 并且做好封装. 包括以下几层:
     *      *功能层: 构建产品应用层之上的 比如Bootload刷新, (基本上就是应用层命令的序列)
     *      产品应用层, 产品相关的 dtc, did等设置, (以后这个要能实现基于配置动态配置)
     *      服务层, 实现 uds ISO229协议的所有协议. 重点
     *      网络层. 实现 4种帧的收发
     *      can收发层, 实现can报文的收发
     *      
     * */


    #region 应用层
    /*
     * 这里存放具体的产品/厂家相关的代码
     * 以后需要写一个接口层, 所有支持这个接口的 组件,比如诊断, 报文定义. 都要支持写入和加载配置.
     * */
    public abstract class UDS
    {
        public string Description; //ECU描述

        public ushort PhyID;  //物理请求ID
        public ushort FunID;  //功能请求ID
        public ushort RespID; //响应ID
        public byte FillByte;

        public DID[] dids; //产品的诊断相关
        public DTC[] dtcs;
        public RID[] rids;

        public UDSSA sa; //安全算法句柄
        
    }

    public class E100UDS : UDS//和e100相关的uds协议
    {
        public E100UDS()
        {
            Description = "五菱的E100电动车项目";
            PhyID = 0x727;
            FunID = 0x7DF;
            RespID = 0x72F;
            FillByte = 0x55;

            sa = new WulingSA(mask1: 0x51adc421, mask3: 0x32cfa321); //五菱的安全算法

            dtcs = new DTC[]
            {
                new DTC(0xC07300, "总线关闭"),
                new DTC(0x900016, "电压低于阈值"),
                new DTC(0x900017, "电压高于阈值"),
                new DTC(0xD90287, "$155报文丢失"),
                new DTC(0xD90281, "$155报文无效"),
                new DTC(0xD90487, "$36A报文丢失"),
                new DTC(0xD90481, "$36A报文无效"),
                new DTC(0xD90581, "$36A报文无效"),
                new DTC(0xE60087, "$120报文丢失"),
                new DTC(0xD40287, "$17D报文丢失"),
                new DTC(0xD40281, "$17D报文无效"),
                new DTC(0xD40987, "$34C报文丢失"),
                new DTC(0xD40981, "$34C报文无效"),
                new DTC(0xD41187, "$34E报文丢失"),
                new DTC(0xD41181, "$34E报文无效"),
                new DTC(0x100111, "位置传感器故障"),
                new DTC(0x100112, "电机开路故障"),
                new DTC(0x100113, "电机堵转故障"),
                new DTC(0x100114, "Eeprom故障"),
                new DTC(0xDD0087, "$230报文丢失"),
            };

            dids = new DID[]
            {
                new DID(0xF186, 1, "当前激活会话", new Signal[] {new SignalText("ADS", 0, 1)}),
                new DID(0xF18A, 7, "供应商编号", new Signal[] {new SignalText("SIDI", 0, 7)}),
                new DID(0xF18B, 4, "ECU生产日期", new Signal[] {new SignalBCD("ECUMD", 0, 4)}),
                new DID(0xF18C, 10, "ECU序列号", new Signal[] {new SignalText("ECUSN", 0, 10)}),
                new DID(0xF190, 17, "VIN码", new Signal[] {new SignalText("VIN", 0, 17)}),
                new DID(0xF192, 4, "供应商硬件号", new Signal[] {new SignalText("SEHN", 0, 4)}),
                new DID(0xF193, 4, "供应商硬件版本号", new Signal[] {new SignalText("SEHVN", 0, 4)}),
                new DID(0xF194, 4, "供应商软件号", new Signal[] {new SignalText("SESN", 0, 4)}),
                new DID(0xF195, 4, "供应商软件版本号", new Signal[] {new SignalText("SESVN", 0, 4)}),
                new DID(0xF1B2, 4, "车辆生产日期", new Signal[] {new SignalText("VMD", 0, 4)}),
                new DID(0xF1B9, 4, "高速CAN子网设置", new Signal[] {new SignalText("SCLHSC", 0, 4)}),
                new DID(0xF1CB, 4, "EndModelPartNumber", new Signal[] {new SignalText("EMPN", 0, 4)}),

                new DID(0xFD00, 1, "供电电压", new Signal[] {new SignalAnalog("EMPN", 0, 7, 8)}),
                new DID(0xFD01, 2, "车速", new Signal[] {new SignalAnalog("VS", 0, 7, 16)}),
                new DID(0xFD02, 2, "车发动机转速", new Signal[]{new SignalAnalog("VMAS", 0, 7, 16)}),
                new DID(0xFD03, 8, "标定数据", new Signal[]{
                                                        new SignalAnalog("P档位置", 0, 7, 16),
                                                        new SignalAnalog("非P档位置", 2, 7, 16),
                                                        new SignalAnalog("预留", 4, 7, 16),
                                                        new SignalAnalog("预留", 6, 7, 16),
                                                  }),
                new DID(0xFD04, 1, "位置传感器电压", new Signal[]{new SignalAnalog("PSV", 0, 7, 8)}),
                new DID(0xFD05, 1, "上次换档控制PWM", new Signal[]{new SignalAnalog("LTMCPD", 0, 7, 8)}),
                new DID(0xFD06, 1, "停车状态", new Signal[]{new SignalAnalog("PLS", 0, 7, 8)}),
                new DID(0xFD07, 2, "当前位置传感器值", new Signal[]{new SignalAnalog("RTPSV", 0, 7, 16)}),
                
                //2F控制
                new DID(0x2001, 1, "电机控制", new Signal[]{new SignalText("电机方向", 0, 1)}),
            };
        }

    }

    public class WulingSA : UDSSA //五菱的安全算法!
    {
        public const uint NC_DEFAULT_SEED = 0xa548fd85;

        uint[]  nc_uds_keymul = new uint[] {
            0x7678,0x9130,0xd753,0x750f,0x72cb,0x55f7,0x13da,0x786b,
            0x372a,0x4932,0x0e7c,0x3687,0x3261,0xa82c,0x8935,0xd00c,
            0x1995,0x4311,0xb854,0x0d8d,0x9863,0x1a21,0xf753,0xd6d3,
            0xb15d,0x7f3d,0x6821,0x791c,0x26c5,0x2e37,0x0e69,0x64a0 };

        public uint NC_UDS_KEYMASK_lv1;
        public uint NC_UDS_KEYMASK_lv3;
        
        public WulingSA(uint mask1, uint mask3)
        {
            this.NC_UDS_KEYMASK_lv1 = mask1;
            this.NC_UDS_KEYMASK_lv3 = mask3;
        }
        uint croleft(uint c, byte b)
        {
            uint left, right, croleftvalue;
            left = c << b;
            right = c >> (32 - b);
            croleftvalue = left | right;
            return croleftvalue;
        }
        ushort croshortright(ushort c, ushort b)
        {
            ushort right, left, crorightvalue;
            right = (ushort)(c >> b);
            left = (ushort)(c << (16 - b));
            crorightvalue = (ushort)(left | right);
            return crorightvalue;
        }
        uint mulu32(uint val1, uint val2, uint NC_UDS_KEYMASK)
        {
            uint x, y, z, p;

            x = (val1 & NC_UDS_KEYMASK) | ((~val1) & val2);
            y = ((croleft(val1, 1)) & (croleft(val2, 14))) | ((croleft(NC_UDS_KEYMASK, 21)) & (~(croleft(val1, 30))));
            z = (croleft(val1, 17)) ^ (croleft(val2, 4)) ^ (croleft(NC_UDS_KEYMASK, 11));
            p = x ^ y ^ z;
            return p;
        }
        uint uds_calc_key(uint SeedIn, uint NC_UDS_KEYMASK)
        {
            uint temp;
            ushort index;
            ushort mult1;
            ushort mult2;

            if (SeedIn == 0)
            {
                SeedIn = NC_DEFAULT_SEED;
            }
            index = 0x5D39;
            temp = 0x80000000;
            for (; Convert.ToBoolean(temp); temp >>= 1)
            {
                if (Convert.ToBoolean(temp & SeedIn))
                {
                    index = croshortright(index, 1);
                    if (Convert.ToBoolean(temp & NC_UDS_KEYMASK))
                    {
                        index ^= 0x74c9;
                    }
                }
            }
            mult1 = (ushort)((nc_uds_keymul[(index >> 2) & ((1 << 5) - 1)] ^ index));
            mult2 = (ushort)((nc_uds_keymul[(index >> 8) & ((1 << 5) - 1)] ^ index));
            temp = (((uint)mult1) << 16) | ((uint)mult2);
            temp = mulu32(SeedIn, temp, NC_UDS_KEYMASK);
            return temp;
        }
        public override uint CalcKey(uint seed, Level level)
        {
            if (level == Level.Level1)
                return uds_calc_key(seed, NC_UDS_KEYMASK_lv1);
            else if (level == Level.Level3)
                return uds_calc_key(seed, NC_UDS_KEYMASK_lv3);
            else
                throw new NotImplementedException("不存在这个level的算法");
        }
    }

    public class ZhongtaiSA : UDSSA
    {
        const uint UNLOCKKEY = 0x00000000;
        const uint UNLOCKSEED = 0x00000000;
        const uint UNDEFINESEED = 0xFFFFFFFF;
        const uint SEEDMASK = 0x80000000;
        const int SHIFTBIT = 1;

        public uint ALGORITHMASK_LVL1;
        public uint ALGORITHMASK_LVL3;
        public ZhongtaiSA(uint masklvl1, uint masklvl3)
        {
            this.ALGORITHMASK_LVL1 = masklvl1;
            this.ALGORITHMASK_LVL3 = masklvl3;
        }
        public uint uds_calc_key(uint seed, uint mask)
        {
            uint key = UNLOCKKEY;
            if (!((seed == UNLOCKSEED) || (seed == UNDEFINESEED)))
            {
                for (int i = 0; i < 35; i++)
                {
                    if ((seed & mask) != 0)
                    {
                        seed <<= SHIFTBIT;
                        seed ^= mask;
                    }
                    else
                    {
                        seed <<= SHIFTBIT;
                    }
                }
                key = seed;
            }
            return key;
        }
        public override uint CalcKey(uint seed, Level level)
        {
            if (level == Level.Level1)
                return uds_calc_key(seed, ALGORITHMASK_LVL1);
            else if (level == Level.Level3)
                return uds_calc_key(seed, ALGORITHMASK_LVL3);
            else
                throw new NotImplementedException("不存在这个level的算法");
        }
    }
    #endregion

    #region 服务层
    /*
     * 相比较原先的程序, 主要重写的是服务层, 所有的dtc, did等,变成具体的类, 解析要具有层次结构
     * */

    public enum UDSInd
    {
        NotDone,    //还未完成
        Positive,   //肯定回复
        Negative,   //否定回复
        Error,      //包括超时无回复, 和其他不可知结果.
    }
    public enum NRCCode //错误码
    {
        /*
         * 对于NRC 觉得没有必要作为类实体, 而简单采用枚举值来表示.
         * */
        NRC_11 = 0x11,  //服务不支持
        NRC_12 = 0x12,  //不支持子功能
        NRC_13 = 0x13,  //不正确的消息长度或无效的格式
        NRC_21 = 0x21,  //重复请求忙
        NRC_22 = 0x22,  //条件不正确
        NRC_24 = 0x24,  //请求序列错误
        NRC_25 = 0x25,  //子网节点无应答
        NRC_26 = 0x26,  //故障阻值请求工作执行
        NRC_31 = 0x31,  //请求超出范围
        NRC_33 = 0x33,  //安全访问拒绝
        NRC_35 = 0x35,  //密钥无效
        NRC_36 = 0x36,  //超出尝试次数
        NRC_37 = 0x37,  //所需时间延迟未到
        NRC_70 = 0x70,  //不允许上传下载
        NRC_71 = 0x71,  //数据传输暂停
        NRC_72 = 0x72,  //一般编程失败
        NRC_73 = 0x73,  //错误的数据块序列计数器
        NRC_78 = 0x78,  //正确接收请求消息-等待响应
        NRC_7E = 0x7E,  //激活会话不支持该子服务
        NRC_7F = 0x7F,  //激活会话不支持该服务
        NRC_92 = 0x92,  //电压过高
        NRC_93 = 0x93,  //电压过低
    }
    public abstract class Signal
    {
        /* 信号解析, 应包括:
         * Description: 描述
         * SignalType: 信号类型
         * StartByte, Startbit, length
         * 以及更详细的信号解析
         * */
        public string Description { get; set; }
        public abstract void Pollute(byte[] data, dynamic sig); //写回数据
        public abstract dynamic Parse(byte[] data); //提取数据
    }

    public class SignalBool : Signal //简单的bool信号量
    {
        public int StartByte;
        public int Startbit;
        public SignalBool(string desc, int StartByte, int Startbit)
        {
            this.Description = desc;
            this.StartByte = StartByte;
            this.Startbit = Startbit;
        }
        public override dynamic Parse(byte[] data) //忽略对于长度的判断
        {
            return Convert.ToBoolean(data[StartByte] & (1 << Startbit));
        }
        public override void Pollute(byte[] data, dynamic sig)
        {
            if (sig.GetType() != typeof(Boolean))
                throw new Exception("提供的值必须为布尔类型");
            if (sig)
            {
                data[StartByte] = (byte)(data[StartByte] | (1 << Startbit)); //置1
            }
            else
            {
                data[StartByte] = (byte)(data[StartByte] & ~(1 << Startbit)); //置0
            }
        }
    }

    public class SignalText : Signal //字符串信号量 用ascii来表示
    {
        public int StartByte;
        public int ByteLength;
        public SignalText(string desc, int StartByte, int ByteLength)
        {
            this.Description = desc;
            this.StartByte = StartByte;
            this.ByteLength = ByteLength;
        }
        public override dynamic Parse(byte[] data)
        {
            // 将byte* 以string的方式输出出来
            StringBuilder sb = new StringBuilder(ByteLength);
            foreach (byte b in data.Skip(StartByte).Take(ByteLength))
                sb.Append((char)b);
            return sb.ToString();
        }

        public override void Pollute(byte[] data, dynamic sig)
        {
            for (int i = 0; i < ByteLength; i++)
                data[StartByte + i] = (byte)sig[i];
        }
    }

    public class SignalBCD : Signal //用BCD码来表示信号量, 一般用在日期上
    {
        public int StartByte;
        public int ByteLength;
        public SignalBCD(string desc, int StartByte, int ByteLength)
        {
            this.Description = desc;
            this.StartByte = StartByte;
            this.ByteLength = ByteLength;
        }
        public override void Pollute(byte[] data, dynamic sig)
        {
            throw new NotImplementedException();
        }
        public override dynamic Parse(byte[] data)
        {
            throw new NotImplementedException();
        }
    }

    public class SignalAnalog : Signal //模拟量, 简单值, 只支持uint以下!!
    {
        public int StartByte;
        public int StartBit;
        public int BitLength;
        public SignalAnalog(string desc, int StartByte, int StartBit, int BitLength)
        {
            this.Description = desc;
            this.StartByte = StartByte;
            this.StartBit = StartBit;
            this.BitLength = BitLength;
        }
        public override dynamic Parse(byte[] data) 
        {
            // 简单analog值 只支持 byte, ushort, uint 三种
            return MyConvert.GetDynamicData(data, StartByte, StartBit, BitLength); //根据位长度动态调节
        }
        public override void Pollute(byte[] data, dynamic sig)
        {
            //首先把sig缩放到对应的位数, 然后逐位操作. 注意data是按照大头排布的!!
            //思路是 把sig从最低位开始放到motluola的最高位
            int tmpBit = (BitLength -1) % 8; //位数为4位, 相当于右移3位..
            int lsbByte; 
            int lsbBit;
            if (tmpBit <= StartBit) //
            {
                lsbByte = StartByte;
                lsbBit = StartBit - BitLength;
            }
            else
            {
                lsbByte = StartByte + (BitLength - 1) / 8 + 1;
                lsbBit = StartBit - BitLength + 8;
            }
            int byteIdx, bitIdx;
            for (int i=0; i< BitLength; i++)
            {
                _calcShift(lsbByte, lsbBit, i, out byteIdx, out bitIdx);
                if (Convert.ToBoolean(sig & 1))
                    data[byteIdx] = (byte)(data[byteIdx] | (1 << bitIdx));
                else
                    data[byteIdx] = (byte)(data[byteIdx] & ~(1 << bitIdx));
            }
        }
        public static void _calcShift(int lsbByte, int lsbBit, int bit, out int byteIdx, out int bitIdx)
        {
            byteIdx = lsbByte - (lsbBit + bit) / 8;
            bitIdx = (lsbBit + bit) % 8;
        }

    }

    //public class SignalAnalogScale : Signal //带缩放, y = ax +b
    //public class SignalStateEncode : Signal //带枚举值

    public class DID
    {
        /*
         * 针对DID, 仿照vs的设计, 包含以下元素:
         *      Display Name: 显示名
         *      DataID: did
         *      Description: 描述
         *      
         *      Security: 为了这个id访问, 需要的安全码(其实这个也没必要放在did之中, 算是一个功能)
         *      Signals: 信号解析
         *      
         * */

        public string DisplayName;
        public ushort DataID;

        public uint SpecificLength; //要求返回的字符长度
        public Signal[] signals; //包含的信号解析

        public DID(ushort dataID, uint spcLength, string displayName, Signal[] signals)
        {
            this.DisplayName = displayName;
            this.DataID = dataID;
            this.SpecificLength = spcLength;
            this.signals = signals;
        }
    }
    public class RID
    {
        /*
         * RoutineID, 针对31服务, 在uds协议中, did和rid没分开, 但是来往数据不同
         * 包含以下元素:
         *      Display Name: 显示名
         *      DataID: rid
         *      Description: 描述
         *      
         *      Security: 只有对于控制的要求
         *      
         *      Request的信号解析
         *      Response的信号解析
         * */
    }

    public class DTC
    {
        /*
         * 包含以下元素:
         *      Description: 描述
         *      DTC Value: 3字节的dtc_id
         *      Long Description
         * */
        public string description;
        public uint dtcID;
        //public string longDescription;
        
        public DTC(uint dtc, string desc)
        {
            this.description = desc;
            this.dtcID = dtc;
            //this.longDescription = longDesc;
        }
    }

    public abstract class UDSSA
    {
        /*
         * 不同于SA文件中的加载dll的方式, 这里用软件的方式 直接暴露预先已经生成的几家厂家的算法
         * */
        public enum Level
        {
            Level1, // 01/02
            Level2, // 03/04, 09/0A
            Level3, // 05/06
        }
        public abstract uint CalcKey(uint seed, Level level);
    }

    public abstract class UDSService
    {
        /*
         * ISO90229 协议层, uds协议中的服务实体
         * */
        public enum ServiceType //支持的
        {
            DiagnosticSessionControl        = 0x10, //会话控制
            ECUReset                        = 0x11, //ECU复位
            SecurityAccess                  = 0x27, //安全访问
            CommunicationControl            = 0x28, //通讯控制
            TesterPresent                   = 0x3E, //诊断仪在线
            ControlDTCSetting               = 0x85, //DTC控制
            ReadDataByIdentifier            = 0x22, //读数据
            ReadMemoryByAddress             = 0x23, //通过内存读数据
            ReadScalingDataByIdentifier     = 0x24, //通过标识符读缩放数据
            ReadDataByPeriodicIdentifier    = 0x2A, //通过周期标识符读数据,
            DynamicallyDefineDataIdentifier = 0x2C, //动态定义数据标识符
            WriteDataByIdentifier           = 0x2E, //通过标识符写数据
            WriteMemoryByAddress            = 0x3D, //通过地址写内存
            ClearDiagnosticInformation      = 0x14, //清除故障码
            ReadDTCInformation              = 0x19, //读取DTC码
            InputOutputControlByIdentifier  = 0x2F, //通过标识符进行控制
            RoutineControl                  = 0x31, //远程控制
            RequestDownload                 = 0x34, //请求下载
            RequestUpload                   = 0x35, //请求上传
            TransferData                    = 0x36, //传输数据
            RequestTransferExit             = 0x37, //请求退出传输
        }

        public bool reply = true;               //此命令是否需要回复
        public ServiceType serviceType;         //服务号
        public byte requestID;
        public byte responseID;

        public bool suppressPosResMsg = false;  //抑制接收
        public int waitAfterReply = default(int); //默认发送后等待的时间

        public List<byte> ServiceData;
        public List<byte> ReplyData;

        public UDSService(ServiceType type)
        {
            this.serviceType = type;
            requestID = (byte)serviceType;
            responseID = (byte)(0x40 + requestID);
        }

        public virtual void ParseData()
        {

        }

    }

    #endregion

    #region 网络层
    /*网络层协议具有以下功能：
        a）一次可发送或接收最多4095个数据字节；
        b）报告发送或接收完成（或失败）。

     * 网络层应该有能力发送一个 N_PDU, 是一个节点的网络层能与另一个对等协议实体之间传递数据.
     * */



    #endregion

}
