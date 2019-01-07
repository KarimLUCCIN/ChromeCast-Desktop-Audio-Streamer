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

namespace ColorWheel.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Shapes;
    using System.Diagnostics;
    using ColorWheel.Core;
    using System.ComponentModel;
    using System.Collections.Specialized;

    public partial class SpectrumControl:
        UserControl
    {
        private ColorPinpoint[]                         m_samplesb       = null;

        public event Handler<int>                       SelectColored;
        public event EventHandler                       ColorsUpdated;

        public SpectrumControl()
        {
            InitializeComponent();

            this.Loaded      += (s, e) => Draw();
            this.SizeChanged += (s, e) => 
            { 
                Draw(); 
                DrawPointers(); 
            };
        }

        #region Saturation dependency property

         public static readonly DependencyProperty SaturationProperty = 
            DependencyProperty.Register("Saturation", typeof(double), typeof(SpectrumControl), 
            new PropertyMetadata(1.0, new PropertyChangedCallback(OnSaturationChanged)));

        public double Saturation
        {
            get
            {
                return (double) GetValue(SaturationProperty);
            }
            set
            {
                SetValue(SaturationProperty, value);
            }
        }

        public static void OnSaturationChanged(
            DependencyObject                            source, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            SpectrumControl                              me;
            double                                       value;

            me    = source as SpectrumControl;
            value = (double) e.NewValue;

            if (e.OldValue != e.NewValue)
            {
                me.Draw();                
                me.DrawPointers();
            }
        }


        #endregion

        #region Palette property

        public static readonly DependencyProperty PaletteProperty = 
            DependencyProperty.Register("Palette", typeof(Palette), typeof(SpectrumControl), 
            new PropertyMetadata(null, new PropertyChangedCallback(OnPaletteChanged)));

        public Palette Palette
        {
            get
            {
                return (Palette) GetValue(PaletteProperty);
            }
            set
            {
                SetValue(PaletteProperty, value);
            }
        }

        public static void OnPaletteChanged(
            DependencyObject                            source, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            SpectrumControl                              me;
            Palette                                     value;

            me    = source as SpectrumControl;
            value = e.NewValue as Palette;

            if (e.OldValue != e.NewValue)
            {
                if (me.Palette != null)
                {
                    me.UnsubscribeFromPalette();
                }
                me.SubscribeToPalette();
                me.DrawPointers();
            }
        }

        private void SubscribeToPalette(
        )
        {
            Palette.PropertyChanged          += OnPalettePropertyChanged;
            Palette.Colors.CollectionChanged += ColorCollectionChanged;

            foreach (PaletteColor c in Palette.Colors)
            {
                c.PropertyChanged += ColorPropertyChanged;
            }
        }

        void OnPalettePropertyChanged(
            object                                      sender, 
            PropertyChangedEventArgs                    e
        )
        {
            DrawPointers();
        }

        private void UnsubscribeFromPalette(
        )
        {
            Palette.PropertyChanged          -= OnPalettePropertyChanged;
            Palette.Colors.CollectionChanged -= ColorCollectionChanged;

            foreach (PaletteColor c in Palette.Colors)
            {
                c.PropertyChanged -= ColorPropertyChanged;
            }
        }

        void ColorPropertyChanged(
            object                                      sender, 
            PropertyChangedEventArgs                    e
        )
        {
            DrawPointers();
        }

        void ColorCollectionChanged(
            object                                      sender, 
            NotifyCollectionChangedEventArgs            e
        )
        {
            foreach (PaletteColor c in e.OldItems)
            {
                c.PropertyChanged -= ColorPropertyChanged;
            }

            foreach (PaletteColor c in e.NewItems)
            {
                c.PropertyChanged += ColorPropertyChanged;
            }

            DrawPointers();
        }

        #endregion

        public void Draw(
        )
        {
            AHSL                                        hsl;
            int                                         width  = (int) imgBorder.ActualWidth; 
            int                                         height = (int) imgBorder.ActualHeight;
#if !SILVERLIGHT
            int[]                                       pixels = new int[width * height];
#endif
            var bitmap = Compat.CreateBitmap(width, height);

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    hsl = new AHSL(x / (width / 360.0), Saturation, 1.0 - (((double) y) / height), 1.0);
#if !SILVERLIGHT
                    pixels[y * width + x] = hsl.Double().ToARGB32();
#else
                    bitmap.Pixels[y * width + x] = hsl.Double().ToARGB32();
#endif
                }
            }
