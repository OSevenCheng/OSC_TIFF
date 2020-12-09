using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
//using System.Diagnostics;
namespace OSC_TIFF
{
    public class CompressionLZW
    {
		static private int Code = 0;
		static private int EoiCode = 257;
		static private int ClearCode = 256;
		static private int OldCode = 256;
		//static private Dictionary<int, string> Dic;
		static private string[] Dic= new string[4096];
		static private int DicIndex;
		//static private List<string> Dic;
		static private byte[] Input;
		static private int startPos;
		static private byte[] Output;
		static private int resIndex;
		static private int current=0;
		static private int bitsCount = 0;
		static private string OutputStr="";
		static string combine ="{0}{1}";
		//delegate int GetBitFunc(int x);
		//static GetBitFunc[] GetBitFuncs = new GetBitFunc[]{GetBit0,GetBit1};
		static private void ResetPara()
		{
            OldCode = 256;
            DicIndex = 0;
            current = 0;
            OutputStr = "";
			resIndex = 0;
		}
		static public void CreateBuffer(int size)
		{
			Output = new byte[size];
			DicIndex = 0;
            while (DicIndex < 256)
            {
                char x = (char)DicIndex;
                Dic[DicIndex++] = x.ToString();
            }
            Dic[256] = "";
            DicIndex = 258;
			//Debug.Log("size " + bits.Length);
		}
		static public byte[] Decode(byte[] input,int _startPos,int _readLength)
        {
			Input = input;
			startPos = _startPos;
			bitsCount = _readLength*8;
			ResetPara();
			while ((Code = GetNextCode()) != EoiCode) 
			{
				if (Code == ClearCode) 
				{
					InitializeTable();
					Code = GetNextCode();
					if (Code == EoiCode)
						break;
					WriteResult(Dic[Code]);
					OldCode = Code;
				}
				else 
				{
					if (Dic[Code]!=null) 
					{
						WriteResult(Dic[Code]);
						Dic[DicIndex++] =string.Format(combine, Dic[OldCode],Dic[Code][0]);
						OldCode = Code;
					} 
					else 
					{   
                        string outs = string.Format(combine, Dic[OldCode], Dic[OldCode][0]);
						WriteResult(outs);
						Dic[DicIndex++] =outs;
						OldCode = Code;
					}
				}
			}
			return Output;
        }
		static private int GetNextCode()
		{
			int tmp = 0;
			int step = GetStep();
			if (current + step > bitsCount)
				return EoiCode;
			//Debug.Log("DicIndex : "+DicIndex);
			//Debug.Log("step : "+step);
			//int tstep = step -1;
			for (int i = 0; i<step; i++)
            {
				int x = current + i;
				int bit = GetBit(x)<<(step-1-i);
				tmp+=bit;
				//tmp = GetBit(x) ?tmp + (128>>i):tmp;
            }
			//Debug.Log("tmp : "+tmp);
			current += step;
			//一开始读9个bit
			//读到510的时候，下一个开始读10bit
			//读到1022的时候，下一个开始读11bit
			//读到2046的时候，下一个开始读11bit
			return tmp;
		}
		static private int GetBit(int x)
		{
			int byteIndex = x/8; //该bit在第几个byte里
			int bitIndex =7-x+byteIndex*8;//该bit是这个byte的第几位
			byte b = Input[startPos + byteIndex];
			return (b>>bitIndex)&1;
		}
		static private int GetStep()
		{
			int res = 12;
			int tmp = DicIndex-2047;//如果大于2046.则为正或零
			res+=(tmp>>31);
			tmp = DicIndex-1023;
			res+=(tmp>>31);
			tmp = DicIndex-511;
			res+=(tmp>>31);
		    //Debug.Log(res);
			return res;
		}
		static private void InitializeTable()
		{
			Array.Clear(Dic,257,3839);
			DicIndex = 258;
		}
		static private void WriteResult(string code)
		{
			for(int i = 0;i<code.Length;i++)
			Output[resIndex++] = (byte)code[i];
		}
    }
}


/*
(255,255,0)  00000001 1111110011111011 000001110001000000010000
(0,0,0)      00000001 0000000000000100 0000101000001000
(1,1,1)      00000001 0000000000000110 0000101000001000
(2,2,2)      00000001 0000000000000101 0000101000001000
(4,4,4)      00000001 1000000000000100 0000101000001000
(8,8,8)      00000001 0100000000000100 0000101000001000
(16,16,16)   00000001 00100000 000001000000101000001000


(255,255,255)00000001 11111100 00000111 00001010 00001000
             10000000 00111111 11100000 01010000 00010000
			 10000000

             10000000 00000100 001000000101000000010000

             00000001 0110100000000100 0000101000001000
			 00000001 0000110000000111 0000101000001000
1   1
2   10
4   100
8   1000
255 011111111
256 100000000
257 100000001
258 100000010
    010000001
259 100000011
260 100000100
*/