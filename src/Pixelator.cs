// Name: Pixelator
// Submenu: Distort
// Author: datb
// Title: Pixelator
// Version: 1.0
// Desc: Convert image to pixel art
// Keywords: dithering|quantisation
// URL: github.com/davidatbailey

// Paint.NET settings UI
#region UICode
CheckboxControl m_flatten = false; // Flatten
CheckboxControl m_pixelate = true; // Pixelate
CheckboxControl m_smooth = false; // {m_pixelate} Antialias
IntSliderControl m_pixelate_percent = 25; // [5,100] {m_pixelate} Pixelate quality %
IntSliderControl m_pixelate_nudge_xy = 0; // [0,4] {m_pixelate} Resample
ListBoxControl m_palette = 0; // Colour palette|WPlace|Gameboy|8 colours|27 colours|MSDOS|Commodore 64|RISC OS|Auto (smart)|Auto (balanced)
IntSliderControl m_col_count = 6; // [2,512] Number of colours
IntSliderControl m_brightness = 0; // [-255,255,5] Brightness
IntSliderControl m_contrast = 0; // [-255,255] Contrast
ListBoxControl m_dither = 1; // Dither style|None|Normal|Photo
#endregion

enum ditherEnum
{
    NONE = 0,
    NORMAL,
    PHOTO,
}

enum paletteEnum
{
    WPLACE = 0,
    GAMEBOY,
    COLOURS_8,
    COLOURS_27,
    MSDOS,
    COMMODORE64,
    RISCOS,
    KMEANS,
    AUTOMATIC
}

public static double clamp(double val, double min, double max)
{
    if (val < min)
        val = min;
    if (val > max)
        val = max;
    return val;
}

struct DoubleRGB
{
    public double R, G, B;
    
    public static double IntToDouble(int val) { return (double)clamp(val / 255.0, 0.0, 255.0); }
    public static double UintToDouble(uint val) { return (double)clamp(val / 255.0, 0.0, 255.0); }
    public static double ByteToDouble(byte val) { return (byte)clamp(val / 255.0, 0.0, 255.0); }
    public static uint DoubleToUint(double val) { return (uint)clamp(val * 255.0, 0.0, 255.0); }
    public static byte DoubleToByte(double val) { return (byte)clamp(val * 255.0, 0.0, 255.0); }
    public static DoubleRGB Make(uint bgra = 0)
    {
        DoubleRGB result = new DoubleRGB();
        result.B = UintToDouble(bgra & 0xFF);
        result.G = UintToDouble((bgra >> 8) & 0xFF);
        result.R = UintToDouble((bgra >> 16) & 0xFF);
        return result;
    }
    public ColorBgra GetBgra()
    {
        ColorBgra result = new ColorBgra();
        result.B = DoubleToByte(B);
        result.G = DoubleToByte(G);
        result.R = DoubleToByte(R);
        result.A = 0xFF;
        return result;
    }
    public DoubleRGB(double r = 0, double g = 0, double b = 0)
    {
        R = r;
        G = g;
        B = b;
    }
    public DoubleRGB(uint bgra) { this = Make(bgra); }
    
    public static DoubleRGB operator +(DoubleRGB a, double val)
    {
        return new DoubleRGB(a.R + val, a.G + val, a.B + val);
    }
    public static DoubleRGB operator -(DoubleRGB a, double val)
    {
        return new DoubleRGB(a.R - val, a.G - val, a.B - val);
    }
    public static DoubleRGB operator *(DoubleRGB a, double val)
    {
        return new DoubleRGB(a.R * val, a.G * val, a.B * val);
    }
    public static DoubleRGB operator /(DoubleRGB a, double val)
    {
        return new DoubleRGB(a.R / val, a.G / val, a.B / val);
    }

    public static DoubleRGB operator +(DoubleRGB a, DoubleRGB b)
    {
        return new DoubleRGB(a.R + b.R, a.G + b.G, a.B + b.B);
    }
    public static DoubleRGB operator -(DoubleRGB a, DoubleRGB b)
    {
        return new DoubleRGB(a.R - b.R, a.G - b.G, a.B - b.B);  
    }
    public static DoubleRGB operator *(DoubleRGB a, DoubleRGB b)
    {
        return new DoubleRGB(a.R * b.R, a.G * b.G, a.B * b.B);
    }
    
    public DoubleRGB ClampDoubleRGB(double min, double max)
    {
        return new DoubleRGB(
          clamp(this.R, min, max),
          clamp(this.G, min, max),
          clamp(this.B, min, max)
        );
    }
}

