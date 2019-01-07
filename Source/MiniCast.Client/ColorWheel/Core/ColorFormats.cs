/* 
 * Copyright (c) 2011, Andriy Syrov
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided 
 * that the following conditions are met:
 * 
 * Redistributions of source code must retain the above copyright notice, this list of conditions and the 
 * following disclaimer.
 * 
 * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and 
 * the following disclaimer in the documentation and/or other materials provided with the distribution.
 *
 * Neither the name of Andriy Syrov nor the names of his contributors may be used to endorse or promote 
 * products derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED 
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
 * PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY 
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
 * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN 
 * IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
 *
 */

/*
 * References and formulas:
 * 
 *          http://en.wikipedia.org/wiki/Lab_color_space
 *          http://msdn.microsoft.com/en-us/library/bb263947%28v=vs.85%29.aspx
 *          http://www.creativephotobook.co.uk/pg09007.html
 * 
 * The code partially licensed by Guillaume Leparmentier under Code Project Open License (CPOL) 1.02
 * The Code Project Open License (CPOL) is intended to provide developers who choose to share their code with 
 * a license that protects them and provides users of their code with a clear statement regarding how the code
 * can be used. The CPOL is a gift to the community. We encourage everyone to use this license if they 
 * wish regardless of whether the code is posted on CodeProject.com: 
 * 
 *          http://www.codeproject.com/info/cpol10.aspx
 *          http://www.codeproject.com/KB/recipes/colorspace1.aspx#lab
 */

namespace ColorWheel.Core
{
    using System;
    using System.Diagnostics;
    using System.Windows.Media;

    /// 
    /// <summary>
    /// Some Definitions:
    ///   - Hue is color (blue, green, red, etc.).
    ///   - Chroma is the purity of a color (a high chroma has no added black, white or gray).
    ///   - Saturation refers to how strong or weak a color is (high saturation being strong).
    ///   - Value refers to how light or dark a color is (light having a high value).
    ///   - Tones are created by adding gray to a color, making it duller than the original.
    ///   - Shades are created by adding black to a color, making it darker than the original.
    ///   - Tints are created by adding white to a color, making it lighter than the original.</summary>
    ///    
    public static partial class ColorExt
    {
        #region Conversion between color formats

        public static long ToLong(
            this Color                                  c
        )
        {
            return (long) (((ulong) ((((c.R << 0x10) | (c.G << 8)) | c.B) | (c.A << 0x18))) & 0xffffffffL);
        }

        public static Color ToColor(
            this long                                   color
        )
        {
            return new Color()
            {
                A = (byte) ((color >> 0x18) & 0xff),
                R = (byte) ((color >> 0x10) & 0xff),
                G = (byte) ((color >> 8) & 0xff),
                B = (byte) (color & 0xff)
            };
        }

        /// 
        /// <summary>
        /// Return value of color for WritableBitmap</summary>
        /// 
        public static int ToARGB32(
            this DoubleColor                            c
        )
        {
            Color color = c.ToColor();

            return color.ToARGB32();
        }

        /// 
        /// <summary>
        /// Return value of color for WritableBitmap</summary>
        /// 
        public static int ToARGB32(
            this Color                                  color
        )
        {
            return ((color.R << 16) | (color.G << 8) | (color.B << 0) | (color.A << 24));
        }

        #region From Color to other types

        public static AHSB ToAHSB(
            this DoubleColor                            c
        )
        {
            double                                      a = c.A / 255.0;
            double                                      r = c.R / 255.0;
            double                                      g = c.G / 255.0;
            double                                      b = c.B / 255.0;

            AHSB                                        ret;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            //
            // Black - gray - white
            //
            if (min == max) 
            {
                ret = new AHSB(a, 0, 0, (double) min);
            }
            else
            {
                var d = (r == min) ? g - b : ((b == min) ? r - g : b - r);
                var h = (r == min) ? 3 : ((b == min) ? 1 : 5);

                double hue = 60 * (h - d / (max - min));
                double sat = (max - min) / max;
                double value = max;

                ret = new AHSB(a, hue, sat, value); 
            }

            if (ret.Saturation == 0)
            {
                ret.HueDegree = c.HueDegree;
            }

            if (ret.Brightness == 0)
            {
                ret.Saturation = c.Saturation;
            }

            return ret;
        }

