using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;

namespace OSC_TIFF
{
    public class TIFF
    {
        //Unity Texture
        Texture2D tex;

        byte[] data;

        bool ByteOrder;//true:II  false:MM

        public int ImageWidth = 0;
        public int ImageLength = 0;

        public List<int> BitsPerSample = new List<int>();
        public int PixelBytes = 0;
        public int Compression = 0;

        /// <summary>
        /// 0 = WhiteIsZero. For bilevel and grayscale images: 0 is imaged as white. The maximum value is imaged as black. This is the normal value for Compression=2.
        /// 1 = BlackIsZero. For bilevel and grayscale images: 0 is imaged as black. The maximum value is imaged as white. If this value is specified for Compression=2, the
        /// image should display and print reversed.
        /// </summary>
        public int PhotometricInterpretation = 0;

        /// <summary>
        ///For each strip, the byte offset of that strip
        /// </summary>
        public List<int> StripOffsets = new List<int>();//For each strip, the byte offset of that strip

        /// <summary>
        ///The number of rows in each strip (except possibly the last strip.)
        ///For example, if ImageLength is 24, and RowsPerStrip is 10, then there are 3
        ///strips, with 10 rows in the first strip, 10 rows in the second strip, and 4 rows in the
        ///third strip. (The data in the last strip is not padded with 6 extra rows of dummy data.)
        /// </summary>
        public int RowsPerStrip = 0;

        /// <summary>
        ///For each strip, the number of bytes in that strip after any compression.
        /// </summary>
        public List<int> StripByteCounts = new List<int>();
        public float XResolution = 0f;
        public float YResolution = 0f;

        public int PlannarConfiguration = 1;
        public int ResolutionUnit = 0;
        public int Predictor = 0;

        /// <summary>
        //This field specifies how to interpret each data sample in a pixel. Possible values are:
        // 1 = unsigned integer data
        // 2 = two’s complement signed integer data
        // 3 = IEEE floating point data [IEEE]
        // 4 = undefined data format
        /// </summary>
        public List<int> SampleFormat = new List<int>();
        public string DateTime = "";
        public string Software = "";

        public void Init(string path)
        {
            data = File.ReadAllBytes(path);

            //首先解码文件头，获得编码方式是大端还是小端，以及第一个IFD的位置
            int pIFD = DecodeIFH();


            //然后解码第一个IFD，返回值是下一个IFD的地址
            while (pIFD != 0)
            {
                pIFD = DecodeIFD(pIFD);
            }
        }
        public Texture2D GetUnityTexture()
        {
            return tex;
        }