static DoubleRGB _Colcor(DoubleRGB src, double brightness, double contrast)
{
    src *= 255.0;
    src += brightness;
    double factor = (259.0 * (contrast + 255.0)) / (255.0 * (259.0 - contrast));
    src -= 127.0;
    src *= factor;
    src += 127.0;
    src *= 1.0 / 255.0;
    src = src.ClampDoubleRGB(0.0, 1.0);
    return src;
}

// raster image
class Img
{
    public int Width = 0;
    public int Height = 0;
    public DoubleRGB[] Buffer = null;
    public Img() { }
    public Img(Surface src)
    {
        Width = src.Width;
        Height = src.Height;
        Buffer = new DoubleRGB[Width * Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                FastSet(x, y, DoubleRGB.Make(src[x, y].Bgra));
            }
    }

    // destructor
    ~Img() { Buffer = null; }
    
    public Img MakeCopy()
    {
        Img ret = new Img();
        ret.Width = Width;
        ret.Height = Height;
        ret.Buffer = new DoubleRGB[Width * Height];
        for (int i = 0; i < Width * Height; ++i)
            ret.Buffer[i] = Buffer[i];
        return ret;
    }
    
    public void FastRead(Img src)
    {
        Buffer = null;
        Width = src.Width;
        Height = src.Height;
        Buffer = new DoubleRGB[Width * Height];
        for (int i = 0; i < Width * Height; ++i)
            Buffer[i] = src.Buffer[i];
    }
    public void FastSet(int x, int y, DoubleRGB col)
    { Buffer[y * Width + x] = col; }
    public DoubleRGB FastGet(int x, int y)
    { return Buffer[y * Width + x]; }
    public DoubleRGB FastGet(int i)
    { return Buffer[i]; }

    // write buffer to Paint.NET canvas
    public void Write(Surface dst)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                dst[x, y] = FastGet(x, y).GetBgra();
    }
    public DoubleRGB Get(int x, int y)
    {
        if (x < 0) x = 0;
        if (x >= Width) x = Width - 1;
        if (y < 0) y = 0;
        if (y >= Height) y = Height - 1;
        return Buffer[y * Width + x];
    }
    public void Set(int x, int y, DoubleRGB col)
    {
        if (x < 0) x = 0;
        if (x >= Width) x = Width - 1;
        if (y < 0) y = 0;
        if (y >= Height) y = Height - 1;
        Buffer[y * Width + x] = col;
    }

    // apply colour correction settings
    public void Colcor(double add, double contrast)
    {
        // use parallel processing for large buffers
        if (Width * Height > 100000)
        {
            System.Threading.Tasks.Parallel.For(0, Width * Height, i =>
            {
                Buffer[i] = _Colcor(Buffer[i], add, contrast);
            });
        }
        else
        {
            for (int i = 0; i < Width * Height; ++i)
                Buffer[i] = _Colcor(Buffer[i], add, contrast);
        }
    }

    public void MakePixelScale(int scale, int pixelScaleXOffset, int pixelScaleYOffset)
    {
        Img buffer = this.MakeCopy();
        int xRemainder;
        int yRemainder;
        for (int y = pixelScaleYOffset; y < Height; y += scale)
        {
            if (y + scale > Height)
                yRemainder = Height - y;
            else
                yRemainder = scale;

            for (int x = pixelScaleXOffset; x < Width; x += scale)
            {
                if (x + scale > Width)
                    xRemainder = Width - x;
                else
                    xRemainder = scale;

                System.Collections.Generic.Dictionary<DoubleRGB, int> usedColours = new();
                for (int yy = 0; yy < yRemainder; ++yy)
                {
                    for (int xx = 0; xx < xRemainder; ++xx)
                    {
                        DoubleRGB currentPixel = buffer.FastGet(x + xx, y + yy);
                        if (!usedColours.ContainsKey(currentPixel))
                            usedColours[currentPixel] = 0;
                        usedColours[currentPixel] += 1;
                    }
                }

                DoubleRGB mostUsedKey = new();
                int mostUsedSize = 0;

                foreach (var myColour in usedColours)
                    if (myColour.Value > mostUsedSize)
                    {
                        mostUsedSize = myColour.Value;
                        mostUsedKey = myColour.Key;
                    }

                for (int yy = 0; yy < yRemainder; ++yy)
                    for (int xx = 0; xx < xRemainder; ++xx)
                        this.FastSet(x + xx, y + yy, mostUsedKey);
            }
        }
    }
}

