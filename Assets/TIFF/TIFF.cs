using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;

namespace OSC_TIFF
{
    //public class Image_Type
    //{
    //    TextureFormat uFormat;
    //    int samplerFormat;
    //    int samplerCount;
    //    int bitsPerSample;
    //    public Image_Type(int _SamplerCount, int _SamplerFormat,int _BitsPerSample, TextureFormat _uFormat)
    //    {
    //        uFormat = _uFormat;
    //        samplerFormat = _SamplerFormat;
    //        samplerCount = _SamplerCount;
    //        bitsPerSample = _BitsPerSample;
    //    }
    //}
    public class TIFF
    {
        public TIFF()
        {
            //List<Image_Type> image_Types = new List<Image_Type>();
            //image_Types.Add(new Image_Type(1, 1, 8, TextureFormat.R8));
            //image_Types.Add(new Image_Type(1, 1, 16, TextureFormat.R16));
            //image_Types.Add(new Image_Type(1, 3, 32, TextureFormat.RFloat));

            //image_Types.Add(new Image_Type(3, 1,  8, TextureFormat.RGB24));

            //image_Types.Add(new Image_Type(4, 1,  4, TextureFormat.RGBA4444));
            //image_Types.Add(new Image_Type(4, 1,  8, TextureFormat.RGBA32));
            //image_Types.Add(new Image_Type(4, 3, 16, TextureFormat.RGBAHalf));
            //image_Types.Add(new Image_Type(4, 3, 32, TextureFormat.RGBAFloat));


            FunctionArrayInit();
        }
        Dictionary<int, HandlePredictor> handlePredictors = new Dictionary<int, HandlePredictor>();
        HandlePredictor handlePredictor;
        Dictionary<int, HandleCompression> handleCompressions = new Dictionary<int, HandleCompression>();
        HandleCompression handleCompression;
        HandleByteOrder[] handleByteOrders = new HandleByteOrder[2];
        Dictionary<int, HandleSample> handleSamples=new Dictionary<int, HandleSample>();
        HandleSample handleSample;
        Dictionary<int, HandleOrientation> handleOrientations = new Dictionary<int, HandleOrientation>();
        HandleOrientation handleOrientation;
        void FunctionArrayInit()
        {
            handlePredictors.Add(1, Predictor1);
            handlePredictors.Add(3, Predictor3);

            handleCompressions.Add(1, Compression1);//No
            //handleCompressions.Add(2, Compression2);//CCITT
            handleCompressions.Add(5, Compression5);
            //handleCompressions.Add(32773, Compression32773);//PackBits

            handleByteOrders[1] = ByteOrderII;
            handleByteOrders[0] = ByteOrderMM;

            //handleSamples.Add(1, Sample1);
            //handleSamples.Add(2, Sample2);
            //handleSamples.Add(3, Sample3);

            handleOrientations.Add(1, Orientation1);
            handleOrientations.Add(2, Orientation2);
            handleOrientations.Add(3, Orientation3);
            handleOrientations.Add(4, Orientation4);
            handleOrientations.Add(5, Orientation5);
            handleOrientations.Add(6, Orientation6);
            handleOrientations.Add(7, Orientation7);
            handleOrientations.Add(8, Orientation8);
        }
        void FunctionArrayUpdate()
        {
            if(SampleFormat.Count==0)
                SampleFormat.Add(1);//Default is 1

            //handleSample = handleSamples[SampleFormat[0]];
            if (SampleFormat[0] == 1)
            {
                switch (BytesPerSample)
                {
                    case 1:
                        handleSample = SampleUInt8ToFloat; break;
                    case 2:
                        handleSample = SampleUInt16ToFloat; break;
                    case 4:
                        handleSample = SampleUInt32ToFloat; break;
                    default:
                        throw new UnityException("暂不支持");//暂不支持
                }
            }
            else if(SampleFormat[0] == 2)
            {
                switch (BytesPerSample)
                {
                    case 1:
                        handleSample = SampleInt8ToFloat; break;
                    case 2:
                        handleSample = SampleInt16ToFloat; break;
                    case 4:
                        handleSample = SampleInt32ToFloat; break;
                    default:
                        throw new UnityException("暂不支持");//暂不支持
                }
            }
            else if (SampleFormat[0] == 3)
            {
                handleSample = SampleFloat;
            }
            else
            {
                throw new UnityException("暂不支持");//暂不支持
            }

            if (Predictor==2)
            {
                switch(BytesPerSample)
                {
                    case 1:
                        handlePredictor = Predictor2UInt8; break;
                    case 2:
                        handlePredictor = Predictor2UInt16; break;
                    case 4:
                        handlePredictor = Predictor2UInt32; break;
                    default:
                        throw new UnityException("暂不支持");//暂不支持
                }
            }
            else
                handlePredictor = handlePredictors[Predictor];

            handleCompression = handleCompressions[Compression];
            handleOrientation = handleOrientations[Orientation];
        }
        