        private int DecodeIFH()
        {
            string byteOrder = GetString(0, 2);
            if (byteOrder == "II")
                ByteOrder = true;
            else if (byteOrder == "MM")
                ByteOrder = false;
            else
                throw new UnityException("The order value is not II or MM.");

            int Version = GetInt(2, 2);

            if (Version != 42)
                throw new UnityException("Not TIFF.");

            return GetInt(4, 4);
        }
        public int DecodeIFD(int Pos)
        {
            int n = Pos;
            int DECount = GetInt(n, 2);
            n += 2;
            for (int i = 0; i < DECount; i++)
            {
                DecodeDE(n);
                n += 12;
            }
            //已获得每条扫描线位置，大小，压缩方式和数据类型，接下来进行解码
            DecodeStrips();
            int pNext = GetInt(n, 4);
            return pNext;
        }
        public void DecodeDE(int Pos)
        {
            int TagIndex = GetInt(Pos, 2);
            int TypeIndex = GetInt(Pos + 2, 2);
            int Count = GetInt(Pos + 4, 4);
            //Debug.Log("Tag: " + Tag(TagIndex) + " DataType: " + TypeArray[TypeIndex].name + " Count: " + Count);

            //先把找到数据的位置
            int pData = Pos + 8;
            int totalSize = TypeArray[TypeIndex].size * Count;
            if (totalSize > 4)
                pData = GetInt(pData, 4);

            //再根据Tag把值读出来，保存起来
            GetDEValue(TagIndex, TypeIndex, Count, pData);
        }
        private void GetDEValue(int TagIndex, int TypeIndex, int Count, int pdata)
        {
            int typesize = TypeArray[TypeIndex].size;
            switch (TagIndex)
            {
                case 254: break;//NewSubfileType
                case 255: break;//SubfileType
                case 256://ImageWidth
                    ImageWidth = GetInt(pdata, typesize); break;
                case 257://ImageLength
                    ImageLength = GetInt(pdata, typesize); break;
                case 258://BitsPerSample
                    for (int i = 0; i < Count; i++)
                    {
                        int v = GetInt(pdata + i * typesize, typesize);
                        BitsPerSample.Add(v);
                        PixelBytes += v / 8;
                    }
                    break;
                case 259: //Compression
                    Compression = GetInt(pdata, typesize); break;
                case 262: //PhotometricInterpretation //一般等于1，2基本不用，等于2就不解了
                    PhotometricInterpretation = GetInt(pdata, typesize); break;
                case 273://StripOffsets
                    for (int i = 0; i < Count; i++)
                    {
                        int v = GetInt(pdata + i * typesize, typesize);
                        StripOffsets.Add(v);
                    }
                    break;
                case 274: break;//Orientation
                case 277: break;//SamplesPerPixel
                case 278://RowsPerStrip
                    RowsPerStrip = GetInt(pdata, typesize); break;
                case 279://StripByteCounts
                    for (int i = 0; i < Count; i++)
                    {
                        int v = GetInt(pdata + i * typesize, typesize);
                        StripByteCounts.Add(v);
                    }
                    break;
                case 282: //XResolution
                    XResolution = GetRational(pdata); break;
                case 283://YResolution
                    YResolution = GetRational(pdata); break;
                case 284:
                    PlannarConfiguration = GetInt(pdata, typesize); break;//PlanarConfig
                case 296://ResolutionUnit
                    ResolutionUnit = GetInt(pdata, typesize); break;
                case 305://Software
                    Software = GetString(pdata, typesize); break;
                case 306://DateTime
                    DateTime = GetString(pdata, typesize); break;
                case 315: break;//Artist
                case 317: //Differencing Predictor
                    Predictor = GetInt(pdata, typesize); break;
                case 320: break;//ColorDistributionTable
                case 338: break;//ExtraSamples
                case 339: //SampleFormat
                    for (int i = 0; i < Count; i++)
                    {
                        int v = GetInt(pdata + i * typesize, typesize);
                        SampleFormat.Add(v);
                    }
                    break;

                default:
                    Debug.LogError(TagIndex);
                    break;
            }
        }
        private void DecodeStrips()
        {
            if (BitsPerSample.Count != 0)
            {
                //若有多个通道，判断多个通道大小是否一致
                int temp = 0;
                int count = -1;
                for (int i = 0; i < BitsPerSample.Count; i++)
                {
                    if (BitsPerSample[i] != temp)
                         count++;
                    temp = BitsPerSample[i];
                }
                if (count > 0)//三个通道大小不一致
                {
                    return;//暂不支持
                }
            }
            else
            {
                return;
            }
            int pStrip = 0;
            int size = 0;
            //int f = 1;
            TextureFormat f = TextureFormat.RGBA32;//默认为4通道unsigned整型
            if (SampleFormat.Count == 4)
            {
                if (SampleFormat[0] == 2)//有符号整型
                {
                    return;//暂不支持
                }
                if (SampleFormat[0] == 3)//float
                {
                    f = TextureFormat.RGBAFloat;
                }
            }
            else if (SampleFormat.Count == 1)
            {
                switch(SampleFormat[0])
                {
                    case 1:
                        f = TextureFormat.R16;
                        break;
                    case 3:
                        f = TextureFormat.RFloat;
                        break;
                    default:
                        return;
                }

            }
            tex = new Texture2D(ImageWidth, ImageLength, f, false);
            //tex = new Texture2D(500, 500, TextureFormat.RGBAFloat, false);
            int PixelCount = ImageWidth * ImageLength;
            Color[] colors = new Color[PixelCount];
            int index = PixelCount;
            if (Compression == 5)
            {
                int stripLength = ImageWidth * RowsPerStrip * BitsPerSample.Count * BitsPerSample[0] / 8;
                CompressionLZW.CreateBuffer(stripLength);
                //Debug.Log("PlannarConfiguration : " + PlannarConfiguration);//2基本不用，等于2就不解了
                if (Predictor == 1)//没有差值处理
                {
                    for (int y = 0; y < StripOffsets.Count; y++)
                    {
                        pStrip = StripOffsets[y];//起始位置
                        size = StripByteCounts[y];//读取长度
                        byte[] Dval = CompressionLZW.Decode(data, pStrip, size);

                        if (f == TextureFormat.RGBA32 && BitsPerSample[0] == 1)
                        {
                            for (int x = 0; x < ImageWidth * RowsPerStrip; x++)
                            {
                                float R = Dval[x * PixelBytes] / 255f;
                                float G = Dval[x * PixelBytes + 1] / 255f;
                                float B = Dval[x * PixelBytes + 2] / 255f;
                                float A = PixelBytes > 3 ? Dval[x * PixelBytes + 3] / 255f : 1.0f;
                                //tex.SetPixel(x,y,new Color(R,G,B,A));//可以一个像素一个像素的设置颜色，也可以先读出来再一起设置颜色
                                colors[--index] = new Color(R, G, B, A);//解出来的图像是反着的，改Unity.Color的顺序
                            }
                        }
                        else if (f == TextureFormat.RGBAFloat)
                        {
                            for (int x = 0; x < ImageWidth * RowsPerStrip; x++)
                            {
                                float R = GetFloat(Dval, x * PixelBytes);
                                float G = GetFloat(Dval, x * PixelBytes + 4);
                                float B = GetFloat(Dval, x * PixelBytes + 8);
                                float A = GetFloat(Dval, x * PixelBytes + 12);
                                colors[--index] = new Color(R, G, B, A);
                            }
                        }
                    }
                }
                else if (Predictor == 2)//差值处理
                {
                    if (f == TextureFormat.RGBA32 && BitsPerSample[0] == 8)
                    {
                        for (int y = 0; y < StripOffsets.Count; y++)
                        {
                            pStrip = StripOffsets[y];//起始位置
                            size = StripByteCounts[y];//读取长度
                            byte[] Dval = CompressionLZW.Decode(data, pStrip, size);

                            for (int rows = 0; rows < RowsPerStrip; rows++)
                            {
                                byte[] last = new byte[4] { 0, 0, 0, 0 };
                                byte max = 255;
                                int start = ImageWidth * rows;
                                int end = ImageWidth * (rows + 1);
                                for (int x = start; x < end; x++)
                                {
                                    byte[] rgba = new byte[4] { 255, 255, 255, 255 };

                                    for (int channel = 0; channel < BitsPerSample.Count; channel++)
                                    {
                                        rgba[channel] = Dval[x * PixelBytes + channel];
                                        rgba[channel] += last[channel];
                                        last[channel] = rgba[channel];
                                    }
                                    colors[--index] = new Color(rgba[0] / 255f, rgba[1] / 255f, rgba[2] / 255f, rgba[3] / 255f);
                                }
                            }
                        }
                    }
                }
                else if (Predictor == 3)//浮点数的差值处理
                {
                    if (f == TextureFormat.RFloat)
                    {
                        for (int y = 0; y < StripOffsets.Count; y++)
                        {
                            pStrip = StripOffsets[y];//起始位置
                            size = StripByteCounts[y];//读取长度
                            byte[] Dval = CompressionLZW.Decode(data, pStrip, size);

                            for (int rows = 0; rows < RowsPerStrip; rows++)
                            {
                                float last = 0;
                                int start = ImageWidth * rows;
                                int end = ImageWidth * (rows + 1);
                                for (int x = start; x < end; x++)
                                {
                                    float r = 0f;
                                     //r = BitConverter.ToSingle(Dval, x * PixelBytes);
                                   r = GetFloat(Dval, x * PixelBytes);
                                    r += last;
                                    last = r;
                                    
                                    //colors[--index] = new Color(r,r,r);
                                        tex.SetPixel(x, y, new Color(r, r, r, 1f));//可以一个像素一个像素的设置颜色，也可以先读出来再一起设置颜色
                                    //colors[--index] = new Color(r, 1f, 1f, 1f);
                                }
                            }
                        }
                    }
                }
            
            }
            tex.SetPixels(colors);
            tex.Apply();
        }
        private byte[] DecompressLZW(byte[] val)
        {
            return val;
        }
        public void PrintInfo()
        {
            Debug.Log("ImageWidth: " + ImageWidth);
            Debug.Log("ImageLength: " + ImageLength);
            string tmp = "";
            for (int i = 0; i < BitsPerSample.Count; i++)
            {
                tmp += BitsPerSample[i];
                tmp += " ";
            }
            Debug.Log("BitsPerSample: " + tmp);
            Debug.Log("Compression: " + Compression);
            Debug.Log("PhotometricInterpretation: " + PhotometricInterpretation);
            tmp = "";
            for (int i = 0; i < StripOffsets.Count; i++)
            {
                tmp += StripOffsets[i];
                tmp += " ";
            }
            Debug.Log("StripOffsets: " + tmp);
            Debug.Log("RowsPerStrip: " + RowsPerStrip);
            tmp = "";
            for (int i = 0; i < StripByteCounts.Count; i++)
            {
                tmp += StripByteCounts[i];
                tmp += " ";
            }
            Debug.Log("StripByteCounts: " + tmp);
            Debug.Log("XResolution: " + XResolution);
            Debug.Log("YResolution: " + YResolution);
            Debug.Log("ResolutionUnit: " + ResolutionUnit);
            Debug.Log("Predictor: " + Predictor);
            tmp = "";
            for (int i = 0; i < SampleFormat.Count; i++)
            {
                tmp += SampleFormat[i];
                tmp += " ";
            }
            Debug.Log("SampleFormat: " + tmp);
        }
        private int GetInt(int startPos, int Length)//读负数会有问题
        {
            int value = 0;

            if (ByteOrder)// "II")
                for (int i = 0; i < Length; i++) value |= data[startPos + i] << i * 8;
            else // "MM")
                for (int i = 0; i < Length; i++) value |= data[startPos + Length - 1 - i] << i * 8;

            return value;
        }
        //private int GetInt(byte[] b, int startPos, int Length)
        //{
        //    int value = 0;

