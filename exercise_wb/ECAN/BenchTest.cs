using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

/* 对我们工厂模式的封装, 基于 <测试台架软件通用要求.doc> V0.02版本
 * 框架要能适应所有的命令 以及之后的扩展
 */


namespace ECAN
{
    public enum BTInd
    {
        NotDone, //未完成
        Error,
        Positive,
        //Negtive, //貌似没有这个
    }
    public enum SuccessState //普适的很多命令都有的回复
    {
        Fail = 0x00,
        Success = 0x01,
        Running = 0x03,
    }

    public abstract class BentchTest //产品级别的检测命令
    {
        public CanProc can;
        public BTNet btNet;
        public BentchTest(CanProc can, ushort SendID, ushort RecvID, bool debug = false)
        {
            this.can = can;
            btNet = new BTNet(can, SendID, RecvID, debug);
        }

        public virtual bool EnterFactory() //进入工厂模式, 统一的
        {
            StartFactoryModeCMD cmd = new StartFactoryModeCMD(StartFactoryModeCMD.Sub.RequestSeed);
            if (!btNet.DoCommand(cmd))
                return false;
            cmd = new StartFactoryModeCMD(StartFactoryModeCMD.Sub.SendKey, StartFactoryModeCMD._calcKey(cmd.seed));
            if (!btNet.DoCommand(cmd))
                return false;
            return true;
        }

        public virtual string ReadSoftVersion()
        {
            InnerSoftwareReadCommand cmd = new InnerSoftwareReadCommand();
            if (!btNet.DoCommand(cmd))
                return null;
            return MyConvert.GetAsciiString(cmd.replyData.Take(26));
        }

        public void ControlMotor(bool pos)
        {
            MotoActionCommand.Sub sub;
            if (pos)
                sub = MotoActionCommand.Sub.pos;
            else
                sub = MotoActionCommand.Sub.neg;
            MotoActionCommand cmd = new MotoActionCommand(sub);
            btNet.DoCommand(cmd);
        }

        public void ReadIOState()
        {
            StateInfoReadCommand cmd = new StateInfoReadCommand();
            btNet.DoCommand(cmd);
        }

    }
    public class B11DCTBentchTest : BentchTest
    {
        public const ushort SendID = 0x1A0;
        public const ushort RecvID = 0x70E;

        [Flags]
        public enum OpSwitch //光电开关
        {
            none = 0x00,
            op1 = 0x01,
            op2 = 0x02,
            op3 = 0x04,
            all = op1 | op2 | op3,
        }

        public struct HallData
        {
            public ushort Left;
            public ushort Right;
            public HallData(ushort left, ushort right)
            {
                Left = left;
                Right = right;
            }
        }
        public B11DCTBentchTest(CanProc can) : base(can, SendID, RecvID)
        {
        }

        public new byte[] ReadIOState()
        {
            StateInfoReadCommand cmd = new StateInfoReadCommand();
            btNet.DoCommand(cmd);
            return cmd.replyData.ToArray();
        }

        public OpSwitch ReadOpState()
        {
            StateInfoReadCommand cmd = new StateInfoReadCommand();
            btNet.DoCommand(cmd);
            return (OpSwitch)(MyConvert.GetByte(cmd.replyData.ToArray(), 6, 2, 3));
        }

        public HallData ReadHallState()
        {
            StateInfoReadCommand cmd = new StateInfoReadCommand();
            btNet.DoCommand(cmd);
            byte[] data = cmd.replyData.ToArray();
            return new HallData(MyConvert.ToUshort(data, 0), MyConvert.ToUshort(data, 2));
        }

        public void ControlGear(GearLedCommand.GearLed gear) //b11是特殊的, 灯信号没有echo
        {
            var cmd = new GearLedCommand(gear);
            cmd.reply = false; //特殊
            btNet.DoCommand(cmd);
        }
        
    }

    public class E100BentchTest : BentchTest
    {
        public const ushort SendID = 0x155;
        public const ushort RecvID = 0x72F;
        public E100BentchTest(CanProc can, bool debug = false) : base(can, SendID, RecvID, debug)
        {

        }