class Palette
{
    public DoubleRGB[] cols = null;
    public int max = 0;
}

double Square(double val) { return val * val; }

uint Hex2Upper(char c)
{
    if (c >= '0' && c <= '9')
        return (uint)(c - '0');
    if (c >= 'a' && c <= 'f')
        return (uint)(c - 'a' + 10);
    if (c >= 'A' && c <= 'F')
        return (uint)(c - 'A' + 10);
    return 0;
}

DoubleRGB Str2Col(string str)
{
    if (str == "" || str.Length < 8)
        return new DoubleRGB();
    uint bgra = 0;
    foreach (char ch in str)
    {
        bgra <<= 4;
        bgra |= Hex2Upper(ch);
    }
    return new DoubleRGB(bgra);
}

DoubleRGB ColourFindClosest(DoubleRGB src, Palette pal)
{
    int bestColour = 0;
    double fMin = 100.0;
    for (int i = 0; i < pal.max; ++i)
    {
        double fi = Math.Sqrt(
          Square(pal.cols[i].B - src.B) +
          Square(pal.cols[i].G - src.G) +
          Square(pal.cols[i].R - src.R));
        if (fi < fMin)
        {
            bestColour = i;
            fMin = fi;
        }
    }
    return pal.cols[bestColour];
}

DoubleRGB ColourDifference(DoubleRGB src, Palette pal)
{
    double total = 3.0;
    int index = 0;
    for (int i = 0; i < pal.max; ++i)
    {
        double diff =
          Math.Abs(pal.cols[i].B - src.B) +
          Math.Abs(pal.cols[i].G - src.G) +
          Math.Abs(pal.cols[i].R - src.R);
        if (total > diff)
        {
            total = diff;
            index = i;
        }
    }
    return pal.cols[index];
}

DoubleRGB FindAverageColour(DoubleRGB src, Palette pal)
{
    double total = 1.0;
    int index = 0;
    for (int i = 0; i < pal.max; ++i)
    {
        var a = (pal.cols[i].B + pal.cols[i].G + pal.cols[i].R) / 3.0;
        var b = (src.B + src.G + src.R) / 3.0;
        double diff = Math.Abs(a - b);
        if (total > diff)
        {
            total = diff;
            index = i;
        }
    }
    return pal.cols[index];
}

class ByIntensity : IComparer<DoubleRGB>
{
    public int Compare(DoubleRGB a, DoubleRGB b)
    {
        double ia = (a.R + a.G + a.B) / 3.0;
        double ib = (b.R + b.G + b.B) / 3.0;
        if (ia == ib) return 0;
        else if (ia < ib) return -1;
        else return 1;
    }
}

double SaturationOf(DoubleRGB c)
{
    double mx = Math.Max(c.R, Math.Max(c.G, c.B));
    double mn = Math.Min(c.R, Math.Min(c.G, c.B));
    if (mx <= 0.0) return 0.0;
    return (mx - mn) / mx;
}