        public static CIEAXYZ ToCIEAXYZ(
            this DoubleColor                            c
        )
        {
            // normalize 
            double rl    = c.R / 255.0;
            double gl    = c.G / 255.0;
            double bl    = c.B / 255.0;
            double alpha = c.A / 255.0;

            // convert to a sRGB form
            double r = (rl > 0.04045) ? Math.Pow((rl + 0.055) / (1.055), 2.4) : (rl / 12.92);
            double g = (gl > 0.04045) ? Math.Pow((gl + 0.055) / (1.055), 2.4) : (gl / 12.92);
            double b = (bl > 0.04045) ? Math.Pow((bl + 0.055) / (1.055), 2.4) : (bl / 12.92);

            // converts
            return new CIEAXYZ(
                alpha,
                (r * 0.4124 + g * 0.3576 + b * 0.1805),
                (r * 0.2126 + g * 0.7152 + b * 0.0722),
                (r * 0.0193 + g * 0.1192 + b * 0.9505));
        }

        public static CIELab ToCIELab(
            this DoubleColor                            c
        )
        {
            return c.ToCIEAXYZ().ToCIELab();
        }

        public static AHSL ToAHSL(
            this DoubleColor                            c
        )        
        {
            double h = 0;
            double s = 0;
            double l = 0;
            double a = 0;

            double r  = (double) c.R / 255.0;
            double g  = (double) c.G / 255.0;
            double b  = (double) c.B / 255.0;
            
            a = (double) c.A / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            // hue
            if (max == min)
            {
                h = 0; // undefined
            }
            else if (max == r && g >= b)
            {
                h = 60.0 * (g - b) / (max - min);
            }
            else if (max == r && g < b)
            {
                h = 60.0 * (g - b) / (max - min) + 360.0;
            }
            else if (max == g)
            {
                h = 60.0 * (b - r) / (max - min) + 120.0;
            }
            else if (max == b)
            {
                h = 60.0 * (r - g) / (max - min) + 240.0;
            }

            // luminance
            l = (max + min) / 2.0;

            // saturation
            if (l == 0 || max == min)
            {
                s = 0;
            }
            else if (0 < l && l <= 0.5)
            {
                s = (max - min) / (max + min);
            }
            else if (l > 0.5)
            {
                s = (max - min) / (2 - (max + min)); 
            } 

            if (s == 0) // gray
            {
                h = c.HueDegree;
            }

            if (l == 0)
            {
                s = c.Saturation;
            }

            return new AHSL(h, s, l, a);
        }

        public static AYUV ToAYUV(
            this DoubleColor                            c
        )
        {
            AYUV yuv = new AYUV();

            //
            // normalizes red, green, blue values
            //
            double r = (double) c.R / 255.0;
            double g = (double) c.G / 255.0;
            double b = (double) c.B / 255.0;

            yuv.Y = 0.299 * r + 0.587 * g + 0.114 * b;
            yuv.U = -0.14713 * r - 0.28886 * g + 0.436 * b;
            yuv.V = 0.615 * r - 0.51499 * g - 0.10001 * b;
            yuv.A = c.A / 255.0;

            return yuv;
        }

        #endregion

        #region Between CIELab and CIEXYZ

        public static CIELab ToCIELab(
            this CIEAXYZ                                axyz
        )
        {
            CIELab                                      lab = CIELab.Empty;

            double                                      x = axyz.X;
            double                                      y = axyz.Y;
            double                                      z = axyz.Z;

            lab.Luminance = 116.0 * Fxyz(y / CIEAXYZ.D65.Y) - 16;
            lab.RedGreenDimension = 500.0 * (Fxyz(x / CIEAXYZ.D65.X) - Fxyz(y / CIEAXYZ.D65.Y));
            lab.YellowBlueDimension = 200.0 * (Fxyz(y / CIEAXYZ.D65.Y) - Fxyz(z / CIEAXYZ.D65.Z));
            lab.Alpha = axyz.Alpha;

            return lab;
        }

