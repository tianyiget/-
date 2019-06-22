using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;
using Dongzr.MidiLite;
using System.Threading;
using System.Collections;

/*这个类负责处理usbcan设备的封装, 提供硬件层的封装  基于Ecan(ECanVci.dll 使用手册V5.1)*/

namespace ECAN
{
    [Flags]
    public enum ECANStatus : uint
    {
        STATUS_ERR = 0x00,
        STATUS_OK  = 0x01,
    }

    [Flags]
    public enum ECANErrorCode : uint
    {
        ERR_CAN_OVERFLOW      = 0x00000001, //CAN控制器内部FIFO溢出
        ERR_CAN_ERRALARM      = 0x00000002, //CAN控制器错误报警
        ERR_CAN_PASSIVE       = 0x00000004, //CAN控制器消极错误
        ERR_CAN_LOSE          = 0x00000008, //CAN控制器仲裁丢失
        ERR_CAN_BUSERR        = 0x00000010, //CAN控制器总线错误
        ERR_CAN_REG_FULL      = 0x00000020, //CAN接收寄存器满
        ERR_CAN_REC_OVER      = 0x00000040, //CAN接收寄存器溢出
        ERR_CAN_ACTIVE        = 0x00000080, //CAN控制器主动错误
        ERR_DEVICEOPENED      = 0x00000100, //设备已经打开
        ERR_DEVICEOPEN        = 0x00000200, //打开设备错误
        ERR_DEVICENOTOPEN     = 0x00000400, //设备没有打开
        ERR_BUFFEROVERFLOW    = 0x00000800, //缓冲区溢出
        ERR_DEVICENOTEXIST    = 0x00001000, //此设备不存在
        ERR_LOADKERNELDLL     = 0x00002000, //装载动态库失败
        ERR_CMDFAILED         = 0x00004000, //执行命令失败
        ERR_BUFFERCREATE      = 0x00008000, //内存不足
        ERR_CANETE_PORTOPENED = 0x00010000, //端口已经被打开
        ERR_CANETE_INDEXUSED  = 0x00020000, //设备索引号已经被占用
    }
    
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct CAN_OBJ
    {
        public UInt32 ID;              //报文ID                
        public UInt32 TimeStamp;       //接收到信息帧的时间标识, 从CAN控制器初始化开始计时
        public Byte TimeFlag;        //是否使用时间标识, 为1有效, 只在为接受帧时有意义
        public Byte SendType;        //发送帧类型, =0为正常发送, =1为单次发送, =2为自发自收 =3为单次自发自收 只在发送帧有效
        public Byte RemoteFlag;      //是否是远程帧
        public Byte ExternFlag;      //是否是扩展帧
        public Byte DataLen;         //数据长度(<=8), 即Data的长度
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public Byte[] data;          //报文数据
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public Byte[] Reserved;      //系统保留

        public CAN_OBJ(uint m_id, byte[] m_data , byte len=8, bool newData=false) //提供一个简单的构造
        {
            ID = m_id;
            TimeStamp = TimeFlag = 0; //无用
            SendType = 0; //默认正常发送, 只有在发送时有用.
            RemoteFlag = ExternFlag = 0;
            DataLen = len;
            if (newData) //自己可以选择每次重新生成一个8byte字节,还是复用.
            {
                data = new byte[8]; //注意这里不能 new byte[len] 结构体规定定长8
                m_data.CopyTo(data, 0);
            }
            else
                data = m_data;
            //Reserved = new byte[3];
            Reserved = null; //Reserved 这个字段无用
        }
    }