void KmeansFromSamples(Palette pal, List<DoubleRGB> samples, int colourCount)
{
    pal.max = colourCount;
    pal.cols = new DoubleRGB[colourCount];

    int k = colourCount;
    int n = samples.Count;
    if (n == 0) return;

    var centers = new DoubleRGB[k];
    var rnd = new Random(0);

    centers[0] = samples[rnd.Next(n)];
    var dist = new double[n];
    for (int i = 0; i < n; ++i)
    {
        double dr = samples[i].R - centers[0].R;
        double dg = samples[i].G - centers[0].G;
        double db = samples[i].B - centers[0].B;
        dist[i] = dr * dr + dg * dg + db * db;
    }
    for (int ci = 1; ci < k; ++ci)
    {
        double sum = 0;
        for (int i = 0; i < n; ++i) sum += dist[i];
        double pick = rnd.NextDouble() * sum;
        double acc = 0;
        int chosen = 0;
        for (int i = 0; i < n; ++i)
        {
            acc += dist[i];
            if (acc >= pick) { chosen = i; break; }
        }
        centers[ci] = samples[chosen];
        for (int i = 0; i < n; ++i)
        {
            double dr = samples[i].R - centers[ci].R;
            double dg = samples[i].G - centers[ci].G;
            double db = samples[i].B - centers[ci].B;
            double d = dr * dr + dg * dg + db * db;
            if (d < dist[i]) dist[i] = d;
        }
    }

    int[] assignments = new int[n];
    double[][] sums = new double[k][];
    int[] counts = new int[k];
    for (int ci = 0; ci < k; ++ci) { sums[ci] = new double[3]; counts[ci] = 0; }

    int MAXITER = 20;
    for (int iter = 0; iter < MAXITER; ++iter)
    {
        bool changed = false;

        // Assignment step
        for (int i = 0; i < n; ++i)
        {
            double bestD = double.MaxValue;
            int best = 0;
            for (int ci = 0; ci < k; ++ci)
            {
                double dr = samples[i].R - centers[ci].R;
                double dg = samples[i].G - centers[ci].G;
                double db = samples[i].B - centers[ci].B;
                double d = dr * dr + dg * dg + db * db;
                if (d < bestD) { bestD = d; best = ci; }
            }
            if (assignments[i] != best) { assignments[i] = best; changed = true; }
        }

        if (!changed) break;

        for (int ci = 0; ci < k; ++ci) { sums[ci][0] = sums[ci][1] = sums[ci][2] = 0.0; counts[ci] = 0; }
        for (int i = 0; i < n; ++i)
        {
            int a = assignments[i];
            sums[a][0] += samples[i].R;
            sums[a][1] += samples[i].G;
            sums[a][2] += samples[i].B;
            counts[a]++;
        }
        for (int ci = 0; ci < k; ++ci)
        {
            if (counts[ci] > 0)
            {
                centers[ci].R = sums[ci][0] / counts[ci];
                centers[ci].G = sums[ci][1] / counts[ci];
                centers[ci].B = sums[ci][2] / counts[ci];
            }
            else
            {
                centers[ci] = samples[rnd.Next(n)];
            }
        }
    }

    for (int ci = 0; ci < k; ++ci) pal.cols[ci] = centers[ci];
}

void AutomaticKmeansPalette(Palette pal, Img img, int colourCount = 16)
{
    // Sample pixels (like kmeans_palette) but filter out low-saturation pixels before clustering
    int totalPixels = img.Width * img.Height;
    int maxSamples = Math.Min(totalPixels, 200000);
    var samples = new List<DoubleRGB>(maxSamples);
    if (maxSamples == totalPixels)
    {
        for (int i = 0; i < totalPixels; ++i) samples.Add(img.FastGet(i));
    }
    else
    {
        int stride = Math.Max(1, totalPixels / maxSamples);
        for (int i = 0; i < totalPixels; i += stride) samples.Add(img.FastGet(i));
    }

    int n = samples.Count;
    if (n == 0) { pal.max = colourCount; pal.cols = new DoubleRGB[colourCount]; return; }

    var sats = new double[n];
    for (int i = 0; i < n; ++i) sats[i] = SaturationOf(samples[i]);

    // remove lowest 20% saturated pixels
    int removeCount = (int)(0.20 * n);
    double cutoff = 0.0;
    if (removeCount > 0 && removeCount < n)
    {
        var copy = new double[n];
        sats.CopyTo(copy, 0);
        Array.Sort(copy);
        cutoff = copy[removeCount];
    }

    var filtered = new List<DoubleRGB>(n);
    for (int i = 0; i < n; ++i)
    {
        if (sats[i] > cutoff) filtered.Add(samples[i]);
    }

    // don't remove too many samples
    if (filtered.Count < Math.Max(16, colourCount)) filtered = samples;

    KmeansFromSamples(pal, filtered, colourCount);
}

// 
void AutomaticMedianPalette(Palette pal, Img img, int colourCount = 16)
{
    pal.max = colourCount;
    pal.cols = new DoubleRGB[colourCount];
    
    var pixlist = new List<DoubleRGB>();
    for (int i = 0; i < img.Width * img.Height; ++i)
        pixlist.Add(img.FastGet(i));
    
    // select colours
    var boundList = new List<Tuple<int, int>>();
    pixlist.Sort(new ByIntensity());
    double mul = (double)pixlist.Count() / colourCount;
    for (int palIndex = 0; palIndex < colourCount; ++palIndex)
        pal.cols[palIndex] = pixlist[(int)(palIndex * mul)];
}