        //    if (ByteOrder)// "II")
        //        for (int i = 0; i < Length; i++) value |= b[startPos + i] << i * 8;
        //    else // "MM")
        //        for (int i = 0; i < Length; i++) value |= b[startPos + Length - 1 - i] << i * 8;

        //    return value;
        //}
        private float GetRational(int startPos)
        {
            int A = GetInt(startPos, 4);
            int B = GetInt(startPos + 4, 4);
            return A / B;
        }
        private float GetFloat(byte[] b, int startPos)
        {
            byte[] byteTemp;
            if (ByteOrder)// "II")
                byteTemp = new byte[] { b[startPos], b[startPos + 1], b[startPos + 2], b[startPos + 3] };
            else
                byteTemp = new byte[] { b[startPos + 3], b[startPos + 2], b[startPos + 1], b[startPos] };
            float fTemp = BitConverter.ToSingle(byteTemp, 0);
            return fTemp;
        }
        private string GetString(int startPos, int Length)
        {
            string tmp = "";
            for (int i = 0; i < Length; i++)
                tmp += (char)data[startPos];
            return tmp;
        }
        static private DType[] TypeArray = {
                new DType("???",0),
                new DType("byte",1), //8-bit unsigned integer
                new DType("ascii",1),//8-bit byte that contains a 7-bit ASCII code; the last byte must be NUL (binary zero)
                new DType("short",2),//16-bit (2-byte) unsigned integer.
                new DType("long",4),//32-bit (4-byte) unsigned integer.
                new DType("rational",8),//Two LONGs: the first represents the numerator of a fraction; the second, the denominator.
                new DType("sbyte",1),//An 8-bit signed (twos-complement) integer
                new DType("undefined",1),//An 8-bit byte that may contain anything, depending on the definition of the field
                new DType("sshort",1),//A 16-bit (2-byte) signed (twos-complement) integer.
                new DType("slong",1),// A 32-bit (4-byte) signed (twos-complement) integer.
                new DType("srational",1),//Two SLONG’s: the first represents the numerator of a fraction, the second the denominator.
                new DType("float",4),//Single precision (4-byte) IEEE format
                new DType("double",8)//Double precision (8-byte) IEEE format
                };
        struct DType
        {
            public DType(string n, int s)
            {
                name = n;
                size = s;
            }
            public string name;
            public int size;
        }

        //用来输出
        // static private string[] TagArray = {//254
        //         "NewSubfileType","SubfileType","ImageWidth","ImageLength","BitsPerSample","Compression",
        //         "260","261","PhotometricInterpretation","263","264","265","266","267","268","269",
        //         "270","271","272","StripOffsets","Orientation","275","276","SamplesPerPixel","RowsPerStrip","StripByteCounts",
        //         "280","281","XResolution","YResolution","PlanarConfig","285","286","287","288","289",
        //         "290","291","292","293","294","295","ResolutionUnit","297","298","299",
        //         "300","301","302","303","304","Software","DateTime","307","308","309",
        //         "310","311","312","313","314","Artist","316","Differencing Predictor","318","319",
        //         "ColorDistributionTable","321","322","323","324","325","326","327","328","329",
        //         "330","331","332","333","334","335","336","337","ExtraSamples","SampleFormat"
        //     //"330","331","332","333","334","335","336","337","338","339",
        //     };

        // static private string Tag(int i)
        // {
        //     if (i <= 339)
        //         return TagArray[i - 254];
        //     else
        //         return i.ToString();
        // }

    }
}