        //Unity Texture
        Texture2D tex;

        byte[] data;

        int ByteOrder;//true:II  false:MM

        public int ImageWidth = 0;
        public int ImageLength = 0;
        public ulong PixelCount = 0;
        public List<int> BitsPerSample = new List<int>();
        public int BytesPerSample = 0;
        public int SamplePerPixel = 0;
        public bool SameBitsPerChannel = true;
        public int PixelBytes = 0;
        public int Compression = 0;
        public ulong StripLength = 0;//解压后的条带长度
        /// <summary>
        /// 0 = WhiteIsZero. For bilevel and grayscale images: 0 is imaged as white. The maximum value is imaged as black. This is the normal value for Compression=2.
        /// 1 = BlackIsZero. For bilevel and grayscale images: 0 is imaged as black. The maximum value is imaged as white. If this value is specified for Compression=2, the
        /// image should display and print reversed.
        /// </summary>
        public int PhotometricInterpretation = 0;


        public int Orientation = 1;
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

        //GeoTiff
        public double[] ModelPixelScaleTag = new double[] { 0.0, 0.0, 0.0 };
        public void Init(string path)
        {
            data = File.ReadAllBytes(path);

            //首先解码文件头，获得编码方式是大端还是小端，以及第一个IFD的位置
            int pIFD = DecodeIFH();


            //然后解码第一个IFD，返回值是下一个IFD的地址
            while (pIFD != 0)
            {
                pIFD = DecodeIFD((ulong)pIFD);
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
                ByteOrder = 1;
            else if (byteOrder == "MM")
                ByteOrder = 2;
            else
                throw new UnityException("The order value is not II or MM.");

            int Version = GetInt(2, 2);

            if (Version != 42)
                throw new UnityException("Not TIFF.");

            return GetInt(4, 4);
        }
        public int DecodeIFD(ulong Pos)
        {
            ulong n = Pos;
            int DECount = GetInt(n, 2);
            n += 2;
            for (int i = 0; i < DECount; i++)
            {
                DecodeDE(n);
                n += 12;
            }
            //已获得每条扫描线位置，大小，压缩方式和数据类型，接下来进行解码
            FunctionArrayUpdate();
            DecodeStrips();
            int pNext = GetInt(n, 4);
            return pNext;
        }
        public void DecodeDE(ulong Pos)
        {
            int TagIndex = GetInt(Pos, 2);
            int TypeIndex = GetInt(Pos + 2, 2);
            int Count = GetInt(Pos + 4, 4);
            //Debug.Log("Tag: " + Tag(TagIndex) + " DataType: " + TypeArray[TypeIndex].name + " Count: " + Count);

            //先把找到数据的位置
            ulong pData = Pos + 8;
            int totalSize = TypeArray[TypeIndex].size * Count;
            if (totalSize > 4)
                pData = (ulong)GetInt(pData, 4);

            //再根据Tag把值读出来，保存起来
            GetDEValue(TagIndex, TypeIndex, Count, pData);
        }
        private void GetDEValue(int TagIndex, int TypeIndex, int Count, ulong pdata)
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
                    int temp = 0;
                    int count = -1;
                    for (int i = 0; i < Count; i++)
                    {
                        int v = GetInt(pdata + (ulong)(i * typesize), typesize);
                        BitsPerSample.Add(v);
                        PixelBytes += v / 8;
                        if (v != temp)
                            count++;
                        temp = v;
                    }
                    if (count > 0)//三个通道大小不一致
                    {
                        throw new UnityException("三个通道大小不一致，暂不支持");//暂不支持
                    }
                    SamplePerPixel = BitsPerSample.Count;
                    BytesPerSample = BitsPerSample[0] / 8;//不支持4-bits per channel
                    if (BitsPerSample[0] < 8)
                    {
                        throw new UnityException("单个通道小于8bits,暂不支持");//暂不支持
                    }
                    break;
                case 259: //Compression
                    Compression = GetInt(pdata, typesize); break;
                case 262: //PhotometricInterpretation //一般等于1，2基本不用，等于2就不解了
                    PhotometricInterpretation = GetInt(pdata, typesize);
                    if (PhotometricInterpretation == 3)
                        throw new UnityException("PhotometricInterpretation暂不支持");//暂不支持
                    break;
                case 273://StripOffsets
                    for (int i = 0; i < Count; i++)
                    {
                        int v = GetInt(pdata + (ulong)(i * typesize), typesize);
                        StripOffsets.Add(v);
                    }
                    break;
                case 274:
                    Orientation = GetInt(pdata, typesize);
                    break;//Orientation
                case 277:
                    SamplePerPixel = GetInt(pdata, typesize);
                    break;//SamplesPerPixel
                case 278://RowsPerStrip
                    RowsPerStrip = GetInt(pdata, typesize); break;
                case 279://StripByteCounts
                    for (int i = 0; i < Count; i++)
                    {
                        int v = GetInt(pdata + (ulong)(i * typesize), typesize);
                        StripByteCounts.Add(v);
                    }
                    break;
                case 282: //XResolution
                    XResolution = GetRational(pdata); break;
                case 283://YResolution
                    YResolution = GetRational(pdata); break;
                case 284:
                    PlannarConfiguration = GetInt(pdata, typesize);
                    if (PlannarConfiguration != 1)
                        throw new UnityException("PlannarConfiguration != 1 暂不支持");//暂不支持
                    break;//ExtraSamplesbreak;//PlanarConfig
                case 296://ResolutionUnit
                    ResolutionUnit = GetInt(pdata, typesize); break;
                case 305://Software
                    Software = GetString(pdata, typesize); break;
                case 306://DateTime
                    DateTime = GetString(pdata, typesize); break;
                case 315: break;//Artist
                case 317: //Differencing Predictor
                    Predictor = GetInt(pdata, typesize); break;
                case 320:
                    throw new UnityException("ColorMap暂不支持");//暂不支持
                    break;//ExtraSamplesbreak;//ColorMap
                case 338:
                    //throw new UnityException("ExtraSamples暂不支持");//暂不支持
                    break;//ExtraSamples
                case 339: //SampleFormat
                    for (int i = 0; i < Count; i++)
                    {
                        int v = GetInt(pdata + (ulong)(i * typesize), typesize);
                        SampleFormat.Add(v);
                    }
                    break;
                case 33550://ModelPixelScaleTag
                    if (typesize != 8)
                    {
                        throw new UnityException("ModelPixelScaleTag not Double暂不支持");//暂不支持
                    }
                    for (int i = 0; i < Count; i++)
                        ModelPixelScaleTag[i] = GetDouble(pdata + (ulong)(i * typesize));
                    break;
                case 33922://ModelTiepointTag double 6*K

                    break;
                case 34735://GeoKeyDirectoryTag short N>=4

                    break;
                case 34736://GeoDoubleParamsTag double N

                    break;
                case 34737://GeoAsciiParamsTag ASCII N

                    break;
                case 42113://GDAL_NODATA ASCII N
                    break;
                default:
                    Debug.LogError(TagIndex);
                    break;
            }
        }
        private void DecodeStrips()
        {
            if(ImageWidth>8192|| ImageLength> 8192)
            {
                throw new UnityException("too Large for Unity");//暂不支持
            }
            tex = new Texture2D((int)ImageWidth, (int)ImageLength, TextureFormat.RGBAFloat, false);
            
            PixelCount = (ulong)ImageWidth * (ulong)ImageLength;
            Color[] colors = new Color[PixelCount];

            handleCompression(colors);

            tex.SetPixels(colors);
            tex.Apply();
        }
        private delegate void HandleCompression(Color[] dst);
        private void Compression1(Color[] colors)
        {
            for (int y = 0; y < StripOffsets.Count; y++)
            {
                byte[] Dval = data.Skip(StripOffsets[y]).Take(StripByteCounts[y]).ToArray();
                for (int x = 0; x < ImageWidth * RowsPerStrip; x++)
                {
                    for (int c = 0; c < BitsPerSample.Count; c++)
                    {
                        tempRGBA[c] = handleSample(Dval, (ulong)(c * BytesPerSample));
                    }
                    //tex.SetPixel(x,y,new Color(R,G,B,A));//可以一个像素一个像素的设置颜色，也可以先读出来再一起设置颜色
                    colors[handleOrientation(x, y)] = new Color(tempRGBA[0], tempRGBA[1], tempRGBA[2], tempRGBA[3]);
                }
            }
        }
        private void Compression5(Color[] colors)
        {
            StripLength = (ulong)ImageWidth * (ulong)RowsPerStrip * (ulong)PixelBytes;
            CompressionLZW.CreateBuffer(StripLength);
            for (int y = 0; y < StripOffsets.Count; y++)
            {
                byte[] Dval = CompressionLZW.Decode(data, StripOffsets[y], StripByteCounts[y]);//原始数据//起始位置//读取长度
                handlePredictor(Dval,colors,y);
            }
        }