Palette InitialisePalette(Img src)
{
    Palette palette = new Palette();
    paletteEnum paletteType = (paletteEnum)m_palette;
    switch (paletteType)
    {
        case paletteEnum.WPLACE:
        default:
            {
                palette.max = 31;
                palette.cols = new DoubleRGB[palette.max];
                palette.cols[0] = new DoubleRGB(0xFF000000);
                palette.cols[1] = new DoubleRGB(0xFF3c3c3c);
                palette.cols[2] = new DoubleRGB(0xFF787878);
                palette.cols[3] = new DoubleRGB(0xFFd2d2d2);
                palette.cols[4] = new DoubleRGB(0xFFFFFFFF);
                palette.cols[5] = new DoubleRGB(0xFF600018);
                palette.cols[6] = new DoubleRGB(0xFFed1c24);
                palette.cols[7] = new DoubleRGB(0xFFff7f27);
                palette.cols[8] = new DoubleRGB(0xFFf6aa09);
                palette.cols[9] = new DoubleRGB(0xFFf9dd3b);
                palette.cols[10] = new DoubleRGB(0xFFfffabc);
                palette.cols[11] = new DoubleRGB(0xFF0eb968);
                palette.cols[12] = new DoubleRGB(0xFF13e67b);
                palette.cols[13] = new DoubleRGB(0xFF87ff5e);
                palette.cols[14] = new DoubleRGB(0xFF0c816e);
                palette.cols[15] = new DoubleRGB(0xFF10aea6);
                palette.cols[16] = new DoubleRGB(0xFF13e1be);
                palette.cols[17] = new DoubleRGB(0xFF28509e);
                palette.cols[18] = new DoubleRGB(0xFF4093e4);
                palette.cols[19] = new DoubleRGB(0xFF60f7f2);
                palette.cols[20] = new DoubleRGB(0xFF6b50f6);
                palette.cols[21] = new DoubleRGB(0xFF99b1fb);
                palette.cols[22] = new DoubleRGB(0xFF780c99);
                palette.cols[23] = new DoubleRGB(0xFFaa38b9);
                palette.cols[24] = new DoubleRGB(0xFFe09ff9);
                palette.cols[25] = new DoubleRGB(0xFFcb007a);
                palette.cols[26] = new DoubleRGB(0xFFec1f80);
                palette.cols[27] = new DoubleRGB(0xFFf38da9);
                palette.cols[28] = new DoubleRGB(0xFF684634);
                palette.cols[29] = new DoubleRGB(0xFF95682a);
                palette.cols[30] = new DoubleRGB(0xFFf8b277);
                return palette;
            }

        case paletteEnum.GAMEBOY:
            {
                // pale green
                palette.max = 8;
                palette.cols = new DoubleRGB[palette.max];
                palette.cols[0] = new DoubleRGB(0xFF181818); // 0
                palette.cols[1] = new DoubleRGB(0xFF4A5138); // 1
                palette.cols[2] = new DoubleRGB(0xFF8C926B); // 2
                palette.cols[3] = new DoubleRGB(0xFFC5CAA4); // 3

                return palette;
            }

        case paletteEnum.COLOURS_8:
            {
                // e.g. BBC Model B
                palette.max = 8;
                palette.cols = new DoubleRGB[palette.max];
                palette.cols[0] = new DoubleRGB(0xFF000000); // black
                palette.cols[1] = new DoubleRGB(0xFFFF0000); // red
                palette.cols[2] = new DoubleRGB(0xFF00FF00); // green
                palette.cols[3] = new DoubleRGB(0xFFFFFF00); // yellow
                palette.cols[4] = new DoubleRGB(0xFF0000FF); // blue
                palette.cols[5] = new DoubleRGB(0xFFFF00FF); // magenta
                palette.cols[6] = new DoubleRGB(0xFF00FFFF); // cyan
                palette.cols[7] = new DoubleRGB(0xFFFFFFFF); // white
                return palette;
            }
        case paletteEnum.COLOURS_27:
            {
                // 3 levels per channel
                palette.max = 3 * 3 * 3;
                palette.cols = new DoubleRGB[palette.max];
                int index = 0;
                for (int r = 0; r < 3; ++r)
                    for (int g = 0; g < 3; ++g)
                        for (int b = 0; b < 3; ++b)
                        {
                            palette.cols[index] = new DoubleRGB(
                              DoubleRGB.IntToDouble((int)clamp(r * 255 / 2.0, 0.0, 255.0)),
                              DoubleRGB.IntToDouble((int)clamp(g * 255 / 2.0, 0.0, 255.0)),
                              DoubleRGB.IntToDouble((int)clamp(b * 255 / 2.0, 0.0, 255.0))
                            );
                            ++index;
                        }
                return palette;
            }
        case paletteEnum.MSDOS:
            {
                palette.max = 16;
                palette.cols = new DoubleRGB[palette.max];
                palette.cols[0] = new DoubleRGB(0xFF000000); // black
                palette.cols[1] = new DoubleRGB(0xFF800000); // dark red
                palette.cols[2] = new DoubleRGB(0xFF008000); // dark green
                palette.cols[3] = new DoubleRGB(0xFF808000); // dark yellow
                palette.cols[4] = new DoubleRGB(0xFF000080); // dark blue
                palette.cols[5] = new DoubleRGB(0xFF800080); // dark magenta
                palette.cols[6] = new DoubleRGB(0xFF008080); // dark cyan
                palette.cols[7] = new DoubleRGB(0xFFC0C0C0); // light grey
                palette.cols[8] = new DoubleRGB(0xFF808080); // dark grey
                palette.cols[9] = new DoubleRGB(0xFFFF0000); // red
                palette.cols[10] = new DoubleRGB(0xFF00FF00); // green
                palette.cols[11] = new DoubleRGB(0xFFFFFF00); // yellow
                palette.cols[12] = new DoubleRGB(0xFF0000FF); // blue
                palette.cols[13] = new DoubleRGB(0xFFFF00FF); // magenta
                palette.cols[14] = new DoubleRGB(0xFF00FFFF); // cyan
                palette.cols[15] = new DoubleRGB(0xFFFFFFFF); // white
                return palette;
            }
        case paletteEnum.COMMODORE64:
            {
                palette.max = 16;
                palette.cols = new DoubleRGB[palette.max];
                palette.cols[0] = new DoubleRGB(0xFF000000); // black
                palette.cols[1] = new DoubleRGB(0xFF606060); // grey1
                palette.cols[2] = new DoubleRGB(0xFF8A8A8A); // grey2
                palette.cols[3] = new DoubleRGB(0xFFB3B3B3); // grey3
                palette.cols[4] = new DoubleRGB(0xFFFFFFFF); // white
                palette.cols[5] = new DoubleRGB(0xFF924a40); // red
                palette.cols[6] = new DoubleRGB(0xFFC18178); // light red
                palette.cols[7] = new DoubleRGB(0xFF675200); // brown
                palette.cols[8] = new DoubleRGB(0xFF99692D); // orange
                palette.cols[9] = new DoubleRGB(0xFFD5DF7C); // yellow
                palette.cols[10] = new DoubleRGB(0xFFB3EC91); // light green
                palette.cols[11] = new DoubleRGB(0xFF72B14B); // green
                palette.cols[12] = new DoubleRGB(0xFF84C5CC); // cyan
                palette.cols[13] = new DoubleRGB(0xFF483AAA); // blue
                palette.cols[14] = new DoubleRGB(0xFF867ADE); // light blue
                palette.cols[15] = new DoubleRGB(0xFF9351B6); // purple
                return palette;
            }

        case paletteEnum.RISCOS:
            {
                palette.max = 16;
                palette.cols = new DoubleRGB[palette.max];
                palette.cols[0] = new DoubleRGB(0xFF000000);
                palette.cols[1] = new DoubleRGB(0xFFDD0000);
                palette.cols[2] = new DoubleRGB(0xFF558800);
                palette.cols[3] = new DoubleRGB(0xFF00CC00);
                palette.cols[4] = new DoubleRGB(0xFF004499);
                palette.cols[5] = new DoubleRGB(0xFF00BBFF);
                palette.cols[6] = new DoubleRGB(0xFFFFBB00);
                palette.cols[7] = new DoubleRGB(0xFFEEEE00);
                palette.cols[8] = new DoubleRGB(0xFF333333);
                palette.cols[9] = new DoubleRGB(0xFF555555);
                palette.cols[10] = new DoubleRGB(0xFF777777);
                palette.cols[11] = new DoubleRGB(0xFF999999);
                palette.cols[12] = new DoubleRGB(0xFFBBBBBB);
                palette.cols[13] = new DoubleRGB(0xFFDDDDDD);
                palette.cols[14] = new DoubleRGB(0xFFFFFFFF);
                palette.cols[15] = new DoubleRGB(0xFFEEEEBB);
                return palette;
            }

        case paletteEnum.KMEANS: AutomaticKmeansPalette(palette, src, m_col_count); break;
        case paletteEnum.AUTOMATIC: AutomaticMedianPalette(palette, src, m_col_count); break;
    }
    return palette;
}

