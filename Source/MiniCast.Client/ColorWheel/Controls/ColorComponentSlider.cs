/* 
 * Copyright (c) 2010, Andriy Syrov
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

namespace ColorWheel.Controls
{
    using System;
    using System.Windows.Media;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;
    using System.Windows;
    using System.Diagnostics;
    using ColorWheel.Core;

    public partial class ColorComponentSlider: Slider
    {
        Func<double, DoubleColor>                       m_calc;
        WriteableBitmap                                 m_bitmap;
        Border                                          m_vertImageBorder;
        Border                                          m_horzImageBorder;

        Color                                           m_begin = Colors.White;
        Color                                           m_end = Colors.Black;

        bool                                            m_isGradient;

        public static readonly DependencyProperty ColorWheelProperty =
           DependencyProperty.Register("ColorWheel", typeof(ColorWheelBase), typeof(ColorComponentSlider),
           new PropertyMetadata(new RGBColorWheel(), new PropertyChangedCallback(OnColorWheelChanged)));

        public ColorWheelBase ColorWheel
        {

            get
            {
                return (ColorWheelBase) GetValue(ColorWheelProperty);

            }
            set
            {

                SetValue(ColorWheelProperty, value);
            }
        }

        public static void OnColorWheelChanged(
            DependencyObject                            source, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            ColorComponentSlider                        s = source as ColorComponentSlider;
            Color                                       c = (Color) e.NewValue;

            s.UpdateColor(c);
        }


        public static readonly DependencyProperty SliderColorProperty =
           DependencyProperty.Register("SliderColor", typeof(Color), typeof(ColorComponentSlider),
           new PropertyMetadata(Colors.White, new PropertyChangedCallback(OnCurrentChangeColor)));

        public Color SliderColor
        {

            get
            {
                return (Color) GetValue(SliderColorProperty);

            }
            set
            {

                SetValue(SliderColorProperty, value);
            }
        }

        public static void OnCurrentChangeColor(
            DependencyObject                            source, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            ColorComponentSlider s = source as ColorComponentSlider;
            Color c = (Color) e.NewValue;

            s.UpdateColor(c);
        }

        public void UpdateColor(
            Color                                       color 
        )
        {
            DoubleColor c = color.Double();

            if (!String.IsNullOrEmpty(SliderColorComponent))
            {
                DoubleColor begin;
                DoubleColor end;

                m_isGradient = true;

                switch (SliderColorComponent.ToLower())
                {
                    case "r":
                        begin = c.Alter(0);
                        end = c.Alter(255);
                        break;

                    case "g":
                        begin = c.Alter(null, 0);
                        end = c.Alter(null, 255);
                        break;

                    case "b":
                        begin = c.Alter(null, null, 0);
                        end = c.Alter(null, null, 255);
                        break;

                    case "alpha":
                    case "a":
                        begin = c.Alter(null, null, null, 0);
                        end = c.Alter(null, null, null, 255);
                        break;

                    case "saturation":
                    case "sat":
                        begin = c.ToAHSB().Alter(null, 0).Double();
                        end = c.ToAHSB().Alter(null, 1).Double();
                        break;

                    case "brightness":
                    case "bri":
                        begin = c.ToAHSB().Alter(null, null, 0).Double();
                        end = c.ToAHSB().Alter(null, null, 1).Double();
                        break;

                    case "hue":
                        m_calc = (double loc) => ColorWheel.GetColor(loc);
                        m_isGradient = false;
                        begin  = c;
                        end    = c;
                        break;

                    default:
                        Debug.Assert(false);

                        begin = Colors.Yellow.Double();
                        end   = Colors.Green.Double();
                        break;

                }

                if (m_begin != begin.ToColor() || m_end != end.ToColor())
                {
                    m_begin = begin.ToColor();
                    m_end = end.ToColor();

                    UpdateImage();
                }
            }
        }

        public string SliderColorComponent
        {
            get;
            set;
        }

        public ColorComponentSlider(
        )
        {
#if SILVERLIGHT
            this.DefaultStyleKey = typeof(ColorComponentSlider);
#endif
            this.SizeChanged += (s, e) => UpdateImage();

            this.Minimum = 0;
            this.Maximum = 255;

            this.Loaded += (s, e) =>
            {
                UpdateColor(SliderColor);
            };
        }

        static ColorComponentSlider(
        )
        {
#if !SILVERLIGHT
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorComponentSlider), new FrameworkPropertyMetadata(typeof(ColorComponentSlider)));
#endif
        }

        public void UpdateImage(
        )
        {
            if (m_isGradient)
            {
                UpdateImageGradient();
            }
            else
            {
                UpdateImageBitmap();
            }
        }

        public void UpdateImageBitmap(
        )
        {
            Border                                     border;

#if !SILVERLIGHT
            int[]                                      pixels;
#endif
            border = this.Orientation == Orientation.Horizontal ? m_horzImageBorder : m_vertImageBorder;

            if (border != null && 
                m_calc != null && 
                border.ActualHeight != 0 && 
                border.ActualWidth != 0)
            {
                int width = (int) Math.Floor(border.ActualWidth);
                int height = (int) Math.Floor(border.ActualHeight);

                m_bitmap = Compat.CreateBitmap(width, height);
#if !SILVERLIGHT
                pixels = new int[width * height];
#endif
                for (int x = 0; x < width; ++x)
                {
                    for (int y = 0; y < height; ++y)
                    {
                        int pixValue = 0;

                        if (this.Orientation == Orientation.Horizontal)
                        {
                            pixValue = m_calc((this.Maximum / width) * x).ToARGB32();;
                        }
                        else
                        {
                            pixValue = m_calc((this.Maximum / height) * y).ToARGB32();;
                        }
#if SILVERLIGHT
                        m_bitmap.Pixels[y * width + x] = pixValue;
#else
                        pixels[y * width + x] = pixValue;
#endif
                    }   
                }
#if !SILVERLIGHT
                m_bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, Compat.GetBitmapStride(width), 0);
#endif
                border.Background = new ImageBrush()
                {
                    ImageSource = m_bitmap
                };
            }
        }

        public override void OnApplyTemplate(
        )
        {
            base.OnApplyTemplate();

            m_vertImageBorder = GetTemplateChild("VerticalTrackRectangle") as Border;
            m_horzImageBorder = GetTemplateChild("HorizontalTrackRectangle") as Border;

            if (m_vertImageBorder != null)
            {
                m_vertImageBorder.SizeChanged += (s, e) => UpdateImage();
            }

            if (m_horzImageBorder != null)
            {
                m_horzImageBorder.SizeChanged += (s, e) => UpdateImage();
            }

            // UpdateImage();
        }

        public void UpdateBarColor(
            Func<double, DoubleColor>                         calculator
        )
        {
            m_isGradient = false;
            m_calc = calculator;

            UpdateImage();
        }

        public void UpdateImageGradient(
        )
        {
            LinearGradientBrush                         lg;
            lg = new LinearGradientBrush();

            if (m_horzImageBorder != null)
            {
                if (Orientation == Orientation.Horizontal)
                {
                    m_horzImageBorder.Child = null;

                    lg.StartPoint = new System.Windows.Point(0, 0.5);
                    lg.EndPoint = new System.Windows.Point(1, 0.5);

                    lg.GradientStops.Add(new GradientStop()
                    {
                        Color = m_begin,
                        Offset = 0
                    });

                    lg.GradientStops.Add(new GradientStop()
                    {
                        Color = m_end,
                        Offset = 1
                    });

                    m_horzImageBorder.Background = lg;
                }
                else
                {
                    m_vertImageBorder.Child = null;

                    lg.StartPoint = new System.Windows.Point(0.5, 0.0);
                    lg.EndPoint = new System.Windows.Point(0.5, 1.0);

                    lg.GradientStops.Add(new GradientStop()
                    {
                        Color = m_begin,
                        Offset = 0
                    });

                    lg.GradientStops.Add(new GradientStop()
                    {
                        Color = m_end,
                        Offset = 1
                    });

                    m_horzImageBorder.Background = lg;
                }
            }
        }

        public void UpdateBarColor(
            DoubleColor                                  begin,
            DoubleColor                                  end
        )
        {
            m_isGradient = true;

            m_begin = begin.ToColor();
            m_end   = end.ToColor();;

            UpdateImage();
        }
    }
}