        float[] tempRGBA = new float[] { 0f, 0f, 0f, 1f };
        private delegate void HandlePredictor(byte[] b,Color[] colors,int stripIndex);
        private void Predictor1(byte[] src, Color[] colors, int stripIndex)
        {
            int y = stripIndex * RowsPerStrip;
            for (int rows = 0; rows < RowsPerStrip; rows++)
            {
                int start = ImageWidth * rows;
                int end = ImageWidth * (rows + 1);
                for (int x = start; x < end; x++)
                //for (int x = 0; x < ImageWidth * RowsPerStrip; x++)
                {
                    for (int c = 0; c < BitsPerSample.Count; c++)
                    {
                        tempRGBA[c] = handleSample(src, (ulong)x * (ulong)PixelBytes + (ulong)(c * BytesPerSample));
                    }
                    colors[handleOrientation(x - start, y + rows)] = new Color(tempRGBA[0], tempRGBA[1], tempRGBA[2], tempRGBA[3]);
                }
            }
        }
        private void Predictor2UInt8(byte[] src, Color[] colors, int stripIndex)
        {
            int y = stripIndex * RowsPerStrip;
            for (int rows = 0; rows < RowsPerStrip; rows++)
            {
                byte[] RGBA = new byte[] { 0, 0, 0, 255 };
                int start = ImageWidth * rows;
                int end = ImageWidth * (rows + 1);
                for (int x = start; x < end; x++)
                {
                    for (int c = 0; c < BitsPerSample.Count; c++)
                    {
                        byte newc = SampleUInt8(src, (ulong)x * (ulong)PixelBytes + (ulong)(c * BytesPerSample));
                        RGBA[c] = (byte)((newc+ RGBA[c]) & 0xff);
                    }
                    colors[handleOrientation(x-start, y+ rows)] = new Color(RGBA[0] / 255f, RGBA[1] / 255f, RGBA[2] / 255f, RGBA[3] / 255f);
                }
            }
        }
        private void Predictor2UInt16(byte[] src, Color[] colors, int stripIndex)
        {
            int y = stripIndex * RowsPerStrip;

            for (int rows = 0; rows < RowsPerStrip; rows++)
            {
                UInt16[] RGBA = new UInt16[] { 0, 0, 0, UInt16.MaxValue };
                int start = ImageWidth * rows;
                int end = ImageWidth * (rows + 1);
                for (int x = start; x < end; x++)
                {
                    for (int c = 0; c < BitsPerSample.Count; c++)
                    {
                        UInt16 newc = SampleUInt16(src, (ulong)x * (ulong)PixelBytes + (ulong)(c * BytesPerSample));
                        RGBA[c] = (UInt16)((newc + RGBA[c]) & 0xffff);
                    }
                    colors[handleOrientation(x - start, y + rows)] = new Color(RGBA[0] / 65535f, RGBA[1] / 65535f, RGBA[2] / 65535f, RGBA[3] / 65535f);
                }
            }
        }
        private void Predictor2UInt32(byte[] src, Color[] colors, int stripIndex)
        {
            int y = stripIndex * RowsPerStrip;
            float max = UInt32.MaxValue;
            for (int rows = 0; rows < RowsPerStrip; rows++)
            {
                UInt32[] RGBA = new UInt32[] { 0, 0, 0, UInt32.MaxValue };
                int start = ImageWidth * rows;
                int end = ImageWidth * (rows + 1);
                for (int x = start; x < end; x++)
                {
                    for (int c = 0; c < BitsPerSample.Count; c++)
                    {
                        UInt32 newc = SampleUInt32(src, (ulong)x * (ulong)PixelBytes + (ulong)(c * BytesPerSample));
                        RGBA[c] = (UInt32)((newc + RGBA[c]) & 0xffff);
                    }
                    colors[handleOrientation(x - start, y + rows)] = new Color(RGBA[0] / max, RGBA[1] / max, RGBA[2] / max, RGBA[3] / max);
                }
            }
        }
        private void Predictor3(byte[] src, Color[] colors, int y)
        {
            if(RowsPerStrip>1)
                throw new UnityException("不确定RowsPerStrip > 1的时候如何处理float predictor");

            for (ulong i = 0; i < StripLength - (ulong)SamplePerPixel; i++)
                src[(ulong)SamplePerPixel + i] = (byte)((src[(ulong)SamplePerPixel + i] + src[i]) & 0xff);

            byte[] dst = new byte[StripLength];
            ulong len = (ulong)ImageWidth;// StripLength / BytesPerSample;
            for (ulong i = 0, j = 0; i < StripLength; j++)
            {
                dst[i++] = src[j + len * 3];
                dst[i++] = src[j + len * 2];
                dst[i++] = src[j + len];
                dst[i++] = src[j ];
            }
            for (int x = 0; x < ImageWidth * RowsPerStrip; x++)
            {
                for (int c = 0; c < BitsPerSample.Count; c++)
                {
                    tempRGBA[c] = handleSample(dst, (ulong)x * (ulong)PixelBytes + (ulong)(c * BytesPerSample));
                }
                colors[handleOrientation(x, y)] = new Color(tempRGBA[0], tempRGBA[1], tempRGBA[2], tempRGBA[3]);
            }
            //return dst;
        }

