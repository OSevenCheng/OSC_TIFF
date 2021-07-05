
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace OSC_TIFF
{
    public delegate int DealByteOrder(byte[] b, ulong startPos, int length);
        // byte[] byteTemp = new byte[8];
        // private void ByteOrderII(byte[] b, ulong startPos, int length)
        // {
        //     for (int i = 0; i < length; i++)
        //         byteTemp[i] = b[startPos + (ulong)i];
        // }
        // private void ByteOrderMM(byte[] b, ulong startPos, int length)
        // {
        //     for (int i = 0; i < length; i++)
        //         byteTemp[i] = b[startPos + (ulong)(length - 1 - i)];
        // }
    public class DEBase
    {
        public static int ByteOrderII(byte[] b, ulong startPos, int length)
        {
            int value=0;
            for (int i = 0; i < length; i++) value |= data[startPos + (ulong)i] << i * 8;
            return value;
        }
        public static int ByteOrderMM(byte[] b, ulong startPos, int length)
        {
            int value=0;
            for (int i = 0; i < length; i++) value |= data[startPos + (ulong)(length - 1 - i)] << i * 8;
            return value;
        }
        public static DealByteOrder byteToInt;
        public static byte[] data;
        //public static int ByteOrder;
        public virtual void SetValue(ulong pdata, int typesize, int Count=1)
        {
            Debug.Log("Base SetValue");
        }
        protected int GetInt(ulong startPos, int Length)//读负数会有问题
        {
            //int value = 0;
            return byteToInt(data,startPos,Length);

            // if (ByteOrder == 1)// "II")
            //     for (int i = 0; i < Length; i++) value |= data[startPos + (ulong)i] << i * 8;
            // else // "MM")
            //     for (int i = 0; i < Length; i++) value |= data[startPos + (ulong)(Length - 1 - i)] << i * 8;
            // return value;
        }
        protected float GetRational(ulong startPos)
        {
            int A = GetInt(startPos, 4);
            int B = GetInt(startPos + 4, 4);
            return A / B;
        }
        protected string GetString(ulong startPos, int Length)
        {
            string tmp = "";
            for (int i = 0; i < Length; i++)
                tmp += (char)data[startPos];
            return tmp;
        }
        protected double GetDouble(ulong startPos, int Length)
        {
            return 0.0;
        }
    }
    public class DEInt : DEBase
    {
        public DEInt(int v) {
            value = v;
        }
        private int value;
        //public int Value=>value;
        public int Value{get{return value;}}
        public override void SetValue(ulong pdata, int typesize,int Count =1)
        {
            value = GetInt(pdata, typesize);
        }
    }
    public class DEIntArray : DEBase
    {
        public DEIntArray() {
        }
        private List<int> values = new List<int>();
        //public int Count => values.Count;
        public int Count{get{return values.Count;}}
        //public int Value(int i)=>values[i];
        public int Value(int i){return values[i];}
        public override void SetValue(ulong pdata, int typesize, int Count=1)
        {
            for (int i = 0; i < Count; i++)
            {
                int v = GetInt(pdata + (ulong)(i * typesize), typesize);
                values.Add(v);
            }
        }
    }
    public class DERational : DEBase
    {
        private float value;
        public float Value{get{return value;}}
        public override void SetValue(ulong pdata, int typesize,int Count = 1)
        {
            value = GetRational(pdata);
        }
    }
    public class DEString : DEBase
    {
        private string value;
        public string Value{get{return value;}}
        public override void SetValue(ulong pdata, int typesize, int Count)
        {
            value = GetString(pdata, typesize);
        }
    }
    public class DEStringArray : DEBase
    {
        private List<string> values = new List<string>();
        //public string Value(int i)=>values[i];
        public string Value(int i){return values[i];}
        public override void SetValue(ulong pdata, int typesize, int Count =1)
        {
            for (int i = 0; i < Count; i++)
            {
               string v = GetString(pdata + (ulong)(i * typesize), typesize);
               values.Add(v);
            }
        }
    }
    public class DEDouble : DEBase
    {
        private double value;
        //public double Value => value;
        public double Value {get{return value;}}
        public override void SetValue(ulong pdata, int typesize, int Count =1)
        {
            value = GetDouble(pdata, typesize);
        }
    }
    public class DEDoubleArray : DEBase
    {
        private List<double> values = new List<double>();
        //public double Value(int i)=>values[i];
        public double Value(int i){return values[i];}
        public override void SetValue(ulong pdata, int typesize, int Count =1)
        {
            for (int i = 0; i < Count; i++)
            {
                double v = GetDouble(pdata + (ulong)(i * typesize), typesize);
                values.Add(v);
            }
        }
    }
    
}