        public static CIEAXYZ ToCIEAXYZ(
            this CIELab                                 lab
        )
        {
            double l = lab.Luminance;
            double a = lab.RedGreenDimension;
            double b = lab.YellowBlueDimension;

            double delta = 6.0 / 29.0;

            double fy = (l + 16) / 116.0;
            double fx = fy + (a / 500.0);
            double fz = fy - (b / 200.0);

            return new CIEAXYZ(
                lab.Alpha,
                (fx > delta) ? CIEAXYZ.D65.X * (fx * fx * fx) : (fx - 16.0 / 116.0) * 3 * (delta * delta) * CIEAXYZ.D65.X,
                (fy > delta) ? CIEAXYZ.D65.Y * (fy * fy * fy) : (fy - 16.0 / 116.0) * 3 * (delta * delta) * CIEAXYZ.D65.Y,
                (fz > delta) ? CIEAXYZ.D65.Z * (fz * fz * fz) : (fz - 16.0 / 116.0) * 3 * (delta * delta) * CIEAXYZ.D65.Z);
        }

        #endregion

        #region Back to RGB

        public static DoubleColor Double(
            this AYUV                                   yuv
        )
        {
            DoubleColor                                 c = DoubleColor.Empty;

            c.R = (yuv.Y + 1.139837398373983740 * yuv.V) * 255.0;
            c.G = (yuv.Y - 0.3946517043589703515 * yuv.U - 0.5805986066674976801 * yuv.V) * 255.0;
            c.B = (yuv.Y + 2.032110091743119266 * yuv.U) * 255.0;
            c.A = yuv.A * 255.0;

            return c;
        }

        public static DoubleColor Double(
            this Color                                  c
        )
        {
            return new DoubleColor(c.A, c.R, c.G, c.B);
        }

        public static DoubleColor Double(
            this AHSB                                   ahsb
            )
        {
            double a          = ahsb.Alpha;
            double hue        = ahsb.HueDegree;
            double saturation = ahsb.Saturation;
            double value      = ahsb.Brightness;
            DoubleColor ret;

            int hi   = ((int) (hue / 60)) % 6;
            double f = hue / 60 - (int) (hue / 60);

            value    = value * 255;
            double v = value;
            double p = (value * (1 - saturation));
            double q = (value * (1 - f * saturation));
            double t = (value * (1 - (1 - f) * saturation));

            if (hi == 0)
            {
                ret = new DoubleColor(a * 255, v, t, p);
            }
            else if (hi == 1)
            {
                ret = new DoubleColor(a * 255, q, v, p);
            }
            else if (hi == 2)
            {
                ret = new DoubleColor(a * 255, p, v, t);
            }
            else if (hi == 3)
            {
                ret = new DoubleColor(a * 255, p, q, v);
            }
            else if (hi == 4)
            {
                ret = new DoubleColor(a * 255, t, p, v);
            }
            else
            { 
                ret = new DoubleColor(a * 255, v, p, q);
            }

            ret.HueDegree = hue;
            ret.Saturation = saturation;

            return ret;
        }

        public static DoubleColor Double(
            this CIEAXYZ                                xyz
        )
        {
            double                                      x = xyz.X;
            double                                      y = xyz.Y;
            double                                      z = xyz.Z;

            double[]                                    Clinear = new double[3];

            Clinear[0] =  x * 3.2406 - y * 1.5372 - z * 0.4986; // red
            Clinear[1] = -x * 0.9689 + y * 1.8758 + z * 0.0415; // green
            Clinear[2] =  x * 0.0557 - y * 0.2040 + z * 1.0570; // blue

            for(int i = 0; i < 3; i++)
            {
                Clinear[i] = (Clinear[i] <= 0.0031308) ? 12.92 * Clinear[i] : 1.055 * Math.Pow(Clinear[i], (1.0 / 2.4)) - 0.055;
            }

            return new DoubleColor()
            {
                A = xyz.Alpha  * 255.0,                 
                R = Clinear[0] * 255.0,                 
                G = Clinear[1] * 255.0,                 
                B = Clinear[2] * 255.0
            };
        }

        public static DoubleColor Double(
            this CIELab                                 lab
        )
        {
            return lab.ToCIEAXYZ().Double();
        }