        private delegate void HandleByteOrder(byte[] b, ulong startPos,int length);

        byte[] byteTemp = new byte[8];
        private void ByteOrderII(byte[] b, ulong startPos, int  length)
        {
            for (int i = 0; i < length; i++)
                byteTemp[i] = b[startPos+(ulong)i];
        }
        private void ByteOrderMM(byte[] b, ulong startPos,int  length)
        {
            for (int i = 0; i < length; i++)
                byteTemp[i] = b[startPos + (ulong)(length - 1 -i)];
        }


        private delegate float HandleSample(byte[] b, ulong startPos);
        private float SampleUInt8ToFloat(byte[] b, ulong startPos)//0~1
        {
            float x = 255f;
            uint value = SampleUInt8(b, startPos);
            return value / x;
        }
        private float SampleUInt16ToFloat(byte[] b, ulong startPos)//0~1
        {
            float x = UInt16.MaxValue;
            uint value = SampleUInt16(b, startPos);
            return value / x;
        }
        private float SampleUInt32ToFloat(byte[] b, ulong startPos)//0~1
        {
            float x = UInt32.MaxValue;
            uint value = SampleUInt32(b, startPos);
            return value / x;
        }
        private float SampleInt8ToFloat(byte[] b, ulong startPos)//-1~1
        {
            float x = 127f;
            uint value = SampleInt8(b, startPos);
            return value / x;
        }
        private float SampleInt16ToFloat(byte[] b, ulong startPos)//-1~1
        {
            float x = Int16.MaxValue;
            Int16 value = SampleInt16(b, startPos);
            return value / x;
        }
        private float SampleInt32ToFloat(byte[] b, ulong startPos)//-1~1
        {
            float x = Int32.MaxValue;
            Int32 value = SampleInt32(b, startPos);
            return value / x;
        }
        private float SampleFloat(byte[] b, ulong startPos)
        {
            handleByteOrders[ByteOrder](b, startPos,BytesPerSample); 
            float fTemp = BitConverter.ToSingle(byteTemp, 0);
            return fTemp;
        }
        private byte SampleUInt8(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, BytesPerSample);
            return byteTemp[0];
        }
        private UInt16 SampleUInt16(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, BytesPerSample);
            return BitConverter.ToUInt16(byteTemp, 0);
        }
        private UInt32 SampleUInt32(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, BytesPerSample);
            return BitConverter.ToUInt32(byteTemp, 0);
        }
        private char SampleInt8(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, BytesPerSample);
            return (char)byteTemp[0];
        }
        private Int16 SampleInt16(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, BytesPerSample);
            return BitConverter.ToInt16(byteTemp, 0);
        }
        private Int32 SampleInt32(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, BytesPerSample);
            return BitConverter.ToInt32(byteTemp, 0);
        }

        private delegate ulong HandleOrientation(int src_x, int src_y);
        private ulong Orientation1(int src_x, int src_y)
        {
            ulong x = (ulong)src_x;
            ulong y = (ulong)(ImageLength - 1 - src_y);
            return x + y * (ulong)ImageWidth;
            
        }
        private ulong Orientation2(int src_x, int src_y)
        {
            ulong x = (ulong)(ImageWidth - 1 - src_x);
            ulong y = (ulong)(ImageLength - 1 - src_y);
            return x + y * (ulong)ImageWidth;
        }
        private ulong Orientation3(int src_x, int src_y)
        {
            ulong x = (ulong)(ImageWidth - 1 - src_x);
            ulong y = (ulong)src_y;
            return x + y * (ulong)ImageWidth;
        }
        private ulong Orientation4(int src_x, int src_y)
        {
            ulong x = (ulong)src_x;
            ulong y = (ulong)src_y;
            return x + y * (ulong)ImageWidth;
        }
        private ulong Orientation5(int src_x, int src_y)
        {
            ulong x = (ulong)src_x;
            ulong y = (ulong)(ImageLength - 1 - src_y);
            return x * (ulong)ImageLength + y;
        }
        private ulong Orientation6(int src_x, int src_y)
        {
            ulong x = (ulong)(ImageWidth - 1 - src_x);
            ulong y = (ulong)(ImageLength - 1 - src_y);
            return x * (ulong)ImageLength + y;
        }
        private ulong Orientation7(int src_x, int src_y)
        {
            ulong x = (ulong)(ImageWidth - 1 - src_x);
            ulong y = (ulong)src_y;
            return x * (ulong)ImageLength + y;
        }
        private ulong Orientation8(int src_x, int src_y)
        {
            ulong x = (ulong)src_x;
            ulong y = (ulong)src_y;
            return x * (ulong)ImageLength + y;
        }
        public void PrintInfo()
        {
            Debug.Log("ByteOrder: " + ByteOrder);
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
            Debug.Log("PlannarConfiguration: " + PlannarConfiguration);
            Debug.Log("SamplePerPixel: " + SamplePerPixel);

            Debug.Log("ModelPixelScaleTag: " + ModelPixelScaleTag[0] + " " +
                   ModelPixelScaleTag[1] + " " +
                  ModelPixelScaleTag[2]);
            Debug.Log("Orientation: " + Orientation);
            
        }
        private int GetInt(ulong startPos, int Length)//读负数会有问题
        {
            int value = 0;

            if (ByteOrder==1)// "II")
                for (int i = 0; i < Length; i++) value |= data[startPos + (ulong)i] << i * 8;
            else // "MM")
                for (int i = 0; i < Length; i++) value |= data[startPos + (ulong)(Length - 1 - i)] << i * 8;

            return value;
        }

        //private float ByteArrayToIntColor(byte[] b, int startPos, int Length)
        //{
        //    int value = 0;

        //    if (ByteOrder)// "II")
        //        for (int i = 0; i < Length; i++) value |= b[startPos + i] << i * 8;
        //    else // "MM")
        //        for (int i = 0; i < Length; i++) value |= b[startPos + Length - 1 - i] << i * 8;

        //    return value/255f;
        //}
        //private float ByteArrayToFloatColor(byte[] b, int startPos, int Length)
        //{
        //    if (Length < 4)
        //    {
        //        throw new UnityException("浮点数不支持非 32 bits");
        //    }
                
        //    byte[] byteTemp;
        //    if (ByteOrder)// "II")
        //        byteTemp = new byte[] { b[startPos], b[startPos + 1], b[startPos + 2], b[startPos + 3] };
        //    else
        //        byteTemp = new byte[] { b[startPos + 3], b[startPos + 2], b[startPos + 1], b[startPos] };
        //    float fTemp = BitConverter.ToSingle(byteTemp, 0);

        //    return fTemp;
        //}
        //private float ByteArrayToFloatColorPredict(byte[] b, int startPos, int Length)
        //{
        //    if (Length < 4)
        //    {
        //        throw new UnityException("浮点数不支持非 32 bits");
        //    }

        //    byte[] byteTemp;
        //    if (!ByteOrder)// "II")
        //        byteTemp = new byte[] { b[startPos], b[startPos + ImageWidth], b[startPos + 2*ImageWidth], b[startPos + 3*ImageWidth] };
        //    else
        //        byteTemp = new byte[] { b[startPos + 3 * ImageWidth], b[startPos + 2 * ImageWidth], b[startPos + ImageWidth], b[startPos] };
        //    float fTemp = BitConverter.ToSingle(byteTemp, 0);

        //    return fTemp;
        //}
        //private int GetInt(byte[] b, int startPos, int Length)
        //{
        //    int value = 0;

        //    if (ByteOrder)// "II")
        //        for (int i = 0; i < Length; i++) value |= b[startPos + i] << i * 8;
        //    else // "MM")
        //        for (int i = 0; i < Length; i++) value |= b[startPos + Length - 1 - i] << i * 8;

        //    return value;
        //}
        private float GetRational(ulong startPos)
        {
            int A = GetInt(startPos, 4);
            int B = GetInt(startPos + 4, 4);
            return A / B;
        }
        private float GetFloat(byte[] b, ulong startPos)
        {
            handleByteOrders[ByteOrder](b, startPos,4);
            float fTemp = BitConverter.ToSingle(byteTemp, 0);
            return fTemp;
        }
        private double GetDouble(ulong startPos)
        {
            handleByteOrders[ByteOrder](data, startPos,8);
            double fTemp = BitConverter.ToDouble(byteTemp, 0);
            return fTemp;
        }
        private string GetString(ulong startPos, int Length)
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