DoubleRGB ApplyPalette(DoubleRGB col, Palette pal)
{
    col = ColourFindClosest(col, pal);
    return col;
}

void DitherNormal(Img dst, Palette pal)
{
    const int MSIZE = 4;
    const int MSIZE_POW = MSIZE * MSIZE;
    double[,] matrix = new double[MSIZE, MSIZE] {
    { 0.0,  8.0,  2.0, 10.0},
    {12.0,  4.0, 14.0,  6.0},
    { 3.0, 11.0,  1.0,  9.0},
    {15.0,  7.0, 13.0,  5.0} };

    for (int y = 0; y < MSIZE; y++)
        for (int x = 0; x < MSIZE; x++)
            matrix[x, y] = (matrix[x, y] / MSIZE_POW) - 0.5;
    
    Img buffer = dst.MakeCopy();
    double mul = 1.0 / (double)pal.max;
    
    System.Threading.Tasks.Parallel.For(0, buffer.Height, y =>
    {
        if (IsCancelRequested) return;
        for (int x = 0; x < buffer.Width; x++)
        {
            DoubleRGB col = buffer.FastGet(x, y);
            col += mul * matrix[x % MSIZE, y % MSIZE];
            col = ApplyPalette(col, pal);
            dst.FastSet(x, y, col);
        }
    });
    matrix = null;
}