    public struct CAN_OBJ_ARRAY
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public CAN_OBJ[] array;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ERR_INFO
    {
        public ECANErrorCode ErrCode; //带枚举的错误码
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Passive_ErrData; //当产生的错误中有消极错误时表示为消极错误的错误标识数据。
        public byte ArLost_ErrData; //当产生的错误中有仲裁丢失错误时表示为仲裁丢失错误的错误标识数据。
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BOARD_INFO
    {
        public ushort hw_Version;     //硬件版本号，用16进制表示。比如0x0100表示V1.00。
        public ushort fw_Version;     //固件版本号，用16进制表示。
        public ushort dr_Version;     //驱动程序版本号，用16进制表示。
        public ushort in_Version;     //接口库版本号，用16进制表示。
        public ushort irq_Num;        //板卡所使用的中断号。
        public byte can_Num;          //表示有几路CAN通道。
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] str_Serial_Num; //此板卡的序列号。
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] str_hw_Type;    //硬件类型，比如“USBCAN V1.00”（注意：包括字符串结束符’\0’）。
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] Reserved;     //系统保留。

        //public override string ToString() //当byte[]里面有 \0的时候, 打印string会出错.
        //{
        //    string s =
        //        $"硬件版本号: {hw_Version} \t固件版本号: {fw_Version} \t驱动版本号: {dr_Version} \t接口库版本号: {in_Version} \tCAN通道数量: {can_Num}\n" +
        //        $"板卡序列号: {Encoding.ASCII.GetString(str_Serial_Num)}" +
        //        $"硬件类型: {Encoding.ASCII.GetString(str_hw_Type)}";
        //    return s;
        //}
    }

    public struct CAN_STATUS
    {
        public byte ErrInterrupt; //中断记录，读操作会清除。
        public byte regMode;      //CAN控制器模式寄存器。
        public byte regStatus;    //CAN控制器状态寄存器。
        public byte regALCapture; //CAN控制器仲裁丢失寄存器。
        public byte regECCapture; //CAN控制器错误寄存器。
        public byte regEWLimit;   //CAN控制器错误警告限制寄存器。
        public byte regRECounter; //CAN控制器接收错误寄存器。
        public byte regTECounter; //CAN控制器发送错误寄存器。
        public uint Reserved;     //系统保留。
    }

    public struct INIT_CONFIG
    {
        public uint AccCode;
        public uint AccMask;
        public uint Reserved;
        public byte Filter;
        public byte Timing0;
        public byte Timing1;
        public byte Mode;
    }
    
    public struct FILTER_RECORD
    {
        public uint ExtFrame; //过滤的帧类型标志，为1代表要过滤的为扩展帧，为0代表要过滤的为标准帧。
        public uint Start;    //滤波范围的起始帧ID
        public uint End;      //滤波范围的结束帧ID
    }

    public static class ECANDLL
    {
        public const uint ERROR_RES = 0xFFFFFFFF;

        [DllImport("ECANVCI.dll", EntryPoint = "OpenDevice")] //此函数用以打开设备。
        public static extern ECANStatus OpenDevice(
            UInt32 DeviceType,
            UInt32 DeviceInd,
            UInt32 Reserved);

        [DllImport("ECANVCI.dll", EntryPoint = "CloseDevice")] //此函数用以关闭设备。
        public static extern ECANStatus CloseDevice(
            UInt32 DeviceType,
            UInt32 DeviceInd);


        [DllImport("ECANVCI.dll", EntryPoint = "InitCAN")] //此函数用以初始化指定的CAN。
        public static extern ECANStatus InitCAN(
            UInt32 DeviceType,
            UInt32 DeviceInd,
            UInt32 CANInd,
            ref INIT_CONFIG InitConfig);

        [DllImport("ECANVCI.dll", EntryPoint = "ReadBoardInfo")] //此函数用以获取设备信息。
        public static extern ECANStatus ReadBoardInfo(
            UInt32 DevType,
            UInt32 DevIndex,
            ref BOARD_INFO BoardInfo
            );
        
        [DllImport("ECANVCI.dll", EntryPoint = "ReadErrInfo")] //此函数用以获取最后一次错误信息。
        public static extern ECANStatus ReadErrInfo(
            UInt32 DeviceType,
            UInt32 DeviceInd,
            UInt32 CANInd,
            out ERR_INFO ReadErrInfo);


        [DllImport("ECANVCI.dll", EntryPoint = "ReadCanStatus")] //此函数用以获取CAN状态。
        public static extern ECANStatus ReadCanStatus(
            UInt32 DeviceType,
            UInt32 DeviceInd,
            UInt32 CANInd,
            out CAN_STATUS ReadCanStatus);

        //[DllImport("ECANVCI.dll", EntryPoint = "GetReference")] //此函数用以获取设备的相应参数。
        //[DllImport("ECANVCI.dll", EntryPoint = "SetReference")] //此函数用以设置设备的相应参数，主要处理不同设备的特定操作。

        [DllImport("ECANVCI.dll", EntryPoint = "GetReceiveNum")] //此函数用以获取指定接收缓冲区中接收到但尚未被读取的帧数。
        public static extern UInt64 GetReceiveNum(
            UInt32 DeviceType, 
            UInt32 DeviceInd, 
            UInt32 CANInd);

        [DllImport("ECANVCI.dll", EntryPoint = "ClearBuffer")] //此函数用以清空指定缓冲区。
        public static extern ECANStatus ClearBuffer(
            UInt32 DeviceType,
            UInt32 DeviceInd,
            UInt32 CANInd);

        [DllImport("ECANVCI.dll", EntryPoint = "StartCAN")] //此函数用以启动CAN。
        public static extern ECANStatus StartCAN(
            UInt32 DeviceType,
            UInt32 DeviceInd,
            UInt32 CANInd);


        [DllImport("ECANVCI.dll", EntryPoint = "Transmit")] //返回实际发送的帧数。单帧发送
        public static extern UInt32 Transmit(
            UInt32 DeviceType,
            UInt32 DeviceInd,
            UInt32 CANInd,
            ref CAN_OBJ Send,
            UInt32 length);


        [DllImport("ECANVCI.dll", EntryPoint = "Transmit")] //返回实际发送的帧数。多帧连续发送
        public static extern UInt32 Transmit_array(
            UInt32 DeviceType,
            UInt32 DeviceInd,
            UInt32 CANInd,
            ref CAN_OBJ_ARRAY send,
            UInt32 length);

        [DllImport("ECANVCI.dll", EntryPoint = "Receive")] //返回实际读取到的帧数。如果返回值为0xFFFFFFFF，则表示读取数据失败，有错误发生，请调用ReadErrInfo函数来获取错误码。
        public static extern UInt32 Receive(
            UInt32 DeviceType,
            UInt32 DeviceInd,
            UInt32 CANInd,
            out CAN_OBJ Receive,
            UInt32 length,
            UInt32 WaitTime);

        [DllImport("ECANVCI.dll", EntryPoint = "Receive")] // 返回实际读取到的帧数。如果返回值为0xFFFFFFFF，则表示读取数据失败，有错误发生，请调用ReadErrInfo函数来获取错误码。
        public static extern UInt32 Receive_array(
            UInt32 DeviceType,
            UInt32 DeviceInd,
            UInt32 CANInd,
            out CAN_OBJ_ARRAY Receive,
            UInt32 length,
            UInt32 WaitTime);

        [DllImport("ECANVCI.dll", EntryPoint = "ResetCAN")]
        public static extern ECANStatus ResetCAN(
        UInt32 DeviceType,
        UInt32 DeviceInd,
        UInt32 CANInd);

    }
    
    public class CanProc : IDisposable //发送类
    {
        public class Message //对于发送的数据, 只保留最基本的参数     
        {
            public uint CanID;
            public byte Length;
            public byte[] Data;
            public int Period;
            public bool needSend;
            int count; //用来统计多少次周期
            public CAN_OBJ ToCanObj()
            {
                return new CAN_OBJ(CanID, Data, Length);
            }
            public Message(uint canid, byte length, byte[] data, int period, bool needSend)
            {
                this.CanID = canid;
                this.Length = length;
                this.Data = data;
                this.Period = period;
                this.needSend = needSend;
                count = 0;
            }
            public bool checkSend(int p = TickCycle)
            {
                if (!needSend)
                    return false;

                count += p;
                if (count >= this.Period)
                {
                    count = 0;
                    return true;
                }
                else
                    return false;
            }
        }

        [Flags]
        public enum CanPort //只要有一个还连接就断定已经open
        {
            None = 0x00,
            Can1 = 0x01,
            Can2 = 0x02,
            All = Can1 | Can2,
        }
        
        //变量定义  
        public static CanPort connect = CanPort.None;
        CanPort can;
        const byte device = 3;                                         //设备: usbcan2=4 USBCAN1=3
        byte deviceInd;
        public uint canIndex;                                          //多个类可以共用一个connect实例 为了避免写很多分开的代码
        const byte TickCycle = 10;                                      //刷新周期, 最小的
        const byte BufSize = 10;                                      //一次发送或接收最大值, 根据CAN_OBJ_ARRAY定义

        List<CAN_OBJ> _buf= new List<CAN_OBJ>(); //供批量读或写使用的临时缓存

        public MyQueue<CAN_OBJ> TxFramesBuf = new MyQueue<CAN_OBJ>();    //发送报文 buf 发送共用同一个,其实要保证入队的原子性
        public Dictionary<uint, MyQueue<CAN_OBJ>> RxFramesBuf = new Dictionary<uint, MyQueue<CAN_OBJ>>();
        public Dictionary<string, Message> periodMessage = new Dictionary<string, Message>();
        MmTimer timer;
        bool isRunning = false; //是否正在运行
        
        
        //供调试使用
        public BOARD_INFO board_info = new BOARD_INFO();
        public ERR_INFO errinfo;

        public CanProc(byte deviceInd = 0, CanPort can = CanPort.Can1)
        {
            if (connect.HasFlag(can)) //已经连接
            {
                throw new Exception("该CAN口已被占用");
            }
            timer = TimerSetter.Setter(TickCycle, timer_tick); //绑定定时器
            this.deviceInd = deviceInd; //以后电脑上可能有多个usbcan设备
            canIndex = can == CanPort.Can1? (uint)0x00: (uint)0x01;
            this.can = can;
        }

        public void AddBuf(uint i) //监控某一个报文
        {
            if (!RxFramesBuf.ContainsKey(i))
                RxFramesBuf[i] = new MyQueue<CAN_OBJ>();
        }
        
        public bool Open()
        {
            if (connect == CanPort.None) //之前从没连接过
            {
                // 打开设备
                if (ECANDLL.OpenDevice(device, deviceInd, 0) != ECANStatus.STATUS_OK)
                    return false;
            }
            if (!SetConnect())
            {
                ECANDLL.CloseDevice(device, deviceInd);
                return false;
            }

            connect |= can; //我已连接
            Thread.Sleep(200); //给初始化准备一点时间.
            timer.Start();    //直接开始
            isRunning = true;
            return true;
        }

        public void Close() //如果还有其他用例, 则不关闭
        {
            timer.Stop();   //关闭
            connect -= can;
            if (connect == CanPort.None) //如果全部没有连接了, 由这个例子去关闭
                ECANDLL.CloseDevice(device, deviceInd);
            isRunning = false;
        }

        bool SetConnect() //不屏蔽任何报文
        {
            INIT_CONFIG init_config = new INIT_CONFIG();

            //统一不采用屏蔽策略, 交给软件
            init_config.AccCode = 0;            //验收码
            init_config.AccMask = 0xFFFFFF;     //屏蔽码
            init_config.Filter = 0;             //滤波方式

            init_config.Timing0 = 0;            //固定是500速率  
            init_config.Timing1 = 0x1C;
            init_config.Mode = 0;

            //初始化设备
            if (ECANDLL.InitCAN(device, deviceInd, canIndex, ref init_config) != ECANStatus.STATUS_OK)
            {
                return false;
            }

            //开始读写
            if (ECANDLL.StartCAN(device, deviceInd, canIndex) != ECANStatus.STATUS_OK)
            {
                return false;
            }

            ECANDLL.ClearBuffer(device, deviceInd, canIndex); //清除当前缓存

            //读取这个usb版本信息.
            ECANDLL.ReadBoardInfo(device, deviceInd, ref board_info); //获取board数据
            return true;
        }
        
        public void timer_tick(object sender, EventArgs e)
        {
            PeriodSend();
            //SendFrame();
            SendBatchFrame(); //批量发送
            //ReceiveFrame();
            ReceiveBatchFrame(); //批量接收
        }

        public void PeriodSend()
        {
            // 定时检查, 如果需要发送, 则放到发送帧里面
            foreach (string id in periodMessage.Keys)
            {
                if (periodMessage[id].checkSend())
                {
                    TxFramesBuf.Enqueue(periodMessage[id].ToCanObj());
                }
            }
        }
        public void SendBatchFrame() //批量发送
        {
            _buf.Clear();
            int sCount = TxFramesBuf.BatchDequeue(_buf);
            if (sCount > 0)
            {
                CAN_OBJ_ARRAY _ARRAY = new CAN_OBJ_ARRAY();
                _ARRAY.array = new CAN_OBJ[BufSize];
                int sendCNT; //一次发送的个数
                for (int start = 0; start < sCount; start += BufSize) //分批发送
                {
                    sendCNT = Math.Min(BufSize, sCount - start);
                    _buf.CopyTo(start, _ARRAY.array, 0, sendCNT);
                    if (ECANDLL.Transmit_array(device, deviceInd, canIndex, ref _ARRAY, (uint)sendCNT) == ECANDLL.ERROR_RES)
                    {
                        CheckError();
                    }

                }
            }
        }
        public void SendFrame() //发送帧 
        {
            int sCount = 0;
            while (!TxFramesBuf.IsEmpty)
            {
                CAN_OBJ frame = TxFramesBuf.Dequeue();
                if (ECANDLL.Transmit(device, deviceInd, canIndex, ref frame, length: 1) == ECANDLL.ERROR_RES) //返回0xFFFFFFFF表示发送失败.
                {
                    CheckError();
                }

                if (++sCount > 200)
                    break;
            }
        }

        public void ReceiveBatchFrame() //批量读
        {
            CAN_OBJ_ARRAY _ARRAY;
            uint sCount;
            do
            {
                sCount = ECANDLL.Receive_array(device, deviceInd, canIndex, out _ARRAY, BufSize, 0);
                if (sCount == ECANDLL.ERROR_RES)
                    CheckError();
                else
                {
                    for(int i=0; i < sCount; i++)
                    {
                        uint id = _ARRAY.array[i].ID;
                        if (RxFramesBuf.ContainsKey(id)) //只关心订阅的报文
                            RxFramesBuf[id].Enqueue(_ARRAY.array[i]);
                    }
                }
            } while (sCount == BufSize); //表示可能还有剩余数据
        }

        public void ReceiveFrame() //接收帧
        {
            int sCount = 0;
            CAN_OBJ frame;
            while (ECANDLL.Receive(device, deviceInd, canIndex, out frame, length: 1, WaitTime: 1) == 0x01)
            {
                //除了硬件过滤, 还需要软件过滤!
                if (RxFramesBuf.ContainsKey(frame.ID)) //短路判断
                {
                    RxFramesBuf[frame.ID].Enqueue(frame);
                }
                if (++sCount > 200)
                    break;
            }
        }
        public void CheckError()
        {
            // 调用 lasterror
            ECANDLL.ReadErrInfo(device, deviceInd, canIndex, out errinfo); //读取出最后一次error
            // 如果是那种无法恢复的错误, 应该直接throw exception fixme
            // 如果实验停不下来, 想办法要写下日志.
        }

        public void Dispose()
        {
            if (isRunning)
                Close();
            timer.Dispose();
        }
    }

    #region 辅助函数
    class TimerSetter
    {
        public static MmTimer Setter(int Interval, EventHandler tick)
        {
            MmTimer timer = new MmTimer();
            timer.Mode = MmTimerMode.Periodic;
            timer.Interval = Interval;
            timer.Tick += tick;
            return timer;
        }
    }

    public static class Extension //用来给提供一个拓展
    {
        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue))
        {
            return dict.ContainsKey(key) ? dict[key] : defaultValue;
        }
    }
    #endregion
    public class MyQueue<T>
    {
        /* 自己实现的经常要用到的队列 
         * 基本功能:
         *      线程安全
         *      可以入队和出队
         *      判断是否为空
         *      可以清空
         *      (可以防止无限增加)
         * 
         * 实现方法:
         *      1: 默认的queue实现不是线程安全的, 并且有默认的 resize操作, 会出现空白数据
         *      2: 使用线程安全的 concurrentqueue可以实现, 但是因为用的是链表实现, 没有clear, 不好处理无线增大情况.
         *      3: 使用原生的array. 缺点是大小只能一致. 优势是内存开销一致, 且自己可以实现循环列表来处理clear问题
        * */
        const int MAXLENGTH = 10000; //默认buf大小
        T[] _queue = new T[MAXLENGTH]; //buf池
        int _head, _tail;

        public bool IsEmpty => _tail == _head;

        public void Enqueue(T item)
        {
            _queue[_head++] = item;
            _head %= MAXLENGTH;
        }

        public T Dequeue()
        {
            T item = _queue[_tail++];
            _tail %= MAXLENGTH;
            return item;
        }

        public void Clear()
        {
            _tail = _head; //清空只是简单的把指针指向head即可
        }

        public int BatchDequeue(List<T> buf, int max = MAXLENGTH)
        {
            int cnt = 0;
            while(!IsEmpty)
            {
                buf.Add(Dequeue());
                cnt++;
            }
            return cnt;
        }
    }

    class MyConvert
    {
        /*
         * 注意字节序:
         * Motorola的PowerPC系列CPU采用Big Endian方式存储数据
         * Intel的x86系列CPU采用Little Endian方式存储数据
         * can是大头. pc是小头
         * */
         ///
        //统一采用五菱的(sB, sb, len)三元组
        public static byte GetByte(byte[] data, int startByte, int startBit, int len = 1) //省略len则表示一位
        {
            return Convert.ToByte(data[startByte] >> (startBit - len + 1) & 0xff >> (8 - len));
        }
        public static ushort GetUshort(byte[] data, int startByte, int startBit, int len)
        {
            //不允许跨字节
            int tmp = MyConvert.ToUshort(data, startByte);
            return Convert.ToUInt16(tmp >> (startBit - len + 9) & 0xffff >> (16 - len));
        }
        public static uint GetUint(byte[] data, int startByte, int startBit, int len = 32)
        {
            return MyConvert.ToUInt32(data, startByte); //暂时不考虑不是32位的情况
        }

        public static dynamic GetDynamicData(byte[] data, int StartByte, int StartBit, int BitLength)
        {
            if (BitLength <= 8)
                return MyConvert.GetByte(data, StartByte, StartBit, BitLength);
            else if (BitLength <= 16)
                return MyConvert.GetUshort(data, StartByte, StartBit, BitLength);
            else if (BitLength <= 32)
                return MyConvert.GetUint(data, StartByte, StartBit, BitLength);
            else
                throw new NotImplementedException("不支持这么大长度");
        }

        public static string ToHexString(byte[] data, int DataLen = 8)
        {
            return string.Join(" ", data.Take(DataLen).Select(d => d.ToString("X2")));
        }
        public static string ToHexString(List<byte> data)
        {
            return ToHexString(data.ToArray());
        }
        public static string ToHexString(uint uid)
        {
            return ToHexString(GetBytes(uid));
        }

        public static uint ToUInt32(byte[] data, int start = 0, int len = 4)
        {
            //uint tmp = 0;
            //for(int i = 0; i < len; i++)
            //{
            //    tmp <<= 8;
            //    tmp += data[start + i];
            //}
            //return tmp;
            return BitConverter.ToUInt32(data.Skip(start).Take(len).Reverse().ToArray(), 0);
        }

        public static ushort ToUshort(byte[] data, int start = 0)
        {
            return BitConverter.ToUInt16(data.Skip(start).Take(2).Reverse().ToArray(), 0);
        }
        public static short ToShort(byte[] data, int start = 0)
        {
            return BitConverter.ToInt16(data.Skip(start).Take(2).Reverse().ToArray(), 0);
        }

        //把整数类型转化成byte[]
        public static byte[] GetBytes(uint d)
        {
            byte[] tmp = BitConverter.GetBytes(d);
            return tmp.Reverse().ToArray();
        }
        public static byte[] GetBytes(ushort d)
        {
            byte[] tmp = BitConverter.GetBytes(d);
            return tmp.Reverse().ToArray();
        }

        public static byte[] GetBytes(string str) //fixme 不优雅的拷贝方法
        {
            return str.Select(i => (byte)i).ToArray(); //由char转成byte
        }
        
        public static byte[] GetBCDBytes(string str) //返回一个bcd表示的数组, "171101" 返回 {0x17, 0x11, 0x01}
        {
            // 最多只能支持 8字节长度的str
            long i = Convert.ToInt64(str, 16);
            byte[] y = BitConverter.GetBytes(i);
            return y.Take(str.Length / 2).Reverse().ToArray();
        }

        public static bool GetBool(byte []data, int startByte, int bit) //返回某个字节, 某个位的布尔值
        {
            return Convert.ToBoolean(data[startByte] & 1 << bit);
        }


        public static string GetAsciiString(IEnumerable<byte> data)
        {
            // 把读到的数据转成string
            StringBuilder sb = new StringBuilder();
            foreach (byte b in data)
            {
                sb.Append((char)b);
            }
            return sb.ToString();
        }
    }

    public class CanProc_Test
    {
        CanProc can;
        MmTimer timer;
        public CanProc_Test()
        {
            can = new CanProc();
            can.AddBuf(0x1A0);
            can.AddBuf(0x1A1);
            can.AddBuf(0x1A2);
            can.AddBuf(0x1A3);

            can.Open();
            timer = TimerSetter.Setter(10, timer_tick);
            timer.Start();
        }

        public void timer_tick(object sender, EventArgs e)
        {
            Test();
        }
        public void Test()
        {
            CAN_OBJ frame0 = new CAN_OBJ(0x1B0, new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 });
            CAN_OBJ frame1 = new CAN_OBJ(0x1B1, new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 });
            CAN_OBJ frame2 = new CAN_OBJ(0x1B2, new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 });
            CAN_OBJ frame3 = new CAN_OBJ(0x1B3, new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 });
            CAN_OBJ frame4 = new CAN_OBJ(0x1B4, new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 });
            CAN_OBJ frame5 = new CAN_OBJ(0x1B5, new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 });
            CAN_OBJ frame6 = new CAN_OBJ(0x1B6, new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 });
            CAN_OBJ frame7 = new CAN_OBJ(0x1B7, new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 });
            CAN_OBJ frame8 = new CAN_OBJ(0x1B8, new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 });
            CAN_OBJ frame9 = new CAN_OBJ(0x1B9, new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 });

            can.TxFramesBuf.Enqueue(frame0);
            can.TxFramesBuf.Enqueue(frame1);
            can.TxFramesBuf.Enqueue(frame2);
            can.TxFramesBuf.Enqueue(frame3);
            can.TxFramesBuf.Enqueue(frame4);
            can.TxFramesBuf.Enqueue(frame5);
            can.TxFramesBuf.Enqueue(frame6);
            can.TxFramesBuf.Enqueue(frame7);
            can.TxFramesBuf.Enqueue(frame8);
            can.TxFramesBuf.Enqueue(frame9);

        }
    }
}
