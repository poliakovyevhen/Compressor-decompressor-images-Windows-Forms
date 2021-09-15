using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.IO;
using System.Threading;


namespace WindowsFormsApp4
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            serialPort1.Open();
        }

        private void button1_Click(object sender, EventArgs e)
        {


            //часова затримка перед стартом стискання
            //Thread.Sleep(5000);


            // Розташування файлу де беремо зображення
            string path = "E:\\Yevhen\\Download chrome\\6 курс\\диплом\\visual studio\\try\\woman512.png";

            // Копресор
            // Час роботи
            DateTime startTime = DateTime.Now;
            wvCompress c = new wvCompress();
            byte[] compressed = c.run(path);
            // Вивід витраченого часу
            TimeSpan duration = DateTime.Now - startTime;
            //Console.Write("Compressed: ");
            label1.Text=duration.Seconds * 1000 + duration.Milliseconds + " мс";
            label5.Text = "Розмір до 2104КБ";
            label6.Text = "Розмір після к 58КБ";
            label7.Text = "Розмір після д 1978КБ";

            //Вивід стисненого у пнг
            Bitmap bitmap2 = BytesToBitmap(compressed);
            bitmap2.Save(path + "comp.png", ImageFormat.Png);

            // Декомпресор
            // Час роботи
            startTime = DateTime.Now;
            wvDecompress d = new wvDecompress();
            byte[] decompressed = d.run(compressed);
            duration = DateTime.Now - startTime;
            //Console.Write("Decompressed: ");
            label2.Text = duration.Seconds * 1000 + duration.Milliseconds + " мс";

            // Після декомпресора
            Bitmap bitmap1 = BytesToBitmap(decompressed);
            // Збереженя зображення
            bitmap1.Save(path + "decomp.png", ImageFormat.Png);

            // Сохранение в RAW без пост-сжатия (для того, чтобы можно было поэкспериментировать с пост-сжатием)
            //FileStream f = new System.IO.FileStream(path + ".bmp.raw", FileMode.Create, FileAccess.Write);
            //f.Write(decompressed, 0, decompressed.Length);
            //f.Close();

            string ret = Console.ReadLine();

        }

        public unsafe static Bitmap BytesToBitmap(byte[] data)
        {
            Size size = new System.Drawing.Size(512, 512);
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(size.Width, size.Height, size.Width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            handle.Free();
            return bmp;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.IO.FileStream fs = new System.IO.FileStream(@"E:\\Yevhen\\Download chrome\\6 курс\\диплом\\visual studio\\try\\woman512.png", System.IO.FileMode.Open);
            System.Drawing.Image img = System.Drawing.Image.FromStream(fs);
            fs.Close();
            pictureBox1.Image = img;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.IO.FileStream fs = new System.IO.FileStream(@"E:\\Yevhen\\Download chrome\\6 курс\\диплом\\visual studio\\try\\woman512.pngdecomp.png", System.IO.FileMode.Open);
            System.Drawing.Image img = System.Drawing.Image.FromStream(fs);
            fs.Close();
            pictureBox2.Image = img;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("Студент:" + "\r\n" +
                "Поляков Євген" + "\r\n" +
                "Група ЕС-607" + "\r\n" +
                "Кафедра електроніки" + "\r\n" +
                "ФАЕТ" + "\r\n" +
                "НАУ" + "\r\n" +
                "Україна, Київ",
                "Реквізит автора:",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
                );
        }

        private void button4_Click(object sender, EventArgs e)
        {
            serialPort1.Write("A");
            //Thread.Sleep(5000);
            //label2.Text = " ms";
            //Thread.Sleep(10000);
            //label1.Text = " ms";
        }
    }

    class wvDecompress
    {

        // Константи
        public const int WV_LEFT_TO_RIGHT = 0;
        public const int WV_TOP_TO_BOTTOM = 1;

        public byte[] run(byte[] compressed)
        {
            int z;
            int dwDepth = 6; // кількість рівней згортки вейвлета (чим їх більше, тим краще жметься)
            // розмір зображення
            int w = 512;
            int h = 512;
            // по суті размір зображення і ці коефіціенти повинні братись з заголовка(header) стисненого файлу
            int[] dwDiv = { 48, 32, 16, 16, 24, 24, 1, 1 }, dwTop = { 24, 32, 24, 24, 24, 24, 32, 32 };
            int SamplerDiv = 2, YPerec = 100, crPerec = 85, cbPerec = 85;

            double[,,] yuv = doUnPack(compressed, w, h, dwDepth);

            // Розгортка вейвлета
            for (z = 0; z < 2; z++)
            {
                for (int dWave = dwDepth - 1; dWave >= 0; dWave--)
                {
                    int w2 = Convert.ToInt32(w / Math.Pow(2, dWave));
                    int h2 = Convert.ToInt32(h / Math.Pow(2, dWave));
                    WaveleteUnPack(yuv, z, w2, h2, dwDiv[dWave] * SamplerDiv);
                }
            }
            z = 2;
            for (int dWave = dwDepth - 1; dWave >= 0; dWave--)
            {
                int w2 = Convert.ToInt32(w / Math.Pow(2, dWave));
                int h2 = Convert.ToInt32(h / Math.Pow(2, dWave));
                WaveleteUnPack(yuv, z, w2, h2, dwDiv[dWave]);
            }
            // YCrCb декодування + розкладання зображення у плоский масив
            byte[] rgb_flatened = this.YCrCbDecode(yuv, w, h, YPerec, crPerec, cbPerec);
            return rgb_flatened;
        }

        // Дана процедура є обратною процедурою DoPack у класі wvCompress.
        // Вона зворотньо переводить його в (short)double-тип з типа byte[]
        private static double[,,] doUnPack(byte[] Bytes, int cW, int cH, int dwDepth)
        {
            int lPos = 0;
            byte Value;
            int intIndex = 0;
            // розмір зображення в байтах
            int size = cW * cH * 3;
            // часовий масив для результируючих коефіціентів згорнутого вейвлета
            double[,,] ImgData = new double[3, cW, cH];

            int shortsLength = Bytes.Length - size;
            short[] shorts = new short[shortsLength / 2];
            Buffer.BlockCopy(Bytes, size, shorts, 0, shortsLength);

            for (int d = dwDepth - 1; d >= 0; d--)
            {
                int wSize = (int)Math.Pow(2, d);
                int W = cW / wSize;
                int H = cH / wSize;
                int w2 = W / 2;
                int h2 = H / 2;
                // лівий верхній
                if (d == dwDepth - 1)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        for (int j = 0; j < h2; j++)
                        {
                            for (int i = 0; i < w2; i++)
                            {
                                Value = Bytes[lPos++];
                                if (Value == 255)
                                {
                                    ImgData[z, i, j] = shorts[intIndex++];
                                }
                                else
                                {
                                    ImgData[z, i, j] = Value - 127;
                                }
                            }
                        }
                    }
                }
                // верхній правий + нижній правий
                for (int z = 0; z < 3; z++)
                {
                    for (int j = 0; j < H; j++)
                    {
                        for (int i = w2; i < W; i++)
                        {
                            Value = Bytes[lPos++];
                            if (Value == 255)
                            {
                                ImgData[z, i, j] = shorts[intIndex++];
                            }
                            else
                            {
                                ImgData[z, i, j] = Value - 127;
                            }
                        }
                    }
                }
                // лівий нижній
                for (int z = 0; z < 3; z++)
                {
                    for (int j = h2; j < H; j++)
                    {
                        for (int i = 0; i < w2; i++)
                        {
                            Value = Bytes[lPos++];
                            if (Value == 255)
                            {
                                ImgData[z, i, j] = shorts[intIndex++];
                            }
                            else
                            {
                                ImgData[z, i, j] = Value - 127;
                            }
                        }
                    }
                }
            }
            // повертаємо результат
            return ImgData;
        }

        // Функція розгортки вейвлета
        private void WaveleteUnPack(double[,,] ImgArray, int Component, int cW, int cH, int dwDevider)
        {
            int cw2 = cW / 2, ch2 = cH / 2;
            double dbDiv = 1f / dwDevider;
            // деквантування значень
            for (int i = 0; i < cW; i++)
            {
                for (int j = 0; j < cH; j++)
                {
                    if ((i >= cw2) || (j >= ch2))
                    {
                        if (ImgArray[Component, i, j] != 0)
                        {
                            ImgArray[Component, i, j] /= dbDiv;
                        }
                    }
                }
            }
            // Розгортка вейвлета
            for (int i = 0; i < cW; i++)
            {
                reWv(ref ImgArray, cH, Component, i, WV_LEFT_TO_RIGHT);
            }
            for (int j = 0; j < cH; j++)
            {
                reWv(ref ImgArray, cW, Component, j, WV_TOP_TO_BOTTOM);
            }
        }

        // Процедура зворотнього швидкого ліфтинга дискретного біортогонального CDF 9/7 вейвлета
        private void reWv(ref double[,,] shorts, int n, int z, int dwPos, int Side)
        {

            double a;
            double[] xWavelet = new double[n];
            double[] tempbank = new double[n];

            if (Side == WV_LEFT_TO_RIGHT)
            {
                for (int j = 0; j < n; j++)
                {
                    xWavelet[j] = shorts[z, dwPos, j];
                }
            }
            else if (Side == WV_TOP_TO_BOTTOM)
            {
                for (int i = 0; i < n; i++)
                {
                    xWavelet[i] = shorts[z, i, dwPos];
                }
            }

            for (int i = 0; i < n / 2; i++)
            {
                tempbank[i * 2] = xWavelet[i];
                tempbank[i * 2 + 1] = xWavelet[i + n / 2];
            }
            for (int i = 0; i < n; i++)
            {
                xWavelet[i] = tempbank[i];
            }

            // Undo scale
            a = 1.149604398f;
            for (int i = 0; i < n; i++)
            {
                if (i % 2 != 0)
                {
                    xWavelet[i] = xWavelet[i] * a;
                }
                else
                {
                    xWavelet[i] = xWavelet[i] / a;
                }
            }

            // Undo update 2
            a = -0.4435068522f;
            for (int i = 2; i < n; i += 2)
            {
                xWavelet[i] = xWavelet[i] + a * (xWavelet[i - 1] + xWavelet[i + 1]);
            }
            xWavelet[0] = xWavelet[0] + 2 * a * xWavelet[1];

            // Undo predict 2
            a = -0.8829110762f;
            for (int i = 1; i < n - 1; i += 2)
            {
                xWavelet[i] = xWavelet[i] + a * (xWavelet[i - 1] + xWavelet[i + 1]);
            }
            xWavelet[n - 1] = xWavelet[n - 1] + 2 * a * xWavelet[n - 2];

            // Undo update 1
            a = 0.05298011854f;
            for (int i = 2; i < n; i += 2)
            {
                xWavelet[i] = xWavelet[i] + a * (xWavelet[i - 1] + xWavelet[i + 1]);
            }
            xWavelet[0] = xWavelet[0] + 2 * a * xWavelet[1];

            // Undo predict 1
            a = 1.586134342f;
            for (int i = 1; i < n - 1; i += 2)
            {
                xWavelet[i] = xWavelet[i] + a * (xWavelet[i - 1] + xWavelet[i + 1]);
            }
            xWavelet[n - 1] = xWavelet[n - 1] + 2 * a * xWavelet[n - 2];

            if (Side == WV_LEFT_TO_RIGHT)
            {
                for (int j = 0; j < n; j++)
                {
                    shorts[z, dwPos, j] = xWavelet[j];
                }
            }
            else if (Side == WV_TOP_TO_BOTTOM)
            {
                for (int i = 0; i < n; i++)
                {
                    shorts[z, i, dwPos] = xWavelet[i];
                }
            }
        }

        // Метод перекодування YCrCb в RGB
        private byte[] YCrCbDecode(double[,,] yuv, int w, int h, double Ydiv, double Udiv, double Vdiv)
        {
            byte[] bytes_flat = new byte[3 * w * h];
            double vr, vg, vb;
            double vY, vCb, vCr;
            Ydiv = Ydiv / 100f;
            Udiv = Udiv / 100f;
            Vdiv = Vdiv / 100f;
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    vCr = yuv[0, i, j] / Vdiv;
                    vCb = yuv[1, i, j] / Udiv;
                    vY = yuv[2, i, j] / Ydiv;
                    vr = vY + 1.402f * (vCr - 128f);
                    vg = vY - 0.34414f * (vCb - 128f) - 0.71414f * (vCr - 128f);
                    vb = vY + 1.722f * (vCb - 128f);
                    if (vr > 255) { vr = 255; }
                    if (vg > 255) { vg = 255; }
                    if (vb > 255) { vb = 255; }
                    if (vr < 0) { vr = 0; }
                    if (vg < 0) { vg = 0; }
                    if (vb < 0) { vb = 0; }
                    bytes_flat[j * w * 3 + i * 3 + 0] = (byte)vb;
                    bytes_flat[j * w * 3 + i * 3 + 1] = (byte)vg;
                    bytes_flat[j * w * 3 + i * 3 + 2] = (byte)vr;
                }
            }
            return bytes_flat;
        }

    }

    class wvCompress
    {

        // Константи
        public const int WV_LEFT_TO_RIGHT = 0;
        public const int WV_TOP_TO_BOTTOM = 1;

        public byte[] run(string path)
        {

            // Завантажуємо зображення з файлу
            Bitmap bmp = new Bitmap(path, true);

            // Конвертируємо завантажене зображення в байтовий масив
            byte[,,] b = this.BmpToBytes_Unsafe(bmp);

            // Використання вейвлета
            byte[] o = this.Compress(b, bmp.Width, bmp.Height);

            // Збереження в RAW без пост-стиснення
            FileStream f = new System.IO.FileStream(path + ".raw", FileMode.Create, FileAccess.Write);
            f.Write(o, 0, o.Length);
            f.Close();

            // Стиснення отриманого масива звичайним Gzip-ом и збереження у файл
            // Якщо для стиснення використовувати що небуть інше замість GZIP, то можно отримати файл разміром ще в 2 рази меньше
            string outGZ = path + ".gz";
            FileStream outfile = new FileStream(outGZ, FileMode.Create);
            GZipStream compressedzipStream = new GZipStream(outfile, CompressionMode.Compress, true);
            compressedzipStream.Write(o, 0, o.Length);
            compressedzipStream.Close();

            // повертаємо нестиснений GZip-ом масив
            return o;

        }

        private byte[] Compress(byte[,,] rgb, int cW, int cH)
        {
            // Значення, для квантовання коефіціентів вейвлета
            int[] dwDiv = { 48, 32, 16, 16, 24, 24, 1, 1 };
            int[] dwTop = { 24, 32, 24, 24, 24, 24, 32, 32 };
            int SamplerDiv = 2, SamplerTop = 2;
            // Відсотки квантовання Y, cr, cb компонентів кольору
            int YPerec = 100, crPerec = 85, cbPerec = 85;
            int WVCount = 6; // кількість рівней згортки вейвлета
            // Перекодування RGB в YCrCb
            double[,,] YCrCb = YCrCbEncode(rgb, cW, cH, YPerec, crPerec, cbPerec, cW, cH);
            // Використовуємо вейвлет згортку послідовно до кажного кольорового каналу
            for (int z = 0; z < 3; z++)
            {
                // Кажен канал згортаємо вказану кількість разів
                for (int dWave = 0; dWave < WVCount; dWave++)
                {
                    int waveW = Convert.ToInt32(cW / Math.Pow(2, dWave));
                    int waveH = Convert.ToInt32(cH / Math.Pow(2, dWave));
                    if (z == 2)
                    {
                        // Канал з компонентом Y квантуємо на меньше значення,
                        // т.к. в ньому лежать структури зображень (яскрава компонента), а у інших каналах дані о кольорах
                        YCrCb = WaveletePack(YCrCb, z, waveW, waveH, dwDiv[dWave], dwTop[dWave], dWave);
                    }
                    else
                    {
                        YCrCb = WaveletePack(YCrCb, z, waveW, waveH, dwDiv[dWave] * SamplerDiv, dwTop[dWave] * SamplerTop, dWave);
                    }
                }
            }
            // конвертація масива в одномірний
            byte[] flattened = doPack(YCrCb, cW, cH, WVCount);
            return flattened;

        }

        /* Процедура пакує масив типа Double в масив типа Byte
        За рахунок наявності в масиві більшої кількості значень розміщених у межах байта.
        на початку усі Double доводяться до типу Short.
        Потім значення, які не влізли в тип байт дописуються у кінець вихідного потоку, а замість них в масив байтів
        записывается значение 255 */
        private byte[] doPack(double[,,] ImgData, int cW, int cH, int wDepth)
        {
            short Value;
            int lPos = 0;
            int size = cW * cH * 3;
            // резервування для short значень
            int intCount = 0;
            short[] shorts = new short[size];
            byte[] Ret = new byte[size];
            // прохід масива послідовно по вейвлет-рівням
            for (int d = wDepth - 1; d >= 0; d--)
            {
                int wSize = (int)Math.Pow(2f, Convert.ToDouble(d));
                int W = cW / wSize;
                int H = cH / wSize;
                int w2 = W / 2;
                int h2 = H / 2;
                // лівий верхній кут
                if (d == wDepth - 1)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        for (int j = 0; j < h2; j++)
                        {
                            for (int i = 0; i < w2; i++)
                            {
                                Value = (short)Math.Round(ImgData[z, i, j]);
                                if ((Value >= -127) && (Value <= 127))
                                {
                                    Ret[lPos++] = Convert.ToByte(Value + 127);
                                }
                                else
                                {
                                    Ret[lPos++] = 255;
                                    shorts[intCount++] = Value;
                                }
                            }
                        }
                    }
                }
                // правий верхній + правий нижній
                for (int z = 0; z < 3; z++)
                {
                    for (int j = 0; j < H; j++)
                    {
                        for (int i = w2; i < W; i++)
                        {
                            Value = (short)Math.Round(ImgData[z, i, j]);
                            if ((Value >= -127) && (Value <= 127))
                            {
                                Ret[lPos++] = Convert.ToByte(Value + 127);
                            }
                            else
                            {
                                Ret[lPos++] = 255;
                                shorts[intCount++] = Value;
                            }
                        }
                    }
                }
                // лівий нижній
                for (int z = 0; z < 3; z++)
                {
                    for (int j = h2; j < H; j++)
                    {
                        for (int i = 0; i < w2; i++)
                        {
                            Value = (short)Math.Round(ImgData[z, i, j]);
                            if ((Value >= -127) && (Value <= 127))
                            {
                                Ret[lPos++] = Convert.ToByte(Value + 127);
                            }
                            else
                            {
                                Ret[lPos++] = 255;
                                shorts[intCount++] = Value;
                            }
                        }
                    }
                }
            }
            // склеювання двох масивів (byte[] і short[]) в один
            int shortArraySize = intCount * 2;
            Array.Resize(ref Ret, Ret.Length + shortArraySize);
            Buffer.BlockCopy(shorts, 0, Ret, Ret.Length - shortArraySize, shortArraySize);
            // повертаємо результуючий плоский масив
            return Ret;
        }

        private double[,,] WaveletePack(double[,,] ImgArray, int Component, int cW, int cH, int dwDevider, int dwTop, int dwStep)
        {
            short Value;
            int cw2 = cW / 2;
            int cH2 = cH / 2;
            // підрахунок коефіціента квантовання
            double dbDiv = 1f / dwDevider;
            ImgArray = Wv(ImgArray, cW, cH, Component, WV_TOP_TO_BOTTOM);
            ImgArray = Wv(ImgArray, cH, cW, Component, WV_LEFT_TO_RIGHT);
            // квантовання
            for (int j = 0; j < cH; j++)
            {
                for (int i = 0; i < cW; i++)
                {
                    if ((i >= cw2) || (j >= cH2))
                    {
                        Value = (short)Math.Round(ImgArray[Component, i, j]);
                        if (Value != 0)
                        {
                            int value2 = Value;
                            if (value2 < 0) { value2 = -value2; }
                            if (value2 < dwTop)
                            {
                                ImgArray[Component, i, j] = 0;
                            }
                            else
                            {
                                ImgArray[Component, i, j] = Value * dbDiv;
                            }
                        }
                    }
                }
            }
            return ImgArray;
        }

        // Швидкий ліфтинг дискретного біортогонального CDF 9/7 вейвлета
        private double[,,] Wv(double[,,] ImgArray, int n, int dwCh, int Component, int Side)
        {

            double a;
            int i, j, n2 = n / 2;
            double[] xWavelet = new double[n];
            double[] tempbank = new double[n];

            for (int dwPos = 0; dwPos < dwCh; dwPos++)
            {
                if (Side == WV_LEFT_TO_RIGHT)
                {
                    for (j = 0; j < n; j++)
                    {
                        xWavelet[j] = ImgArray[Component, dwPos, j];
                    }
                }
                else if (Side == WV_TOP_TO_BOTTOM)
                {
                    for (i = 0; i < n; i++)
                    {
                        xWavelet[i] = ImgArray[Component, i, dwPos];
                    }
                }

                // Predict 1
                a = -1.586134342f;
                for (i = 1; i < n - 1; i += 2)
                {
                    xWavelet[i] += a * (xWavelet[i - 1] + xWavelet[i + 1]);
                }

                xWavelet[n - 1] += 2 * a * xWavelet[n - 2];

                // Update 1
                a = -0.05298011854f;
                for (i = 2; i < n; i += 2)
                {
                    xWavelet[i] += a * (xWavelet[i - 1] + xWavelet[i + 1]);
                }
                xWavelet[0] += 2 * a * xWavelet[1];

                // Predict 2
                a = 0.8829110762f;
                for (i = 1; i < n - 1; i += 2)
                {
                    xWavelet[i] += a * (xWavelet[i - 1] + xWavelet[i + 1]);
                }
                xWavelet[n - 1] += 2 * a * xWavelet[n - 2];

                // Update 2
                a = 0.4435068522f;
                for (i = 2; i < n; i += 2)
                {
                    xWavelet[i] += a * (xWavelet[i - 1] + xWavelet[i + 1]);
                }
                xWavelet[0] += 2 * a * xWavelet[1];

                // Scale
                a = 1f / 1.149604398f;
                j = 0;

                // множимо непарні на коефіціиент "а"
                // ділимо парні на коефіціент "а"
                if (Side == WV_LEFT_TO_RIGHT)
                {
                    for (i = 0; i < n2; i++)
                    {
                        ImgArray[Component, dwPos, i] = xWavelet[j++] / a;
                        ImgArray[Component, dwPos, n2 + i] = xWavelet[j++] * a;
                    }
                }
                else if (Side == WV_TOP_TO_BOTTOM)
                {
                    for (i = 0; i < n2; i++)
                    {
                        ImgArray[Component, i, dwPos] = xWavelet[j++] / a;
                        ImgArray[Component, n2 + i, dwPos] = xWavelet[j++] * a;
                    }
                }

            }
            return ImgArray;
        }

        // Метод перекодування RGB в YCrCb
        private double[,,] YCrCbEncode(byte[,,] BytesRGB, int cW, int cH, double Ydiv, double Udiv, double Vdiv, int oW, int oH)
        {
            double vr, vg, vb;
            double kr = 0.299, kg = 0.587, kb = 0.114, kr1 = -0.1687, kg1 = 0.3313, kb1 = 0.5, kr2 = 0.5, kg2 = 0.4187, kb2 = 0.0813;
            Ydiv = Ydiv / 100f;
            Udiv = Udiv / 100f;
            Vdiv = Vdiv / 100f;
            double[,,] YCrCb = new double[3, cW, cH];
            for (int j = 0; j < oH; j++)
            {
                for (int i = 0; i < oW; i++)
                {
                    vb = (double)BytesRGB[0, i, j];
                    vg = (double)BytesRGB[1, i, j];
                    vr = (double)BytesRGB[2, i, j];
                    YCrCb[2, i, j] = (kr * vr + kg * vg + kb * vb) * Ydiv;
                    YCrCb[1, i, j] = (kr1 * vr - kg1 * vg + kb1 * vb + 128) * Udiv;
                    YCrCb[0, i, j] = (kr2 * vr - kg2 * vg - kb2 * vb + 128) * Udiv;
                }
            }
            return YCrCb;
        }

        private unsafe byte[,,] BmpToBytes_Unsafe(Bitmap bmp)
        {
            BitmapData bData = bmp.LockBits(new Rectangle(new Point(), bmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            // кількість байтів у растровому зображенні
            int byteCount = bData.Stride * bmp.Height;
            byte[] bmpBytes = new byte[byteCount];
            Marshal.Copy(bData.Scan0, bmpBytes, 0, byteCount); // Скопіюйте заблоковані байти з пам'яті
            // не забудьте розблокувати растрове зображення !!
            bmp.UnlockBits(bData);
            byte[,,] ret = new byte[3, bmp.Width, bmp.Height];
            for (int z = 0; z < 3; z++)
            {
                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)
                    {
                        ret[z, i, j] = bmpBytes[j * bmp.Width * 3 + i * 3 + z];
                    }
                }
            }
            return ret;
        }

    }
    
}