        public bool WriteFactoryData(string hwVersion, string date, string sncode) //写入出厂数据
        {
            byte[] tmp;
            ////写入零件号 不用写
            //ComponentWriteCommand pnCmd = new ComponentWriteCommand(pnNumber);
            //if (!btNet.DoCommand(pnCmd) || !pnCmd.res)
            //    return false;

            //写入硬件版本号
            BTCommand hwCmd = new BTCommand(0x03, 1);
            tmp = new byte[20];
            hwVersion.Select(i => (Byte)i).ToArray().CopyTo(tmp, 0);
            hwCmd.serviceData.AddRange(tmp);
            if (!btNet.DoCommand(hwCmd) || !(hwCmd.replyData[0] == 0x01))
                return false;

            //写入生产日期
            ProductDateWriteCommand dateCmd = new ProductDateWriteCommand(date);
            if (!btNet.DoCommand(dateCmd) || !dateCmd.res)
                return false;

            //写入流水号
            SerialNumberWriteCommand snCmd = new SerialNumberWriteCommand(sncode);
            if (!btNet.DoCommand(snCmd) || !snCmd.res)
                return false;
            return true;
        }
    }

    public class BTCommand //命令实体, 协议层(服务层)
    {
        /*
         * 服务层中 只包含 命令, 数据 两个
        * */
        public bool reply = true; //是否要有响应
        public BTInd ind = BTInd.NotDone;
        public string infos;

        public List<byte> serviceData = new List<byte>();
        public List<byte> replyData = new List<byte>();

        public byte CommandId { get; set; }
        public byte DataLen { get; set; }
        public BTCommand(byte cmdID, byte dataLen)
        {
            CommandId = cmdID;
            DataLen = dataLen; //默认的要求返回的数据长度
        }
        public byte[] ToBytes()
        {
            List<byte> tmp = new List<byte>();
            tmp.Add(CommandId);
            tmp.AddRange(serviceData);
            return tmp.ToArray();
        }
        public virtual void ParseData()
        {
        }
        public virtual void ParseCheck()
        {
            if (replyData.Count != DataLen) //如果之后数据改了, 需要更改
                throw new Exception("报文回复长度不对");
            //对一下 sub的字段, 貌似没必要了...
        }
    }
    //其他cmd,省略.
    public class StartFactoryModeCMD : BTCommand
    {
        public enum Sub
        {
            RequestSeed = 0x01, //请求种子命令
            SendKey = 0x02, //发送秘钥
            ExitMode = 0x03, //退出工厂模式
        }

        const uint ALGORITHMASK = 0x42313142;
        const uint UNLOCKKEY = 0x00000000;
        const uint UNLOCKSEED = 0x00000000;
        const uint UNDEFINESEED = 0xFFFFFFFF;
        const uint SEEDMASK = 0x80000000;
        const int SHIFTBIT = 1;

        public Sub sub;

        public uint seed; //在请求时使用

        public SuccessState success; //在发送秘钥和退出时使用

        public static uint _calcKey(uint seed)
        {
            uint key = UNLOCKKEY;
            if (seed != UNLOCKSEED && seed != UNDEFINESEED)
            {
                key = seed;
                for (int i = 0; i < 35; i++)
                {
                    if (Convert.ToBoolean(key & SEEDMASK))
                    {
                        key <<= SHIFTBIT;
                        key ^= ALGORITHMASK;
                    }
                    else
                    {
                        key <<= SHIFTBIT;
                    }
                }
            }
            return key;
        }

        public StartFactoryModeCMD(Sub sub, uint? key = null) : base(cmdID: 0x00, dataLen: 5)
        {
            this.sub = sub;
            serviceData.Add((byte)sub);
            if (sub == Sub.RequestSeed)
            {
                serviceData.AddRange(new byte[4]);
            }
            else if (sub == Sub.SendKey)
            {
                serviceData.AddRange(MyConvert.GetBytes((uint)key));
            }
            else if (sub == Sub.ExitMode)
            {
                serviceData.AddRange(new byte[4]);
            }
        }

