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

        //Dictionary<ushort, HandleDE> handleDEs = new Dictionary<ushort, HandleDE>();

        Dictionary<int, HandlePredictor> handlePredictors = new Dictionary<int, HandlePredictor>();
        HandlePredictor handlePredictor;
        Dictionary<int, HandleCompression> handleCompressions = new Dictionary<int, HandleCompression>();
        HandleCompression handleCompression;
        HandleByteOrder[] handleByteOrders = new HandleByteOrder[2];
        Dictionary<int, HandleSample> handleSamples = new Dictionary<int, HandleSample>();
        HandleSample handleSample;
        Dictionary<int, HandleOrientation> handleOrientations = new Dictionary<int, HandleOrientation>();
        HandleOrientation handleOrientation;
        void InitFunctionArray()
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
            int sf = 1;
            if (SampleFormat.Count != 0)
                sf = SampleFormat.Value(0);//Default is 1

            //handleSample = handleSamples[SampleFormat[0]];
            if (sf == 1)
            {
                switch (_BytesPerSample)
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
            else if (sf == 2)
            {
                switch (_BytesPerSample)
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
            else if (sf == 3)
            {
                handleSample = SampleFloat;
            }
            else
            {
                throw new UnityException("暂不支持");//暂不支持
            }

            if (Predictor.Value == 2)
            {
                switch (_BytesPerSample)
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
                handlePredictor = handlePredictors[Predictor.Value];

            if (Predictor.Value == 3)
            {
                Debug.Log("不确定RowsPerStrip > 1的时候如何处理float predictor");
            }

            handleCompression = handleCompressions[Compression.Value];
            handleOrientation = handleOrientations[Orientation.Value];
        }
        public TIFF()
        {
            InitFunctionArray();
        }
        public Dictionary<ushort, DEBase> DEs = new Dictionary<ushort, DEBase>();
        public DEInt ImageWidth = new DEInt(0);
        public DEInt ImageLength = new DEInt(0);
        private DEIntArray BitsPerSample = new DEIntArray();
        private DEInt Compression = new DEInt(1);

        ///Summary///
        ///0 White is Zero
        ///1 Black is Zero
        ///
        private DEInt PhotometricInterpretation = new DEInt(0);
        private DEIntArray StripOffsets = new DEIntArray();
        private DEInt Orientation = new DEInt(1);
        private DEInt SamplesPerPixel = new DEInt(1);
        private DEInt RowsPerStrip = new DEInt(UInt16.MaxValue);

        private DEIntArray StripByteCounts = new DEIntArray();//For each strip, the number of bytes in that strip after any compression
        private DERational XResolution = new DERational();
        private DERational YResolution = new DERational();
        private DEInt PlanarConfiguration = new DEInt(1);
        private DEInt ResolutionUnit = new DEInt(2);
        private DEInt FillOrder = new DEInt(1);
        private DEString Software = new DEString();
        private DEString DateTime = new DEString();
        private DEString Artist = new DEString();
        private DEInt Predictor = new DEInt(1);
        private DEIntArray ColorMap = new DEIntArray();
        private DEIntArray ExtraSamples = new DEIntArray();
        private DEIntArray SampleFormat = new DEIntArray();//default is 1
        private DEDoubleArray ModelPixelScaleTag = new DEDoubleArray();
        private DEDoubleArray ModelTiepointTag = new DEDoubleArray(); //double 6*K
        private DEIntArray GeoKeyDirectoryTag = new DEIntArray(); // short N>=4
        private DEDoubleArray GeoDoubleParamsTag = new DEDoubleArray(); //double N
        private DEStringArray GeoAsciiParamsTag = new DEStringArray(); //ASCII N
        private DEStringArray GDAL_NODATA = new DEStringArray();// ASCII; //N

        private void InitDEs()
        {
            DEBase.data = data;
            //DEBase.ByteOrder = ByteOrder;
            if(ByteOrder==1)
            DEBase.byteToInt =DEBase.ByteOrderII;
            else
            DEBase.byteToInt =DEBase.ByteOrderMM;
            DEs.Add(256, ImageWidth);
            DEs.Add(257, ImageLength);
            DEs.Add(258, BitsPerSample);
            DEs.Add(259, Compression);
            DEs.Add(262, PhotometricInterpretation);
            DEs.Add(273, StripOffsets);
            DEs.Add(274, Orientation);
            DEs.Add(277, SamplesPerPixel);
            DEs.Add(278, RowsPerStrip);
            DEs.Add(279, StripByteCounts);
            DEs.Add(282, XResolution);
            DEs.Add(283, YResolution);
            DEs.Add(284, PlanarConfiguration);
            DEs.Add(296, ResolutionUnit);
            DEs.Add(305, Software);
            DEs.Add(306, DateTime);
            DEs.Add(315, Artist);//
            DEs.Add(317, Predictor);
            DEs.Add(320, ColorMap);
            DEs.Add(338, ExtraSamples);
            DEs.Add(339, SampleFormat);

            DEs.Add(33550, ModelPixelScaleTag);
            DEs.Add(33922, ModelTiepointTag); //double 6*K
            DEs.Add(34735, GeoKeyDirectoryTag); //short N>=4
            DEs.Add(34736, GeoDoubleParamsTag);// double N
            DEs.Add(34737, GeoAsciiParamsTag);// ASCII N
            DEs.Add(42113, GDAL_NODATA);// ASCII); N
        }
        private void PostProcessDEs()
        {

            int _BitsPerPixel = 0;
            for (int i = 0; i < BitsPerSample.Count; i++)
            {
                _BitsPerPixel += BitsPerSample.Value(i);
            }
            _BytesPerPixel = _BitsPerPixel / 8;
            if (BitsPerSample.Count != 0)
            {
                if (_BitsPerPixel / BitsPerSample.Count != BitsPerSample.Value(0))
                {
                    Debug.Log(_BitsPerPixel);
                     Debug.Log(BitsPerSample.Count);
                     Debug.Log(BitsPerSample.Value(0));
                    throw new UnityException("每个通道宽度不一致，暂不支持");
                }
                
                    
            }
            else
                throw new UnityException("BitsPerSample.Count==0");


            _BytesPerSample = BitsPerSample.Value(0) / 8;
        }
        //Unity Texture
        Texture2D tex;

        byte[] data;

        int ByteOrder;//true:II  false:MM



        // public int ImageWidth = 0;
        // public int ImageLength = 0;
        // public ulong PixelCount = 0;
        // public List<int> BitsPerSample = new List<int>();
        // public int BytesPerSample = 0;
        // public int SamplePerPixel = 0;
        // public bool SameBitsPerChannel = true;
        // public int _BytesPerPixel = 0;
        // public int Compression = 0;
        // public ulong StripLength = 0;//解压后的条带长度
        // /// <summary>
        // /// 0 = WhiteIsZero. For bilevel and grayscale images: 0 is imaged as white. The maximum value is imaged as black. This is the normal value for Compression=2.
        // /// 1 = BlackIsZero. For bilevel and grayscale images: 0 is imaged as black. The maximum value is imaged as white. If this value is specified for Compression=2, the
        // /// image should display and print reversed.
        // /// </summary>
        // public int PhotometricInterpretation = 0;


        // public int Orientation = 1;
        // /// <summary>
        // ///For each strip, the byte offset of that strip
        // /// </summary>
        // public List<int> StripOffsets = new List<int>();//For each strip, the byte offset of that strip

        // /// <summary>
        // ///The number of rows in each strip (except possibly the last strip.)
        // ///For example, if ImageLength is 24, and RowsPerStrip is 10, then there are 3
        // ///strips, with 10 rows in the first strip, 10 rows in the second strip, and 4 rows in the
        // ///third strip. (The data in the last strip is not padded with 6 extra rows of dummy data.)
        // /// </summary>
        // public int RowsPerStrip = 0;

        // /// <summary>
        // ///For each strip, the number of bytes in that strip after any compression.
        // /// </summary>
        // public List<int> StripByteCounts = new List<int>();
        // public float XResolution = 0f;
        // public float YResolution = 0f;

        // public int PlanarConfiguration = 1;
        // public int ResolutionUnit = 0;
        // public int Predictor = 0;

        // /// <summary>
        // //This field specifies how to interpret each data sample in a pixel. Possible values are:
        // // 1 = unsigned integer data
        // // 2 = two’s complement signed integer data
        // // 3 = IEEE floating point data [IEEE]
        // // 4 = undefined data format
        // /// </summary>
        // public List<int> SampleFormat = new List<int>();
        // public string DateTime = "";
        // public string Software = "";

        // //GeoTiff
        // public double[] ModelPixelScaleTag = new double[] { 0.0, 0.0, 0.0 };
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

            InitDEs();

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
            PostProcessDEs();
            //已获得每条扫描线位置，大小，压缩方式和数据类型，接下来进行解码
            FunctionArrayUpdate();
            DecodeStrips();
            int pNext = GetInt(n, 4);
            return pNext;
        }
        public void DecodeDE(ulong Pos)
        {
            ushort TagIndex = (ushort)GetInt(Pos, 2);
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
        int _BytesPerPixel = 0;
        int _BytesPerSample = 0;
        private void GetDEValue(ushort TagIndex, int TypeIndex, int Count, ulong pdata)
        {
            int typesize = TypeArray[TypeIndex].size;
            if (DEs.ContainsKey(TagIndex))
                DEs[TagIndex].SetValue(pdata, typesize, Count);
            else
                Debug.Log(TagIndex + " is not exist");



        }
        
        private void DecodeStrips()
        {
            if (ImageWidth.Value > 8192 || ImageLength.Value > 8192)
            {
                throw new UnityException("too Large for Unity");//暂不支持
            }
            tex = new Texture2D((int)ImageWidth.Value, (int)ImageLength.Value, TextureFormat.RGBAFloat, false);

            ulong _PixelCount = (ulong)ImageWidth.Value * (ulong)ImageLength.Value;
            Color[] colors = new Color[_PixelCount];
            //Debug.Log("PixelCount: " + _PixelCount);

            handleCompression(colors);

            tex.SetPixels(colors);
            tex.Apply();
        }
        private delegate void HandleCompression(Color[] dst);
        private void Compression1(Color[] colors)
        {//未测试
            for (int stripIndex = 0; stripIndex < StripOffsets.Count; stripIndex++)
            {
                int startPos = StripOffsets.Value(stripIndex);
                int byteCount = StripByteCounts.Value(stripIndex);
                int pixelCount = byteCount / _BytesPerPixel;
                int Channel = BitsPerSample.Count;

                for (int i = 0; i < pixelCount; i++)
                {
                    for (int c = 0; c < Channel; c++)
                    {
                        ulong index = (ulong)startPos + (ulong)(i * _BytesPerPixel + c * _BytesPerSample);
                        tempRGBA[c] = handleSample(data, index);
                    }
                    int x = i % ImageWidth.Value;
                    int y = stripIndex * RowsPerStrip.Value + i / ImageWidth.Value;
                    colors[handleOrientation(x, y)] = new Color(tempRGBA[0], tempRGBA[1], tempRGBA[2], tempRGBA[3]);
                }
            }
        }
        ulong DecodedStripLength = 0;
        System.Diagnostics.Stopwatch lzwTime = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch predictorTime = new System.Diagnostics.Stopwatch();
        private void Compression5(Color[] colors)
        {
            DecodedStripLength = (ulong)ImageWidth.Value * (ulong)RowsPerStrip.Value * (ulong)_BytesPerPixel;
            CompressionLZW.CreateBuffer(DecodedStripLength);
            for (int y = 0; y < StripOffsets.Count; y++)
            {
                lzwTime.Start();
                byte[] Dval = CompressionLZW.Decode(data, StripOffsets.Value(y), StripByteCounts.Value(y));//原始数据//起始位置//读取长度
                lzwTime.Stop();
                predictorTime.Start();
                handlePredictor(Dval, colors, y);
                predictorTime.Stop();
            }
            Debug.Log(string.Format("LZW使用时间 {0} ms", lzwTime.ElapsedMilliseconds));
            Debug.Log(string.Format("Predictor使用时间 {0} ms", predictorTime.ElapsedMilliseconds));
        }


        float[] tempRGBA = new float[] { 0f, 0f, 0f, 1f };
        private delegate void HandlePredictor(byte[] b, Color[] colors, int stripIndex);
        private void Predictor1(byte[] src, Color[] colors, int stripIndex)
        {
            int y = stripIndex * RowsPerStrip.Value;
            int rowsInThisStrip = Mathf.Min(ImageLength.Value - y, RowsPerStrip.Value);
            for (int rows = 0; rows < rowsInThisStrip; rows++)
            {
                int start = ImageWidth.Value * rows;
                int end = ImageWidth.Value * (rows + 1);
                for (int x = start; x < end; x++)
                {
                    for (int c = 0; c < BitsPerSample.Count; c++)
                    {
                        tempRGBA[c] = handleSample(src, (ulong)x * (ulong)_BytesPerPixel + (ulong)(c * _BytesPerSample));
                    }
                    colors[handleOrientation(x - start, y + rows)] = new Color(tempRGBA[0], tempRGBA[1], tempRGBA[2], tempRGBA[3]);
                }
            }
        }
        private void Predictor2UInt8(byte[] src, Color[] colors, int stripIndex)
        {
            int y = stripIndex * RowsPerStrip.Value;
            int rowsInThisStrip = Mathf.Min(ImageLength.Value - y, RowsPerStrip.Value);
            for (int rows = 0; rows < rowsInThisStrip; rows++)
            {
                byte[] RGBA = new byte[] { 0, 0, 0, 255 };
                int start = ImageWidth.Value * rows;
                int end = ImageWidth.Value * (rows + 1);
                for (int x = start; x < end; x++)
                {
                    for (int c = 0; c < BitsPerSample.Count; c++)
                    {
                        byte newc = SampleUInt8(src, (ulong)x * (ulong)_BytesPerPixel + (ulong)(c * _BytesPerSample));
                        RGBA[c] = (byte)((newc + RGBA[c]) & 0xff);
                    }
                    uint colindex = (uint)handleOrientation(x - start, y + rows);
                    colors[colindex] = new Color(RGBA[0] / 255f, RGBA[1] / 255f, RGBA[2] / 255f, RGBA[3] / 255f);
                }
            }
        }
        private void Predictor2UInt16(byte[] src, Color[] colors, int stripIndex)
        {
            int y = stripIndex * RowsPerStrip.Value;
            int rowsInThisStrip = Mathf.Min(ImageLength.Value - y, RowsPerStrip.Value);
            for (int rows = 0; rows < rowsInThisStrip; rows++)
            {
                UInt16[] RGBA = new UInt16[] { 0, 0, 0, UInt16.MaxValue };
                int start = ImageWidth.Value * rows;
                int end = ImageWidth.Value * (rows + 1);
                for (int x = start; x < end; x++)
                {
                    for (int c = 0; c < BitsPerSample.Count; c++)
                    {
                        UInt16 newc = SampleUInt16(src, (ulong)x * (ulong)_BytesPerPixel + (ulong)(c * _BytesPerSample));
                        RGBA[c] = (UInt16)((newc + RGBA[c]) & 0xffff);
                    }
                    colors[handleOrientation(x - start, y + rows)] = new Color(RGBA[0] / 65535f, RGBA[1] / 65535f, RGBA[2] / 65535f, RGBA[3] / 65535f);
                }
            }
        }
        private void Predictor2UInt32(byte[] src, Color[] colors, int stripIndex)
        {
            int y = stripIndex * RowsPerStrip.Value;
            int rowsInThisStrip = Mathf.Min(ImageLength.Value - y, RowsPerStrip.Value);
            float max = UInt32.MaxValue;
            for (int rows = 0; rows < rowsInThisStrip; rows++)
            {
                UInt32[] RGBA = new UInt32[] { 0, 0, 0, UInt32.MaxValue };
                int start = ImageWidth.Value * rows;
                int end = ImageWidth.Value * (rows + 1);
                for (int x = start; x < end; x++)
                {
                    for (int c = 0; c < BitsPerSample.Count; c++)
                    {
                        UInt32 newc = SampleUInt32(src, (ulong)x * (ulong)_BytesPerPixel + (ulong)(c * _BytesPerSample));
                        RGBA[c] = (UInt32)((newc + RGBA[c]) & 0xffff);
                    }
                    colors[handleOrientation(x - start, y + rows)] = new Color(RGBA[0] / max, RGBA[1] / max, RGBA[2] / max, RGBA[3] / max);
                }
            }
        }
        private void Predictor3(byte[] src, Color[] colors, int stripIndex)
        {
            // if(RowsPerStrip>1)
            //     throw new UnityException("不确定RowsPerStrip > 1的时候如何处理float predictor");

            // for (ulong i = 0; i < StripLength - (ulong)SamplePerPixel; i++)
            //     src[(ulong)SamplePerPixel + i] = (byte)((src[(ulong)SamplePerPixel + i] + src[i]) & 0xff);

            // byte[] dst = new byte[StripLength];
            // ulong len = (ulong)ImageWidth;// StripLength / BytesPerSample;
            // for (ulong i = 0, j = 0; i < StripLength; j++)
            // {
            //     dst[i++] = src[j + len * 3];
            //     dst[i++] = src[j + len * 2];
            //     dst[i++] = src[j + len];
            //     dst[i++] = src[j ];
            // }
            // for (int x = 0; x < ImageWidth * RowsPerStrip; x++)
            // {
            //     for (int c = 0; c < BitsPerSample.Count; c++)
            //     {
            //         tempRGBA[c] = handleSample(dst, (ulong)x * (ulong)_BytesPerPixel + (ulong)(c * BytesPerSample));
            //     }
            //     colors[handleOrientation(x, y)] = new Color(tempRGBA[0], tempRGBA[1], tempRGBA[2], tempRGBA[3]);
            // }
            ulong spp = (ulong)SamplesPerPixel.Value;

            int y = stripIndex * RowsPerStrip.Value;
            int rowsInThisStrip = Mathf.Min(ImageLength.Value - y, RowsPerStrip.Value);
            for (int rows = 0; rows < rowsInThisStrip; rows++)
            {
                ulong start = (ulong)ImageWidth.Value * (ulong)rows * (ulong)_BytesPerPixel;
                ulong end = (ulong)ImageWidth.Value * (ulong)(rows + 1) * (ulong)_BytesPerPixel;

                for (ulong i = start; i < end - (ulong)SamplesPerPixel.Value; i++)
                    src[(ulong)spp + i] = (byte)((src[(ulong)spp + i] + src[i]) & 0xff);

                byte[] dst = new byte[DecodedStripLength];
                ulong len = (ulong)ImageWidth.Value;// StripLength / BytesPerSample;
                for (ulong i = 0, j = start; i < end; j++)
                {
                    dst[i++] = src[j + len * 3];
                    dst[i++] = src[j + len * 2];
                    dst[i++] = src[j + len];
                    dst[i++] = src[j];
                }
                for (int x = 0; x < ImageWidth.Value * RowsPerStrip.Value; x++)
                {
                    for (int c = 0; c < BitsPerSample.Count; c++)
                    {
                        tempRGBA[c] = handleSample(dst, (ulong)x * (ulong)_BytesPerPixel + (ulong)(c * _BytesPerSample));
                    }
                    colors[handleOrientation(x, y)] = new Color(tempRGBA[0], tempRGBA[1], tempRGBA[2], tempRGBA[3]);
                }
            }
        }



        private delegate void HandleByteOrder(byte[] b, ulong startPos, int length);
        byte[] byteTemp = new byte[8];
        private void ByteOrderII(byte[] b, ulong startPos, int length)
        {
            for (int i = 0; i < length; i++)
                byteTemp[i] = b[startPos + (ulong)i];
        }
        private void ByteOrderMM(byte[] b, ulong startPos, int length)
        {
            for (int i = 0; i < length; i++)
                byteTemp[i] = b[startPos + (ulong)(length - 1 - i)];
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
            handleByteOrders[ByteOrder](b, startPos, _BytesPerSample);
            float fTemp = BitConverter.ToSingle(byteTemp, 0);
            return fTemp;
        }
        private byte SampleUInt8(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, _BytesPerSample);
            return byteTemp[0];
        }
        private UInt16 SampleUInt16(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, _BytesPerSample);
            return BitConverter.ToUInt16(byteTemp, 0);
        }
        private UInt32 SampleUInt32(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, _BytesPerSample);
            return BitConverter.ToUInt32(byteTemp, 0);
        }
        private char SampleInt8(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, _BytesPerSample);
            return (char)byteTemp[0];
        }
        private Int16 SampleInt16(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, _BytesPerSample);
            return BitConverter.ToInt16(byteTemp, 0);
        }
        private Int32 SampleInt32(byte[] b, ulong startPos)//不归一化
        {
            handleByteOrders[ByteOrder](b, startPos, _BytesPerSample);
            return BitConverter.ToInt32(byteTemp, 0);
        }

        private delegate ulong HandleOrientation(int src_x, int src_y);
        private ulong Orientation1(int src_x, int src_y)
        {
            ulong x = (ulong)src_x;
            ulong y = (ulong)(ImageLength.Value - 1 - src_y);
            return x + y * (ulong)ImageWidth.Value;

        }
        private ulong Orientation2(int src_x, int src_y)
        {
            ulong x = (ulong)(ImageWidth.Value - 1 - src_x);
            ulong y = (ulong)(ImageLength.Value - 1 - src_y);
            return x + y * (ulong)ImageWidth.Value;
        }
        private ulong Orientation3(int src_x, int src_y)
        {
            ulong x = (ulong)(ImageWidth.Value - 1 - src_x);
            ulong y = (ulong)src_y;
            return x + y * (ulong)ImageWidth.Value;
        }
        private ulong Orientation4(int src_x, int src_y)
        {
            ulong x = (ulong)src_x;
            ulong y = (ulong)src_y;
            return x + y * (ulong)ImageWidth.Value;
        }
        private ulong Orientation5(int src_x, int src_y)
        {
            ulong x = (ulong)src_x;
            ulong y = (ulong)(ImageLength.Value - 1 - src_y);
            return x * (ulong)ImageLength.Value + y;
        }
        private ulong Orientation6(int src_x, int src_y)
        {
            ulong x = (ulong)(ImageWidth.Value - 1 - src_x);
            ulong y = (ulong)(ImageLength.Value - 1 - src_y);
            return x * (ulong)ImageLength.Value + y;
        }
        private ulong Orientation7(int src_x, int src_y)
        {
            ulong x = (ulong)(ImageWidth.Value - 1 - src_x);
            ulong y = (ulong)src_y;
            return x * (ulong)ImageLength.Value + y;
        }
        private ulong Orientation8(int src_x, int src_y)
        {
            ulong x = (ulong)src_x;
            ulong y = (ulong)src_y;
            return x * (ulong)ImageLength.Value + y;
        }
        public void PrintInfo()
        {
            Debug.Log("ByteOrder: " + ByteOrder);
            Debug.Log("ImageWidth: " + ImageWidth.Value);
            Debug.Log("ImageLength: " + ImageLength.Value);
            string tmp = "";
            for (int i = 0; i < BitsPerSample.Count; i++)
            {
                tmp += BitsPerSample.Value(i);
                tmp += " ";
            }
            Debug.Log("BitsPerSample: " + tmp);
            Debug.Log("Compression: " + Compression.Value);
            Debug.Log("PhotometricInterpretation: " + PhotometricInterpretation.Value);
            // tmp = "";
            // for (int i = 0; i < StripOffsets.Count; i++)
            // {
            //     tmp += StripOffsets.Value(i);
            //     tmp += " ";
            // }
            // Debug.Log("StripOffsets: " + tmp);
            Debug.Log("RowsPerStrip: " + RowsPerStrip.Value);
            // tmp = "";
            // for (int i = 0; i < StripByteCounts.Count; i++)
            // {
            //     tmp += StripByteCounts.Value(i);
            //     tmp += " ";
            // }
            // Debug.Log("StripByteCounts: " + tmp);
            Debug.Log("XResolution: " + XResolution.Value);
            Debug.Log("YResolution: " + YResolution.Value);
            Debug.Log("ResolutionUnit: " + ResolutionUnit.Value);
            Debug.Log("Predictor: " + Predictor.Value);
            tmp = "";
            for (int i = 0; i < SampleFormat.Count; i++)
            {
                tmp += SampleFormat.Value(i);
                tmp += " ";
            }
            Debug.Log("SampleFormat: " + tmp);
            Debug.Log("PlanarConfiguration: " + PlanarConfiguration.Value);
            Debug.Log("SamplePerPixel: " + SamplesPerPixel.Value);

            // Debug.Log("ModelPixelScaleTag: " + ModelPixelScaleTag.Value(0) + " " +
            //        ModelPixelScaleTag.Value(1) + " " +
            //       ModelPixelScaleTag.Value(2));
            Debug.Log("Orientation: " + Orientation.Value);
        }
        private int GetInt(ulong startPos, int Length)//读负数会有问题
        {
            int value = 0;

            if (ByteOrder == 1)// "II")
                for (int i = 0; i < Length; i++) value |= data[startPos + (ulong)i] << i * 8;
            else // "MM")
                for (int i = 0; i < Length; i++) value |= data[startPos + (ulong)(Length - 1 - i)] << i * 8;

            return value;
        }

        private float GetRational(ulong startPos)
        {
            int A = GetInt(startPos, 4);
            int B = GetInt(startPos + 4, 4);
            return A / B;
        }
        private float GetFloat(byte[] b, ulong startPos)
        {
            handleByteOrders[ByteOrder](b, startPos, 4);
            float fTemp = BitConverter.ToSingle(byteTemp, 0);
            return fTemp;
        }
        private double GetDouble(ulong startPos)
        {
            handleByteOrders[ByteOrder](data, startPos, 8);
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

    }
}