        public static DoubleColor Double(
            this AHSL                                   hsl
        )
        {
            double h = hsl.HueDegree;
            double s = hsl.Saturation;
            double l = hsl.Luminance;
            double a = hsl.Alpha;
            DoubleColor ret;

            if(s == 0)
            {
                // achromatic color (gray scale)
                ret = new DoubleColor(a * 255.0, l * 255.0, l * 255.0, l * 255.0);
            }
            else
            {
                double q = (l < 0.5) ? (l * (1.0 + s)) : (l + s - (l * s));
                double p = (2.0 * l) - q;

                double Hk  = h / 360.0;
                double[] T = new double[3];

                T[0] = Hk + (1.0 / 3.0);    // Tr
                T[1] = Hk;                  // Tb
                T[2] = Hk - (1.0 / 3.0);    // Tg

                for (int i = 0; i < 3; i++)
                {
                    if (T[i] < 0) T[i] += 1.0;
                    if (T[i] > 1) T[i] -= 1.0;

                    if ((T[i] * 6) < 1)
                    {
                        T[i] = p + ((q - p) * 6.0 * T[i]);
                    }
                    else if ((T[i] * 2.0) < 1) // (1.0 / 6.0) <= T[i] && T[i] < 0.5
                    {
                        T[i] = q;
                    }
                    else if ((T[i] * 3.0) < 2) // 0.5 <= T[i] && T[i] < (2.0/3.0)
                    {
                        T[i] = p + (q - p) * ((2.0 / 3.0) - T[i]) * 6.0;
                    }
                    else 
                    {
                        T[i] = p;
                    }
                }

                ret = new DoubleColor(a * 255.0, T[0] * 255.0, T[1] * 255.0, T[2] * 255.0);
            }

            //
            // this will preserve hue for gray colors, and saturation for black
            //
            ret.HueDegree = h;
            ret.Saturation = s;

            return ret;
        }

        public static Color ToColor(
            this DoubleColor                            dc
        )
        {
            return new Color()
            {
                A = (byte) Math.Round(dc.A, 0),
                R = (byte) Math.Round(dc.R, 0),
                G = (byte) Math.Round(dc.G, 0),
                B = (byte) Math.Round(dc.B, 0)
            };
        }

        #endregion

        #endregion

        #region Mics
        
        private static double Fxyz(double t)
        {
            return ((t > 0.008856) ? Math.Pow(t, (1.0 / 3.0)) : (7.787 * t + 16.0 / 116.0));
        }

        #endregion

        public static DoubleColor Lerp(
            this DoubleColor                            c1,
            DoubleColor                                 c2,
            double                                      amount // value between 0.0 and 1.0
        )
        {
            var c = new DoubleColor()
            {
                A = MathEx.Lerp(c1.A, c2.A, amount),
                R = MathEx.Lerp(c1.R, c2.R, amount),
                G = MathEx.Lerp(c1.G, c2.G, amount),
                B = MathEx.Lerp(c1.B, c2.B, amount)
            };

            return c;
        }
    }

    #region Color Formats

    /// 
    /// <summary>
    /// Similar to RGB, but uses double for more precision during conversions between different 
    /// color formats.
    /// It also keeps last known hue degree, because it cannot be calculated when r = g = b 
    /// </summary>
    /// 
    public struct DoubleColor
    {
        public static readonly DoubleColor              Empty = new DoubleColor(0, 0, 0, 0);
        public static readonly DoubleColor              White = new DoubleColor(255, 255, 255, 255);
        public static readonly DoubleColor              Black = new DoubleColor(255, 0, 0, 0);

        private double                                  m_hue;        // last known 'good' hue (when r != g != b)
        private double                                  m_saturation; // last known 'good' saturation when brightness == 0

        private double                                  m_a;
        private double                                  m_r;
        private double                                  m_g;
        private double                                  m_b;

        public DoubleColor(
            Color                                       c
        ): this(c.A, c.R, c.G, c.B)
        {
        }

        public DoubleColor(
            double                                      a,
            double                                      r,
            double                                      g,
            double                                      b
        )
        {
            m_a = a > 255 ? 255 : a;
            m_r = r > 255 ? 255 : r;
            m_g = g > 255 ? 255 : g;
            m_b = b > 255 ? 255 : b;

            m_saturation = m_hue = 0;

            m_hue = ToHueDegree();
            m_saturation = ToSaturation();
        }

        public double A
        {
            get
            {
                return m_a;
            }
            set
            {
                m_a = (value > 255) ? 255 : ((value < 0) ? 0 : value);
            }
        }

        public double R
        {
            get
            {
                return m_r;
            }
            set
            {
                m_r = (value > 255) ? 255 : ((value < 0) ? 0 : value);
            }
        }

        public double G
        {
            get
            {
                return m_g;
            }
            set
            {
                m_g = (value > 255) ? 255 : ((value < 0) ? 0 : value);
            }
        }