        public override void ParseData()
        {
            if (sub == Sub.RequestSeed)
            {
                seed = MyConvert.ToUInt32(replyData.Skip(1).ToArray());
            }
            else if (sub == Sub.SendKey || sub == Sub.ExitMode)
            {
                success = (SuccessState)replyData[1];
            }
        }
    }

    public class CaliGearCMD : BTCommand
    {
        public enum Sub
        {
            P = 0x01,
            R = 0x02,
            N = 0x03,
            D = 0x04,
            M = 0x05,
            Plus = 0x06,
            Minus = 0x07,
            Sport = 0x08
        }
        [Flags]
        public enum ErrorInfo
        {
            //旋转霍尔失效         = 0x01,    //其实可以中文, 但总是很怪
            //写标定数值出错        = 0x02,
            //线性霍尔出错         = 0x04,
            //光电开关失效或者结构卡止   = 0x08,
            //电机运行时间过长或者带轮断裂 = 0x10,
            //电机损坏或电机驱动芯片损坏  = 0x20,

            NoError = 0x00,
            HallInvalid = 0x01, //旋转霍尔失效
            WriteCaliError = 0x02, //写标定数值出错
            LinerHallError = 0x04, //线性霍尔出错
            SwitchInvalidOrStuck = 0x08, //光电开关失效或者结构卡止
            MotorRunTimeTooLong = 0x10, //电机运行时间过长或者带轮断裂
            MotorFail = 0x20, //电机损坏或电机驱动芯片损坏
        }

        Sub sub;
        public SuccessState success; //标定是否成功
        public ErrorInfo errInfo; //如果失败, 失败的原因
        public CaliGearCMD(Sub gear) : base(cmdID: 0x01, dataLen: 3)
        {
            this.sub = gear; //这个可以之后统一起来.
            serviceData.Add((byte)gear);
        }
        public override void ParseData()
        {
            success = (SuccessState)replyData[1];
            errInfo = (ErrorInfo)replyData[2];
        }
    }

    public class SolenoidTestCommand : BTCommand
    {
        public enum Sub
        {
            ON = 0x01, //电磁阀开启
            OFF = 0x02, //电磁阀关闭
        }
        [Flags]
        public enum Errorinfo
        {
            OpenCircle = 0x01,
            CloseToBattary = 0x02,
            CloseToGround = 0x04,
            Stuck = 0x08,
        }
        public SuccessState success;
        public Errorinfo errInfo;

        public SolenoidTestCommand(Sub sub) : base(cmdID: 0x02, dataLen: 3)
        {
            serviceData.Add((byte)sub);
        }
        public override void ParseData()
        {
            success = (SuccessState)replyData[1];
            errInfo = (Errorinfo)replyData[2];
        }
    }

    public class GearLedCommand : BTCommand
    {
        public enum GearLed
        {
            AllOff = 0x00,
            P = 0x01,
            R = 0x02,
            N = 0x03,
            D = 0x04,
            M = 0x05,
            Plus = 0x06,
            Minus = 0x07,
            Sport = 0x08,
            Winter = 0x09,
            AllOn = 0xFF,
        }
        public SuccessState success;
        public GearLedCommand(GearLed sub) : base(cmdID: 0x08, dataLen: 2)
        {
            serviceData.Add((byte)sub);
            //reply = false; //这个命令是不需要echo的. //最新的协议是需要echo的
        }

        public override void ParseData()
        {
            success = (SuccessState)replyData[1];
        }
    }

    //class GearCompareCommand : BTCommand //请求档位比对


    //class HardWareWriteCommand : BTCommand // 写入硬件版本号

    //class HardWareReadCommand : BTCommand //读硬件版本号

    class SerialNumberWriteCommand : BTCommand //写序列号命令
    {
        public bool res;
        public SerialNumberWriteCommand(string snCode) : base(0x04, 1)
        {
            byte[] tmp = new byte[30];
            snCode.Select(i => (Byte)i).ToArray().CopyTo(tmp, 0);
            serviceData.AddRange(tmp);
        }
        public override void ParseData()
        {
            res = replyData[0] == 0x01; //00表示失败, 01表示成功
        }
    }

