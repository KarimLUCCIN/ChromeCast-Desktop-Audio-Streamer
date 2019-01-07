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

namespace ColorWheel.Core
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using System.Diagnostics;

    public static partial class ColorExt
    {
        public static DoubleColor Alter(
            this DoubleColor                            c,
            double?                                     r = null,
            double?                                     g = null,
            double?                                     b = null,
            double?                                     a = null
        )
        {
            if (r != null)
            {
                c.R = r.Value;
            }

            if (g != null)
            {
                c.G = g.Value;
            }

            if (b != null)
            {
                c.B = b.Value;
            }

            if (a != null)
            {
                c.A = a.Value;
            }

            return c;
        }

        public static DoubleColor AlterHue(
            this DoubleColor                            c,
            ColorWheelBase                              wheel,
            double                                      hue
        )
        {
            AHSB                                        src = c.ToAHSB();
            AHSB                                        dest = wheel.GetColor(hue).ToAHSB();

            dest.Saturation = src.Saturation;
            dest.Brightness = src.Brightness;
            dest.Alpha = src.Alpha;

            return dest.Double();
        }

        public static AHSB Alter(
            this AHSB                                   hsb,
            double?                                     hue        = null,
            double?                                     saturation = null,
            double?                                     brightness = null
        )
        {
            if (hue != null)
            {
                hsb.HueDegree = hue.Value;
            }

            if (saturation != null)
            {
                hsb.Saturation = saturation.Value;
            }

            if (brightness != null)
            {
                hsb.Brightness = brightness.Value;
            }

            return hsb;
        }

        public static DoubleColor Invert(
            this DoubleColor                             c
        )
        {
            return new DoubleColor()
            {
                 A = c.A,
                 R = 255 - c.R,
                 G = 255 - c.G,
                 B = 255 - c.B
            };
        }

        public static Color Invert(
            this Color                                   c
        )
        {
            return new Color()
            {
                 A = c.A,
                 R = (byte) (255 - c.R),
                 G = (byte) (255 - c.G),
                 B = (byte) (255 - c.B)
            };
        }

        public static Color InvertLiminosity(
            this Color                                  c
        )
        {
            AHSL                                        dc;

            dc = (new DoubleColor(c)).ToAHSL();
            dc.Luminance = 1 - dc.Luminance;

            return dc.Double().ToColor();
        }
        
        public static DoubleColor Darker(
            this DoubleColor                            c, 
            double                                      coff = 0.25
        )
        {
            AHSL                                        hsl = ColorExt.ToAHSL(c);

            hsl.Luminance = MathEx.Lerp(hsl.Luminance, 0, coff);

            return ColorExt.Double(hsl);
        }

        public static Color Darker(
            this Color                                  c, 
            double                                      coff = 0.25
        )
        {
            return c.Double().Darker(coff).ToColor();
        }

        public static DoubleColor Lighter(
            this DoubleColor                            c, 
            double                                      coff = 0.25
        )
        {
            AHSL                                        hsl = ColorExt.ToAHSL(c);

            hsl.Luminance = MathEx.Lerp(hsl.Luminance, 1, coff);

            return ColorExt.Double(hsl);
        }

        public static Color Lighter(
            this Color                                  c, 
            double                                      coff = 0.25
        )
        {
            return c.Double().Lighter(coff).ToColor();
        }

        public static Brush Clone(
            this Brush                                  val
            
        )
        {
            return Clone(val, new Func<Color,Color>((c) => c));
        }

        public static Brush Clone(
            this Brush                                  val,
            Func<Color, Color>                          transform
        )
        {
            if (val as SolidColorBrush != null)
            {
                return new SolidColorBrush(transform((val as SolidColorBrush).Color));
            }
            else if (val as RadialGradientBrush != null)
            {
                RadialGradientBrush lgb = val as RadialGradientBrush;
                RadialGradientBrush result = new RadialGradientBrush()
                {
                    GradientOrigin = lgb.GradientOrigin,
                    Center = lgb.Center,
                    RadiusY = lgb.RadiusY,
                    RadiusX = lgb.RadiusX
                };

                foreach (GradientStop gs in lgb.GradientStops)
                {
                    result.GradientStops.Add(new GradientStop()
                    {
                        Color = transform(gs.Color),
                        Offset = gs.Offset
                    });
                }

                return result;
            }
            else if (val as LinearGradientBrush != null)
            {
                LinearGradientBrush lgb = val as LinearGradientBrush;
                LinearGradientBrush result = new LinearGradientBrush()
                {
                    StartPoint = lgb.StartPoint,
                    EndPoint = lgb.EndPoint
                };

                foreach (GradientStop gs in lgb.GradientStops)
                {
                    result.GradientStops.Add(new GradientStop()
                    {
                        Color = transform(gs.Color),
                        Offset = gs.Offset
                    });
                }

                return result;
            }
            else
            {
                throw new NotImplementedException("ColorExt.Clone LinearGradientBrush or SolidColorBrush is expected");
            }
        }

        public static Brush ToBrush(
            this Color                                  c
        )
        {
            return new SolidColorBrush(c);
        }

        #region Utilities (pure, random, luma, chroma, calc foreground)

        public static Color GetPure(
            this Color                                  c
        )
        {
            AHSB ahsb = c.Double().ToAHSB();

            ahsb.Brightness = 1.0;
            ahsb.Saturation = 1.0;

            return ahsb.Double().ToColor();
        }

        public static int GetChroma(
            this Color                                  color
        )
        {
            int max = Math.Max(Math.Max(color.R, color.G), color.B);
            int min = Math.Min(Math.Min(color.R, color.G), color.B);

            return max - min;
        }
        
        public static double GetLuminance(
            this Color                                  c
        )
        {
            return c.Double().ToCIELab().Luminance;
        }

        public static Color SetLuminance(
            this Color                                  c,
            double                                      luminance
        )
        {
            CIELab                                      lab;

            lab = c.Double().ToCIELab();
            lab.Luminance = luminance;

            return lab.Double().ToColor();
        }

        #region Calculating Foreground

        /// 
        /// <summary>
        /// For given background color returns best foreground color (white or black)</summary>
        /// 
        public static Color GetForeground(
            this Color                                  bg
        )
        {
            return bg.Double().ToAHSB().Brightness > 0.5 ? Colors.Black : Colors.White;;
        }


        /// 
        /// <summary>
        /// This method will return a number in the rage of 0 (black) to 255 (White) and to set 
        /// the foreground color based on the Brightness</summary>
        /// 
        public static double GetBrightness(
            this Color                                  c
        )
        {
            //
            // Other Formulas:
            //
            //     (standard, objective): (0.2126*R) + (0.7152*G) + (0.0722*B)
            //     (perceived option 1): (0.299*R + 0.587*G + 0.114*B)
            //     (perceived option 2, slower to calculate): sqrt( 0.241*R^2 + 0.691*G^2 + 0.068*B^2 )
            //
            return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
        }

        #endregion

        #endregion

        #region W3C Color Accessibility

        /// <summary>
        /// WCAG 2 (draft):
        /// Text or diagrams and their background must have a luminosity contrast ratio of at least 5:1 for level 2 conformance 
        /// to guideline 1.4, and text or diagrams and their background must have a luminosity contrast ratio of at least 10:1 
        /// for level 3 conformance to guideline 1.4.</summary>
        /// 
        public static double GetW3CLuminosityContrastRatio(
            Color                                   c1,
            Color                                   c2
        )
        {
            Func<byte, double> lr = (color) => Math.Pow((color / 255.0), 2.2);

            double l1 = 0.2126 * lr(c1.R) + .7152 * lr(c1.G) + .0722 * lr(c1.B);
            double l2 = 0.2126 * lr(c2.R) + .7152 * lr(c2.G) + .0722 * lr(c2.B);

            return l1 > l2 ? (l1 + 0.05) / (l2 + 0.05) : (l2 + 0.05) / (l1 + 0.05);
        }
            
        /// 
        /// <summary>
        /// From WCAG 2
        /// The difference between the background brightness, and the foreground brightness 
        /// should be greater than 125.</summary>
        /// 
        public static double GetW3CBrightness(
            this Color                              c
            )
        {
            return ((c.R * 299.0) + (c.G * 587.0) + (c.B * 114.0)) / 1000.0;
        }

        /// <summary>
        /// The following is the formula suggested by the W3C to determine the difference between two colors.
        /// The difference between the background color and the foreground color should be greater than 500.</summary>
        /// 
        public static double GetW3CColorDifference(
            Color                                   l,
            Color                                   r
        )
        {
            return (Math.Max(l.R, r.R) - Math.Min(l.R, r.R)) +
                   (Math.Max(l.G, r.G) - Math.Min(l.G, r.G)) + 
                   (Math.Max(l.B, r.B) - Math.Min(l.B, r.B));
        }

        #endregion 
    }
}