        public double B
        {
            get
            {
                return m_b;
            }
            set
            {
                m_b = (value > 255) ? 255 : ((value < 0) ? 0 : value);
            }
        }

        /// 
        /// <summary>
        /// The purpose of this property is to preserve saturation during conversion between rgb based and hue based
        /// color schemas when black color. Setter works *only* if color is black</summary>
        /// 
        public double Saturation
        {
            get
            {
                if (G != 0 || B != 0 || R != 0)
                {
                    m_saturation = this.ToSaturation();
                }
                return m_saturation;
            }
            set
            {
                if (R == 0 && G == 0 && B == 0)
                {
                    m_saturation = value;
                }
            }
        }

        public double ToSaturation(
        )
        {
            AHSB hsb = this.ToAHSB();
            return hsb.Brightness == 0 ? m_saturation : hsb.Saturation;
        }

        /// 
        /// <summary>
        /// The purpose of this property is to preserve hue during conversion between rgb based and hue based
        /// color schemas when operating with gray colors. Setter works *only* if color is a variation gray 
        /// (saturation == 0)</summary>
        /// 
        public double HueDegree
        {
            get
            {
                if (!(R == G && G == B))
                {
                    m_hue = this.ToHueDegree();
                }
                return m_hue;
            }
            set
            {
                if (R == G && G == B)
                {
                    //
                    // This is useful only in case if color is Gray, otherwise it will be fixed in getter
                    //
                    m_hue = value;
                }
            }
        }

        public double ToHueDegree(
        )
        {
            if (R == G && G == B)
            {
                return m_hue;
            }
            else
            {
                double r = R / 255.0;
                double g = G / 255.0;
                double b = B / 255.0;

                double max = Math.Max(r, Math.Max(g, b));
                double min = Math.Min(r, Math.Min(g, b));

                if (min == max)
                {
                    return 0;
                }
                else
                {
                    var d = (r == min) ? g - b : ((b == min) ? r - g : b - r);
                    var h = (r == min) ? 3 : ((b == min) ? 1 : 5);

                    return 60 * (h - d / (max - min));
                }
            }
        }

        public override int GetHashCode(
        )
        {
            return A.GetHashCode() ^ R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode();
        }

        public static explicit operator Color(
            DoubleColor                                 c
        )
        {
            return c.ToColor();
        }

        public override bool Equals(
            Object                                      obj
        ) 
        {
            return obj is DoubleColor && this == (DoubleColor) obj;
        }

        public static bool operator ==(
            DoubleColor                                 x, 
            DoubleColor                                 y
        ) 
        {
            return (x.A == y.A && x.R == y.R && x.G == y.G && x.B == y.B);
        }

        public static bool operator !=(
            DoubleColor                                 x, 
            DoubleColor                                 y
        ) 
        {
            return !(x == y);
        }
    }

    public struct AHSL
    {
        public static readonly AHSL                     Empty = new AHSL(0, 0, 0, 1);

        private double                                  m_hue;
        private double                                  m_saturation;
        private double                                  m_luminance;
        private double                                  m_alpha;

        public AHSL(
            double                                      h, 
            double                                      s, 
            double                                      l,
            double                                      a
        )
        {
            m_hue = m_saturation = m_luminance = m_alpha = 0;

            HueDegree = h;
            Saturation = s;
            Luminance = l;
            Alpha = a;
        }

        public double HueDegree
        {
            get
            {
                return m_hue;
            }
            set
            {
                m_hue = value % 360;
            }
        }

        public double Saturation
        {
            get
            {
                return m_saturation;
            }
            set
            {
                m_saturation = (value > 1) ? 1 : ((value < 0) ? 0 : value);
            }
        }

        public double Luminance
        {
            get
            {
                return m_luminance;
            }
            set
            {
                m_luminance = (value > 1) ? 1 : ((value < 0) ? 0 : value);
            }
        }

        public double Alpha
        {
            get
            {
                return m_alpha;
            }
            set
            {
                m_alpha = (value > 1) ? 1 : ((value < 0) ? 0 : value);
            }
        }

        public override int GetHashCode()
        {
            return HueDegree.GetHashCode() ^ Saturation.GetHashCode() ^ Luminance.GetHashCode() ^ Alpha.GetHashCode();
        }

        public override bool Equals(
            Object                                      obj
        ) 
        {
            return obj is AHSL && this == (AHSL) obj;
        }