    //class SerialNumberReadCommand : BTCommand //读序列号命令

    class ProductDateWriteCommand : BTCommand //写入生产日期
    {
        public bool res;
        public ProductDateWriteCommand(string date) : base(0x05, 1)
        {
            byte[] tmp = new byte[10];
            MyConvert.GetBCDBytes(date).CopyTo(tmp, 0);
            serviceData.AddRange(tmp);
        }
        public override void ParseData()
        {
            res = replyData[0] == 0x01;
        }
    }

    //class ProductDateReadCommand : BTCommand //读生产日期

    class ComponentWriteCommand : BTCommand //写入零件号
    {
        public bool res;
        public ComponentWriteCommand(string snCode) : base(0x06, 1)
        {
            //填入所需的字符, 不足填充到30字节
            //传入的是一个string, 但实际上要以一个四字节整形输入

            byte[] tmp = new byte[30];
            MyConvert.GetBytes(Convert.ToUInt32(snCode)).CopyTo(tmp, 0);
            serviceData.AddRange(tmp);
        }
        public override void ParseData()
        {
            res = replyData[0] == 0x01; //00表示失败, 01表示成功
        }
    }

    class ComponentReadCommand : BTCommand//读出零件号
    {
        public ComponentReadCommand() : base(0x86, 30)
        {

        }
    }

    class MotoActionCommand : BTCommand //电机升降命令
    {
        public enum Sub
        {
            pos = 0x01, //正转
            neg = 0x02, //反转
        }
        public SuccessState res;
        public byte err;
        public MotoActionCommand(Sub sub) : base(0x07, 3)
        {
            serviceData.Add((byte)sub);
        }
        public override void ParseData()
        {
            res = (SuccessState)replyData[1];
            err = replyData[2];
        }
    }

    class InnerSoftwareReadCommand : BTCommand //读内部软件版本号
    {
        public InnerSoftwareReadCommand() : base(0x89, 30)
        {
        }
    }

    public class StateInfoReadCommand : BTCommand//读状态信息命令
    {
        public StateInfoReadCommand() : base(0x8A, 20)
        {

        }
    }
    public class BTNet // 相当于uds里面的网络层, 负责命令的收发, 状态的变更
    {
        /*
         * 网络层, 包含 报文帧的其他部分. 包括帧头, 帧数据长度和校验码.
         * */
        //public const ushort SendID = 0x1AE;
        //public const ushort RecvID = 0x7FF;
        public ushort SendID;
        public ushort RecvID;

        public const ushort FillByte = 0x5AA5; //帧头 

        public const int WAITTIME = 300; //最大等待时间
        public const int WAITPERIOD = 50;

        public bool debug;
        public MyQueue<CAN_OBJ> frameList = new MyQueue<CAN_OBJ>();

        CanProc can;
        public BTNet(CanProc can, ushort sendId, ushort recvId, bool debug = false)
        {
            this.SendID = sendId; //通过设定id
            this.RecvID = recvId;
            this.can = can;
            can.AddBuf(RecvID);
            this.debug = debug;
        }
        public void Record(CAN_OBJ frame)
        {
            frameList.Enqueue(frame);
        }