DoubleRGB DitherError(DoubleRGB a, DoubleRGB b)
{
    return a - b;
}

// Floyd-Steinberg
void DitherPhoto(Img dst, Palette pal)
{
    Img buffer = dst.MakeCopy();
    for (int y = 0; y < buffer.Height; y++)
    {
        if (IsCancelRequested) return;
        for (int x = 0; x < buffer.Width; x++)
        {
            DoubleRGB old = buffer.FastGet(x, y);
            DoubleRGB updated = ApplyPalette(old, pal);
            buffer.FastSet(x, y, updated);
            DoubleRGB qError = DitherError(old, updated);
            
            DoubleRGB temp;
            temp = (buffer.Get(x + 1, y) + qError * (7.0 / 16.0)).ClampDoubleRGB(0.0, 1.0);
            buffer.Set(x + 1, y, temp);
            
            temp = (buffer.Get(x - 1, y + 1) + qError * (3.0 / 16.0)).ClampDoubleRGB(0.0, 1.0);
            buffer.Set(x - 1, y + 1, temp);
            
            temp = (buffer.Get(x, y + 1) + qError * (5.0 / 16.0)).ClampDoubleRGB(0.0, 1.0);
            buffer.Set(x, y + 1, temp);
            
            temp = (buffer.Get(x + 1, y + 1) + qError * (1.0 / 16.0)).ClampDoubleRGB(0.0, 1.0);
            buffer.Set(x + 1, y + 1, temp);
        }
    }
    dst.FastRead(buffer);
}

void AddAlpha(Surface dst, Surface src)
{
    var width = src.Width;
    var height = src.Height;
    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var srcColour = src[x, y].Bgra;
            var dstColour = dst[x, y].Bgra;
            var result = new ColorBgra();
            result.A = (byte)((srcColour >> 24) & 0xFF);
            result.R = (byte)((dstColour >> 16) & 0xFF);
            result.G = (byte)((dstColour >> 8) & 0xFF);
            result.B = (byte)((dstColour >> 0) & 0xFF);
            dst[x, y] = result;
        }
}

void ApplyBilateralFilter(Surface dst, Surface src, int radius, int threshold)
{
    int thresholdSq = threshold * threshold * 3;
    int radiusSq = radius * radius;
    
    System.Threading.Tasks.Parallel.For(0, src.Height, y =>
    {
        if (IsCancelRequested) return;
        
        for (int x = 0; x < src.Width; x++)
        {
            ColorBgra centerPixel = src[x, y];
            int sumR = 0, sumG = 0, sumB = 0, totalWeight = 0;
            int centerR = centerPixel.R;
            int centerG = centerPixel.G;
            int centerB = centerPixel.B;

            bool isUniform = true;
            
            for (int ky = -radius; ky <= radius; ky++)
            {
                int ny = y + ky;
                if (ny < 0 || ny >= src.Height) continue;
                
                for (int kx = -radius; kx <= radius; kx++)
                {
                    int nx = x + kx;
                    if (nx < 0 || nx >= src.Width) continue;
                    
                    ColorBgra neighbor = src[nx, ny];
                    

                    int dr = neighbor.R - centerR;
                    int dg = neighbor.G - centerG;
                    int db = neighbor.B - centerB;
                    int diff = dr * dr + dg * dg + db * db;
                    
                    if (diff <= thresholdSq)
                    {
                        int distSq = kx * kx + ky * ky;
                        int weight = distSq <= radiusSq ? (radiusSq - distSq + 1) : 1;
                        
                        sumR += neighbor.R * weight;
                        sumG += neighbor.G * weight;
                        sumB += neighbor.B * weight;
                        totalWeight += weight;
                        isUniform = false;
                    }
                }
            }

            if (totalWeight > 0 && !isUniform)
            {
                dst[x, y] = ColorBgra.FromBgra(
                    (byte)(sumB / totalWeight),
                    (byte)(sumG / totalWeight),
                    (byte)(sumR / totalWeight),
                    centerPixel.A);
            }
            else
            {
                dst[x, y] = centerPixel;
            }
        }
    });
}