        public static bool operator ==(
            AHSL                                        x, 
            AHSL                                        y
        ) 
        {
            return (x.Alpha == y.Alpha && x.Luminance == y.Luminance && x.HueDegree == y.HueDegree && x.Saturation == y.Saturation);
        }

        public static bool operator !=(
            AHSL                                        x, 
            AHSL                                        y
        ) 
        {
            return !(x == y);
        }
    }

    public struct CIELab
    {
        public double                                   Alpha; // 0.0 to 1.0
        public double                                   Luminance;
        public double                                   RedGreenDimension;
        public double                                   YellowBlueDimension;

        public static readonly CIELab                   Empty = new CIELab(1, 0, 0, 0);

        public CIELab(
            double                                      alpha, 
            double                                      luminance, 
            double                                      redgreen, 
            double                                      yellowblue
        )
        {
            Luminance           = luminance;
            RedGreenDimension   = redgreen;
            YellowBlueDimension = yellowblue;
            Alpha               = alpha;
        }

        public override int GetHashCode(
        )
        {
            return Alpha.GetHashCode() ^ Luminance.GetHashCode() ^ RedGreenDimension.GetHashCode() ^ YellowBlueDimension.GetHashCode();
        }
    }

    /// 
    /// <summary>
    /// CIE XYZ model defines an absolute color space. It is also known as the CIE 1931 XYZ 
    /// color space and stands for:
    /// 
    ///     X, which can be compared to red. Ranges from 0 to 0.9505
    ///     Y, which can be compared to green. Ranges from 0 to 1.0
    ///     Z, which can be compared to blue. Ranges from 0 to 1.089</summary>
    ///     
    public struct CIEAXYZ
    {
        // CIE D65 (white) structure.
        public static readonly CIEAXYZ                  D65 = new CIEAXYZ(1.0, 0.9505, 1.0, 1.0890);

        private double                                  x;
        private double                                  y;
        private double                                  z;
        private double                                  alpha;

        public static readonly CIEAXYZ                  Empty = new CIEAXYZ(1, 0, 0, 0);

        public double Alpha
        {
            get
            {
                return alpha;
            }
            set
            {
                alpha = (value > 1 ? 1 : ((value < 0) ? 0 : value));
            }
        }

        public double X
        {
            get
            {
                return x;
            }
            set
            {
                x = (value > 0.9505) ? 0.9505 : (( value < 0) ? 0 : value);
            }
        }

        public double Y
        {
            get
            {
                return y;
            }
            set
            {
                y = (value > 1.0) ? 1.0 : ( (value < 0) ? 0 : value);
            }
        }

        public double Z
        {
            get
            {
                return z;
            }
            set
            {
                z = ( value > 1.089) ? 1.089 : ((value < 0) ? 0 : value);
            }
        }

        public CIEAXYZ(double al, double cx, double cy, double cz)
        {
            x = y = z = alpha = 0;

            X = cx;
            Y = cy;
            Z = cz;
            Alpha = al;
        }
    }

    /// 
    /// <summary>
    /// Transparency-Hue-Saturation-Brightness color representation</summary>
    /// 
    public struct AHSB
    {
        double                                          m_alpha;
        double                                          m_saturation;
        double                                          m_brightness;
        double                                          m_HueDegree;

        /// 
        /// <summary>
        /// Construct color (hue is degree from 0 to 360)</summary>
        /// 
        public AHSB(
            double                                      a, 
            double                                      h, 
            double                                      s, 
            double                                      b
        )
        {
            m_alpha = m_saturation = m_brightness = m_HueDegree = 0;

            Alpha      = a;
            Saturation = s;
            Brightness = b;
            HueDegree  = h;
        }

        public static double Fix(
            double                                      v
        )
        {
            return (v > 1.0) ? 1.0 : ((v < 0) ? 0 : v);
        }

        public double HueRadians
        {
            get
            {
                return (Math.PI / 180) * m_HueDegree;
            }
            set
            {
                m_HueDegree = (180 / Math.PI) * value;
            }
        }

        public double HueDegree
        {
            get
            {
                return m_HueDegree;
            }
            set
            {
                value = value % 360;
                if (value < 0)
                {
                    value += 360;
                }
                m_HueDegree = value;
            }
        }

        public double Alpha
        {
            get
            {
                return m_alpha;
            }
            set
            {
                m_alpha = Fix(value);
            }
        }

