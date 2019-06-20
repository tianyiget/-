using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ECAN;
using Dongzr.MidiLite;
using System.Threading;


namespace exercise_wb
{

    public enum Gear
    {
        P,
        R,
        N,
        D,
        Error,
    }
    public enum VoltaGear
    {
        N=0x00,
        P=0x05,
        R=0x07,
        D=0x04,
    }


    public class Controler
    {
       
        public const ushort INFOID = 0x279; //x37 ESM
        public const ushort GBCID = 0x0D5;
        public byte[] P = { 0x45, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        public byte[] R = { 0x47, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        public uint ID;
        CanProc can;
        CAN_OBJ frame;
        DateTime last_Time;
        public bool isPressent;
        public InfoSig sig;
        public Gear gear;

        public Controler(CanProc can)
        {
            can.AddBuf(GBCID);
            this.can =can;
        }

        public struct InfoSig
        {
            public byte Gear;
            public InfoSig(byte[] data)
            {
                Gear = MyConvert.GetByte(data, 1, 3, 4);
            }
        }

        
        public Gear ToGear(CAN_OBJ frame)
        {
            if (frame.ID != GBCID)
                throw new Exception("wrong  ID");
            switch (MyConvert.GetByte(frame.data, 0, 3, 4))
            {
                case 0x01:
                    return Gear.P;
                case 0x02:
                    return Gear.R;
                case 0x03:
                    return Gear.N;
                case 0x04:
                    return Gear.D;
                default:
                    return Gear.Error;                
            }
        }


        public byte[] Todata()
        {
            byte[] data = new byte[8];
            switch (gear)
            {
                case Gear.P:

                    break;
                case Gear.R:
                    break;
                case Gear.N:
                    break;
                case Gear.D:
                    break;
                case Gear.Error:
                    break;
                default:
                    break;
            }


            return data;
        }

        public void Tick()//实现接收报文的功能
        {
            if (!can.RxFramesBuf[GBCID].IsEmpty)
            {
                do
                {
                    frame = can.RxFramesBuf[GBCID].Dequeue();
                    gear=ToGear(frame);
                    ID=frame.ID;

                    last_Time = DateTime.Now;
                } while (!can.RxFramesBuf[GBCID].IsEmpty);
            }
            else if (DateTime.Now.Subtract(last_Time) > new TimeSpan(0, 0, 0, 0,100))
                isPressent = false;
        }
    }



    public class Product
    {
        public bool isOpen = false;
        public CanProc can;
        MmTimer timer;
        public Controler ctr;
        public Product()
        {
            can = new CanProc();
            ctr = new Controler(can);
            timer = TimerSetter.Setter(Interval: 10, tick: Time_Tick);

        }

        private void Time_Tick(object sender, EventArgs e)
        {
            ctr.Tick();
        }

        public void Open()
        {
            if (isOpen)
                Error("设备已打开");
            if (!can.Open())
                Error("无法初始化CAN！");
            timer.Start();
            isOpen = true;
        }
        public void Close()
        {
            if (isOpen)
            {
                can.Close();
                timer.Stop();
                isOpen = false;
            }
        }

        public void Dispose()
        {
            if (isOpen)
                Close();
            timer.Dispose();
            can.Dispose();
        }

        public void Error(string sig)
        {
            throw new Exception(sig);
        }


    }




}