void PreRender(Surface dst, Surface src) { }

void Render(Surface dst, Surface src, Rectangle rect)
{
    Surface workSurface = new Surface(src.Width, src.Height);
    using (Graphics g = Graphics.FromImage(workSurface.CreateAliasedBitmap()))
    {
        g.DrawImage(src.CreateAliasedBitmap(), 0, 0);
        g.DrawImage(src.CreateAliasedBitmap(), m_pixelate ? m_pixelate_nudge_xy : 0, m_pixelate ? m_pixelate_nudge_xy : 0);
    }

    if (m_flatten)
    {
        Surface blurSurface = new Surface(workSurface.Width, workSurface.Height);
        const int FLATTEN_RADIUS = 10;
        const int FLATTEN_THRESHOLD = 50;
        ApplyBilateralFilter(blurSurface, workSurface, FLATTEN_RADIUS, FLATTEN_THRESHOLD);
        workSurface = blurSurface;
    }

    // scale down
    if (m_pixelate)
    {
        int newWidth = (int)(workSurface.Width * m_pixelate_percent / 100.0);
        int newHeight = (int)(workSurface.Height * m_pixelate_percent / 100.0);
        if (newWidth < 1) newWidth = 1;
        if (newHeight < 1) newHeight = 1;

        Surface scaledSurface = new Surface(newWidth, newHeight);
        using (Graphics g = Graphics.FromImage(scaledSurface.CreateAliasedBitmap()))
        {
            g.InterpolationMode = m_smooth ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
            g.DrawImage(workSurface.CreateAliasedBitmap(), 0, 0, newWidth, newHeight);
        }
        workSurface = scaledSurface;
    }

    Img workImg = new Img(workSurface);
    workImg.Colcor(m_brightness, m_contrast);

    Palette pal = InitialisePalette(workImg);
    if (pal == null)
        return;

    switch ((ditherEnum)m_dither)
    {
        default:
        case ditherEnum.NONE:
            {
                if (workImg.Width * workImg.Height > 100000)
                {
                    System.Threading.Tasks.Parallel.For(0, workImg.Width * workImg.Height, i =>
                    {
                        if (IsCancelRequested) return;
                        workImg.Buffer[i] = ApplyPalette(workImg.Buffer[i], pal);
                    });
                }
                else
                {
                    for (int i = 0; i < workImg.Width * workImg.Height; ++i)
                        workImg.Buffer[i] = ApplyPalette(workImg.Buffer[i], pal);
                }
                break;
            }
        case ditherEnum.NORMAL: DitherNormal(workImg, pal); break;
        case ditherEnum.PHOTO: DitherPhoto(workImg, pal); break;
    }

    if (m_pixelate)
    {
        // scale back to original size
        Surface tempDst = new Surface(workImg.Width, workImg.Height);
        workImg.Write(tempDst);
        using (Graphics g = Graphics.FromImage(dst.CreateAliasedBitmap()))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(tempDst.CreateAliasedBitmap(), new Rectangle(0, 0, dst.Width, dst.Height));
            for (int x = 0; x >= -1; --x)
                for (int y = 0; y >= -1; --y)
                    g.DrawImage(tempDst.CreateAliasedBitmap(), new Rectangle(m_pixelate_nudge_xy * x, m_pixelate_nudge_xy * y, dst.Width, dst.Height));
        }
    }
    else
    {
        workImg.Write(dst);
    }

    AddAlpha(dst, src);

    workSurface.Dispose();
}