        public double Saturation
        {
            get
            {
                return m_saturation;
            }
            set
            {
                m_saturation = Fix(value);
            }
        }

        public double Brightness
        {
            get
            {
                return m_brightness;
            }
            set
            {
                m_brightness = Fix(value);
            }
        }

        #region Helpers for edit through TextBox

        public byte Alpha255
        {
            get
            {
                return (byte) Math.Round(255.0 * m_alpha, 0);
            }
            set
            {
                m_alpha = Fix(value / 255.0);
            }
        }

        public byte Saturation255
        {
            get
            {
                return (byte) Math.Round(255.0 * m_saturation, 0);
            }
            set
            {
                m_saturation = Fix(value / 255.0);
            }
        }

        public byte Brightness255
        {
            get
            {
                return (byte) Math.Round(255.0 * m_brightness, 0);
            }
            set
            {
                m_brightness = Fix(value / 255.0);
            }
        }

        #endregion

        public static explicit operator DoubleColor(
            AHSB                                        a
        )
        {
            return FromAHSB(a);
        }

        public static DoubleColor FromAHSB(
            AHSB                                        a
        )
        {
            return a.Double();
        }

        public override int GetHashCode(
        )
        {
            return m_HueDegree.GetHashCode() ^ m_saturation.GetHashCode() ^ m_brightness.GetHashCode();
        }

        public override bool Equals(
            Object                                      obj
        ) 
        {
            return obj is AHSB && this == (AHSB) obj;
        }

        public static bool operator ==(
            AHSB                                        x, 
            AHSB                                        y
        ) 
        {
            return (x.m_alpha == y.m_alpha && x.m_brightness == y.m_brightness && x.m_HueDegree == y.m_HueDegree && x.m_saturation == y.m_saturation);
        }

        public static bool operator !=(
            AHSB                                        x, 
            AHSB                                        y
        ) 
        {
            return !(x == y);
        }
    }

    public struct AYUV
    {
        public static readonly AYUV                     Empty = new AYUV();

        private double                                  m_a;
        private double                                  m_y;
        private double                                  m_u;
        private double                                  m_v;

        public static bool operator ==(
            AYUV                                        item1, 
            AYUV                                        item2
        )
        {
            return (item1.Y == item2.Y && item1.U == item2.U && item1.V == item2.V);
        }

        public static bool operator !=(
            AYUV                                        item1, 
            AYUV                                        item2
        )
        {
            return (item1.Y != item2.Y || item1.U != item2.U || item1.V != item2.V);
        }

        public double Y
        {
            get
            {
                return m_y;
            }
            set
            {
                m_y = value;
                m_y = (m_y > 1) ? 1 : (( m_y < 0) ? 0 : m_y);
            }
        }

        public double U
        {
            get
            {
                return m_u;
            }
            set
            {
                m_u = value;
                m_u = (m_u > 0.436) ? 0.436 : ((m_u < -0.436) ? -0.436 : m_u);
            }
        }

        public double V
        {
            get
            {
                return m_v;
            }
            set
            {
                m_v = value;
                m_v = (m_v > 0.615) ? 0.615 : ((m_v <- 0.615) ? -0.615 : m_v);
            }
        }

        public double A
        {
            get
            {
                return m_a;
            }
            set
            {
                m_a = value;
                m_a = (m_a > 1) ? 1 : ((m_a < 0) ? 0 : m_a);
            }
        }

        /// <summary>
        /// Creates an instance of a YUV structure.
        /// </summary>
        public AYUV(
            double                                      a,
            double                                      y, 
            double                                      u, 
            double                                      v
        )
        {
            this.m_a = (a > 1) ? 1 : ((a < 0) ? 0 : a);
            this.m_y = (y > 1) ? 1 : ((y < 0) ? 0 : y);
            this.m_u = (u > 0.436) ? 0.436 : ((u < -0.436) ? -0.436 : u);
            this.m_v = (v > 0.615) ? 0.615 : ((v < -0.615) ? -0.615 : v);
        }

        public override bool Equals(
            Object                                      obj
        )
        {
            if (obj == null || GetType() != obj.GetType()) 
            {
                return false;
            }

            return (this == (AYUV) obj);
        }

        public override int GetHashCode(
        )
        {
            return Y.GetHashCode() ^ U.GetHashCode() ^ V.GetHashCode() ^ A.GetHashCode();
        }
    }

    #endregion
}
