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
        void FunctionArrayInit()
        {
            handlePredictors.Add(1, Predictor1);
            handlePredictors.Add(2, Predictor2);
            handlePredictors.Add(3, Predictor3);

            handleCompressions.Add(1, Compression1);//No
            //handleCompressions.Add(2, Compression2);//CCITT
            handleCompressions.Add(5, Compression5);
            //handleCompressions.Add(32773, Compression32773);//PackBits

            handleByteOrders[1] = ByteOrderII;
            handleByteOrders[0] = ByteOrderMM;

            handleSamples.Add(1, Sample1);
            handleSamples.Add(2, Sample2);
            handleSamples.Add(3, Sample3);
        }
        void FunctionArrayUpdate()
        {
            handleSample = handleSamples[SampleFormat[0]];
            handlePredictor = handlePredictors[Predictor];
            handleCompression = handleCompressions[Compression];
        }
        
        //Unity Texture
        Texture2D tex;

        byte[] data;

        int ByteOrder;//true:II  false:MM

        public int ImageWidth = 0;
        public int ImageLength = 0;

        public List<int> BitsPerSample = new List<int>();
        public int BytesPerSample = 0;
        public int SamplePerPixel = 0;
        public bool SameBitsPerChannel = true;
        public int PixelBytes = 0;
        public int Compression = 0;
        public int StripLength = 0;//解压后的条带长度
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
            FunctionArrayUpdate();
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
                    int temp = 0;
                    int count = -1;
                    for (int i = 0; i < Count; i++)
                    {
                        int v = GetInt(pdata + i * typesize, typesize);
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
                        int v = GetInt(pdata + i * typesize, typesize);
                        StripOffsets.Add(v);
                    }
                    break;
                case 274: break;//Orientation
                case 277:
                    SamplePerPixel = GetInt(pdata, typesize);
                    break;//SamplesPerPixel
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
                        int v = GetInt(pdata + i * typesize, typesize);
                        SampleFormat.Add(v);
                    }
                    break;
                case 33550://ModelPixelScaleTag
                    if (typesize != 8)
                    {
                        throw new UnityException("ModelPixelScaleTag not Double暂不支持");//暂不支持
                    }
                    for (int i = 0; i < Count; i++)
                        ModelPixelScaleTag[i] = GetDouble(pdata + i * typesize);
                    break;
                case 33922://ModelTiepointTag double 6*K

                    break;
                case 34735://GeoKeyDirectionTag short >=4

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
                switch (SampleFormat[0])
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
            //tex = new Texture2D(ImageWidth, ImageLength, f, false);
            tex = new Texture2D(ImageWidth, ImageLength, TextureFormat.RGBAFloat, false);

            int PixelCount = ImageWidth * ImageLength;
            Color[] colors = new Color[PixelCount];
            int index = PixelCount;

            handleCompression(colors, ref index);

            tex.SetPixels(colors);
            tex.Apply();
        }
        private delegate void HandleCompression(Color[] dst, ref int index);
        private void Compression1(Color[] colors, ref int index)
        {
            for (int y = 0; y < StripOffsets.Count; y++)
            {
                byte[] Dval = data.Skip(StripOffsets[y]).Take(StripByteCounts[y]).ToArray();
                handlePredictor(colors, Dval, ref index);
            }
        }
        private void Compression5(Color[] colors, ref int index)
        {
            StripLength = ImageWidth * RowsPerStrip * PixelBytes;
            CompressionLZW.CreateBuffer(StripLength);
            for (int y = 0; y < StripOffsets.Count; y++)
            {
                byte[] Dval = CompressionLZW.Decode(data, StripOffsets[y], StripByteCounts[y]);//原始数据//起始位置//读取长度

                handlePredictor(colors, Dval, ref index);
            }
        }

        //private delegate float ByteArrayToColorChannel(byte[] b, int startPos, int Length);

        //private void ByteArrayToColorArray(Color[] dst, ref int index,  ByteArrayToColorChannel BACC)
        //{
        //    for (int y = 0; y < StripOffsets.Count; y++)
        //    {
        //        byte[] Dval = CompressionLZW.Decode(data, StripOffsets[y], StripByteCounts[y]);//原始数据//起始位置//读取长度

        //        handlePredictors[Predictor](dst, Dval, ref index, BACC);
        //    }
        //}


        private delegate void HandlePredictor(Color[] dst, byte[] b, ref int index);
        private void Predictor1(Color[] dst, byte[] src, ref int index)
        {
            float[] RGBA = new float[] { 0f, 0f, 0f, 1f };
            for (int x = 0; x < ImageWidth * RowsPerStrip; x++)
            {
                for (int c = 0; c < BitsPerSample.Count; c++)
                {
                    RGBA[c] = handleSample(src, x * PixelBytes + c * BytesPerSample);
                }
                //tex.SetPixel(x,y,new Color(R,G,B,A));//可以一个像素一个像素的设置颜色，也可以先读出来再一起设置颜色
                dst[--index] = new Color(RGBA[0], RGBA[1], RGBA[2], RGBA[3]);//解出来的图像是反着的，改Unity.Color的顺序
            }
        }
        private void Predictor2(Color[] dst, byte[] src, ref int index)
        {
            throw new UnityException("Predictor == 2暂不支持");//暂不支持
        }
        private void Predictor3(Color[] dst, byte[] src, ref int index)
        {
            for (int i = 0; i < StripLength - 1; i++)
                src[SamplePerPixel + i] = (byte)((src[SamplePerPixel + i] + src[i]) & 0xff);

            byte[] tarray = new byte[StripLength];
            int len = ImageLength;// StripLength / BytesPerSample;
            for (int i = 0, j = 0; i < StripLength; j++)
            {
                tarray[i++] = src[j];
                tarray[i++] = src[j + len];
                tarray[i++] = src[j + len * 2];
                tarray[i++] = src[j + len * 3];
            }

            for (int x = 0; x < ImageWidth * RowsPerStrip; x++)
            {
                float col = handleSample(tarray, x) * 2f;
                dst[--index] = new Color(col, col, col, 1f);
            }
        }

        private delegate void HandleByteOrder(byte[] b, int startPos,int length);

        byte[] byteTemp = new byte[8];
        private void ByteOrderII(byte[] b, int startPos, int  length)
        {
            for (int i = 0; i < length; i++)
                byteTemp[i] = b[startPos+i];
        }
        private void ByteOrderMM(byte[] b, int startPos,int  length)
        {
            for (int i = 0; i < length; i++)
                byteTemp[i] = b[startPos + length - 1 -i];
        }
        private delegate float HandleSample(byte[] b, int startPos);
        private float Sample1(byte[] b, int startPos)
        {
            uint value = 0;
            float x = 8f;
            handleByteOrders[ByteOrder](b,startPos, BytesPerSample);
            switch(BytesPerSample)
            {
                case 1:
                    value = byteTemp[0];
                    x = 255f;
                    break;
                case 2:
                    value = BitConverter.ToUInt16(byteTemp, 0);
                    x = 65536f;
                    break;
                case 4:
                    value = BitConverter.ToUInt32(byteTemp, 0);
                    x = 65536f;
                    break;
                default:
                    break;
            }
            return value / x;
        }
        private float Sample2(byte[] b, int startPos)//有符号归一化
        {
            int value = 0;
            float x = 8f;
            //handleByteOrders[ByteOrder](b, startPos);
            //switch (BytesPerSample)
            //{
            //    case 1:
            //        value = byteTemp[0];
            //        x = 255f;
            //        break;
            //    case 2:
            //        value = BitConverter.ToInt16(byteTemp, 0);
            //        x = 65536f;
            //        break;
            //    case 4:
            //        value = BitConverter.ToInt32(byteTemp, 0);
            //        x = 65536f;
            //        break;
            //    default:
            //        break;
            //}
            return value / x;
        }
        private float Sample3(byte[] b, int startPos)
        {
            handleByteOrders[1](b, startPos,BytesPerSample); 
            float fTemp = BitConverter.ToSingle(byteTemp, 0);
            return fTemp;
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
        }
        private int GetInt(int startPos, int Length)//读负数会有问题
        {
            int value = 0;

            if (ByteOrder==1)// "II")
                for (int i = 0; i < Length; i++) value |= data[startPos + i] << i * 8;
            else // "MM")
                for (int i = 0; i < Length; i++) value |= data[startPos + Length - 1 - i] << i * 8;

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
        private float GetRational(int startPos)
        {
            int A = GetInt(startPos, 4);
            int B = GetInt(startPos + 4, 4);
            return A / B;
        }
        private float GetFloat(byte[] b, int startPos)
        {
            handleByteOrders[ByteOrder](b, startPos,4);
            float fTemp = BitConverter.ToSingle(byteTemp, 0);
            return fTemp;
        }
        private double GetDouble(int startPos)
        {
            handleByteOrders[ByteOrder](data, startPos,8);
            double fTemp = BitConverter.ToDouble(byteTemp, 0);
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