#if !SILVERLIGHT
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, Compat.GetBitmapStride(width), 0);
#endif
            spectrum.Height = height;
            spectrum.Width  = width;

            spectrum.Source = bitmap;
        }

        /// 
        /// <summary>
        /// Draw color schema tool (Monochromatic, Analogous ...)</summary>
        /// 
        public void DrawPointers(
            bool                                        colorOnly = false
        )
        {
            double  width    = imgBorder.ActualWidth; 
            double  height   = imgBorder.ActualHeight;
            Palette p        = Palette;
            double  diam     = 16;
            AHSL    hsl;

            if (Palette != null)
            {
                if (m_samplesb == null || m_samplesb.Length != p.Colors.Count)
                {
                    if (m_samplesb != null)
                    {
                        canvasSpectrum.Children.Clear();
                    }

                    m_samplesb = new ColorPinpoint[p.Colors.Count];

                    for (int i = 0; i < m_samplesb.Length; ++i)
                    {
                        m_samplesb[i] = new ColorPinpoint()
                        {
                            Opacity          = 0.8,
                            IsHitTestVisible = true,
                            Width            = diam,
                            Height           = diam,
                            CurrentColor     = p.Colors[i].DoubleColor.ToColor(),
                            Tag              = i,
                            Cursor           = Cursors.Hand,
                            PaletteColor     = Palette.Colors[i]
                        };

                        m_samplesb[i].SetValue(ToolTipService.ToolTipProperty, p.Colors[i].Name);
                        canvasSpectrum.Children.Add(m_samplesb[i]);
                        SetPointEvents(m_samplesb[i]);
                    }

                    Debug.WriteLine("ColorPaletteControl.InvalidateColors - recreate all color pointers");
                }

                for (int i = 0; i < m_samplesb.Length; ++i)
                {
                    hsl = p.Colors[i].DoubleColor.ToAHSL();

                    double x;
                    double y = height * (1 - hsl.Luminance);

                    if (hsl.Luminance == 0 || hsl.Luminance == 1)
                    {
                        x = (double) m_samplesb[i].GetValue(Canvas.LeftProperty);
                    }
                    else
                    {
                        x = (width / 360.0) * hsl.HueDegree;
                    }

                    m_samplesb[i].CurrentColor = p.Colors[i].DoubleColor.ToColor();

                    x -= diam / 2;
                    y -= diam / 2;

                    if (!colorOnly)
                    {
                        m_samplesb[i].SetValue(Canvas.LeftProperty, x);
                        m_samplesb[i].SetValue(Canvas.TopProperty, y);
                    }
                }
            }
            Debug.WriteLine("ColorPaletteControl.InvalidateColors");
        }

        #region Private methods

        private void SelectColor(
            ColorPinpoint                               pp
        )
        {
            for (int i = 0; i < Palette.Colors.Count; ++i)
            {
                Palette.Colors[i].IsSelected = (pp == m_samplesb[i]);
            }

            if (SelectColored != null)
            {
                SelectColored(this, new EventArg<int>() 
                { 
                    Value = (int) pp.Tag
                });
            }
        }

        private void SetPointEvents(
            ColorPinpoint                               e
        )
        {
            var                                         drag     = false;
            PaletteColor                                selected = null;
            Point?                                      prev     = null;
            Point                                       shift    = new Point();

            e.MouseLeftButtonDown += (s, ev) =>
            {
                drag = true;
                prev = null;
                shift = ev.GetPosition(s as FrameworkElement);
                SelectColor(s as ColorPinpoint);
                selected = Palette.Colors[(int) (s as FrameworkElement).Tag];
                e.CaptureMouse();
            };

            e.MouseLeftButtonUp += (s, ev) =>
            {
                drag = false;
                e.ReleaseMouseCapture();
            };

            e.MouseMove += (s, ev) =>
            {
                if (drag)
                {
                    double width  = spectrum.Width;
                    double height = spectrum.Height;
                    double diam   = (s as ColorPinpoint).ActualWidth;

                    Point p = ev.GetPosition(canvasSpectrum);
                    p.X = p.X - shift.X + diam / 2;
                    p.Y = p.Y - shift.Y + diam / 2;

                    AHSL  hsl = selected.DoubleColor.ToAHSL();

                    double x = Math.Min(Math.Max(p.X, 0), width);
                    double y = Math.Min(Math.Max(p.Y, 0), height);

                    hsl.Luminance  = 1 - (y / height);
                    hsl.HueDegree  = 360 * (x / width);
                    hsl.Saturation = Saturation;
                    
                    if (prev != null)
                    {
                        selected.DoubleColor = hsl.Double();
                        if (ColorsUpdated != null)
                        {
                            ColorsUpdated(this, EventArgs.Empty);
                        }
                    }
                    prev = p;
                }
            };
        }

        #endregion
    }
}