        public bool DoCommand(BTCommand cmd) //把这个服务完成命令并返回
        {
            can.RxFramesBuf[RecvID].Clear(); //防止之前的报文影响

            List<byte> SendData = new List<byte>();
            ushort length = (ushort)(cmd.serviceData.Count + 2); //2代表命令id及最后的校验位

            SendData.AddRange(MyConvert.GetBytes(FillByte)); //填入帧头
            SendData.AddRange(MyConvert.GetBytes(length));   //填入报文长度
            SendData.Add(cmd.CommandId);                     //填入cmdid //到时候来比较下, 这个id应该放在serviceid还是单独
            SendData.AddRange(cmd.serviceData);              //填入数据
            SendData.Add(_calCheckSum(cmd.ToBytes()));       //填入crc校验码

            //开始发送报文, 不足补0
            for (int start = 0; start < SendData.Count; start += 8)
            {
                byte[] sendBytes;
                if (start + 8 < SendData.Count) //剩余长度足够拼成一个完整的8字节帧
                {
                    sendBytes = SendData.Skip(start).Take(8).ToArray();
                }
                else
                {
                    sendBytes = new byte[8];
                    SendData.Skip(start).ToArray().CopyTo(sendBytes, 0); //把剩下的能用的都拷贝过去
                }
                CAN_OBJ frame = new CAN_OBJ(SendID, sendBytes);
                if (debug)
                    Record(frame);
                can.TxFramesBuf.Enqueue(frame);
            }

            if (cmd.reply) //如果需要接收
            {
                /* 接收的数据也包括以下几部分: 
                 * 帧头, 数据长度, CMDid, 数据, crc校验
                 */
                bool isFirstFrame = true;
                List<byte> tmp = new List<byte>();
                ushort byteAllToRead = 0; //返回的需要一共接收的字节

                for (int waittimes = WAITTIME / WAITPERIOD;
                    waittimes >= 0;
                    Thread.Sleep(WAITPERIOD), waittimes--)
                {
                    while (!can.RxFramesBuf[RecvID].IsEmpty)
                    {
                        CAN_OBJ frame = can.RxFramesBuf[RecvID].Dequeue();
                        if (debug)
                            Record(frame);
                        if (isFirstFrame) //第一帧才决定了要读取多少帧
                        {
                            isFirstFrame = false;
                            if (MyConvert.ToUshort(frame.data, 0) != FillByte)
                                return _res(cmd, false, "回复报文帧头不对!!");
                            byteAllToRead = MyConvert.ToUshort(frame.data, 2); //一共要读取多少字节

                            if (byteAllToRead > 4) //剩下一共4个字节
                            {
                                tmp.AddRange(frame.data.Skip(4));
                            }
                            else
                            {
                                tmp.AddRange(frame.data.Skip(4).Take(byteAllToRead));
                                return _valid(cmd, tmp);
                            }
                        }
                        else
                        {
                            if (byteAllToRead - tmp.Count >= 8)
                            {
                                tmp.AddRange(frame.data);
                            }
                            else
                                tmp.AddRange(frame.data.Take(byteAllToRead - tmp.Count)); //剩下的都是00, 不需要

                            if (tmp.Count == byteAllToRead) //数据刚好
                                return _valid(cmd, tmp);
                        }
                    }
                }
                return _res(cmd, false, "超时未收到回复!!");
            }
            else
            {
                return _res(cmd, true);
            }
        }

        public bool _valid(BTCommand cmd, List<byte> data)
        {
            // 对于这个data来说, 第一个字节是 commendid, 最后一个字节是checksum
            if (data.Count < 2)
                return _res(cmd, false, "长度不符合最小要求");

            if (_calCheckSum(data.ToArray()) != 0x00)
                return _res(cmd, false, "验证码校验失败");

            if (data[0] != cmd.CommandId)
                return _res(cmd, false, "cmd命令id不正确");

            cmd.replyData.AddRange(data.Skip(1).Take(data.Count - 2)); //后两个字节 和 前面的cmdid不要
            try
            {
                cmd.ParseCheck();
                cmd.ParseData();
                return _res(cmd, true);
            }
            catch (Exception ex)
            {
                return _res(cmd, false, ex.Message);
            }
        }

        public bool _res(BTCommand cmd, bool ok, string info = "成功")
        {
            if (ok)
                cmd.ind = BTInd.Positive;
            else
                cmd.ind = BTInd.Error;

            cmd.infos = info;
            return ok;
        }

        public byte _calCheckSum(params byte[] data)
        {
            byte t = 0x00;
            foreach (byte b in data)
            {
                t += b;
            }
            t ^= 0xFF;
            return t;
        }
    }

}
