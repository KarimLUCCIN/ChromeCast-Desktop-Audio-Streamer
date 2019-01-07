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
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.Windows.Input;
    using System.Diagnostics;
    using System.Windows;
    using System;
    using ColorWheel.Core;
    using System.ComponentModel;
    using System.Collections.Specialized;

    public partial class BrightnessSaturationControl: 
        UserControl
    {
        private ColorPinpoint[]                         m_samplesb = null;

        public event Handler<int>                       SelectColored;
        public event EventHandler                       ColorsUpdated;

        public BrightnessSaturationControl()
        {
            InitializeComponent();

            this.Loaded      += (s, e) => Draw();

            this.SizeChanged += (s, e) => 
            { 
                Draw(); 
                DrawPointers(); 
            };
        }

        public void Draw(
        )
        {
            AHSB                                        hsb;
            int                                         width  = (int) imgBorder.ActualWidth; 
            int                                         height = (int) imgBorder.ActualHeight;
#if !SILVERLIGHT
            int[]                                       pixels = new int[width * height];
#endif
            var brisatBitmap = Compat.CreateBitmap(width, height);

            for (int x = 0; x < width; ++x)
            {
                int col = x / 6;
                for (int y = 0; y < height; ++y)
                {
                    int   row = y / 6;
                    Color c = Colors.Red;

                    if (col % 2 == 0 && row % 2 == 0)
                    {
                        c = Colors.Red;
                    }
                    else if (col % 2 == 0 && row % 2 == 1)
                    {
                        c = Colors.Blue;
                    }
                    else if (col % 2 == 1 && row % 2 == 1)
                    {
                        c = Colors.Green;
                    }
                    else 
                    {
                        c = Colors.Yellow;
                    }

                    hsb = c.Double().ToAHSB();
                    hsb.Saturation = x / (double) width;
                    hsb.Brightness = 1.0 - y / (double) height;

#if !SILVERLIGHT
                    pixels[y * width + x] = hsb.Double().ToARGB32();
#else
                    brisatBitmap.Pixels[y * width + x] = hsb.Double().ToARGB32();
#endif
                }
            }
#if !SILVERLIGHT
            brisatBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, Compat.GetBitmapStride(width), 0);
#endif
            brisat.Height = height;
            brisat.Width  = width;

            brisat.Source = brisatBitmap;
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
            AHSB    hsb;

            if (Palette != null)
            {
                if (m_samplesb == null || m_samplesb.Length != p.Colors.Count)
                {
                    if (m_samplesb != null)
                    {
                        canvasBriSat.Children.Clear();
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
                            PaletteColor     = Palette.Colors[i],
                            IsMain           = i == 0
                        };

                        m_samplesb[i].SetValue(ToolTipService.ToolTipProperty, p.Colors[i].Name);
                        canvasBriSat.Children.Add(m_samplesb[i]);
                        SetPointEvents(m_samplesb[i]);
                    }

                    Debug.WriteLine("ColorPaletteControl.InvalidateColors - recreate all color pointers");
                }

                for (int i = 0; i < m_samplesb.Length; ++i)
                {
                    hsb = p.Colors[i].Color;

                    double x = (hsb.Saturation * width) - diam / 2;
                    double y = (height - (hsb.Brightness * height)) - diam / 2;

                    m_samplesb[i].CurrentColor = p.Colors[i].DoubleColor.ToColor();

                    if (!colorOnly)
                    {
                        m_samplesb[i].SetValue(Canvas.LeftProperty, x);
                        m_samplesb[i].SetValue(Canvas.TopProperty, y);
                    }
                }
            }
            Debug.WriteLine("ColorPaletteControl.InvalidateColors");
        }

        public static readonly DependencyProperty PaletteProperty = 
            DependencyProperty.Register("Palette", typeof(Palette), typeof(BrightnessSaturationControl), new PropertyMetadata(null, new PropertyChangedCallback(OnPaletteChanged)));

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
            BrightnessSaturationControl                 me;
            Palette                                     value;

            me    = source as BrightnessSaturationControl;
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
            var                                         drag = false;
            PaletteColor                                selected = null;
            Point?                                      prev = null;

            e.MouseLeftButtonDown += (s, ev) =>
            {
                drag = true;
                prev = null;

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
                    double width  = brisat.Width;
                    double height = brisat.Height;
                    double diam   = (s as ColorPinpoint).ActualWidth;

                    Point p = ev.GetPosition(canvasBriSat);
                    AHSB  hsb = selected.Color;

                    hsb.Saturation = p.X / width;
                    hsb.Brightness = 1 - p.Y / height;

                    if (prev != null)
                    {
                        selected.Color = hsb;

                        double x = (hsb.Saturation * width) - diam / 2;
                        double y = (height - (hsb.Brightness * height)) - diam / 2;

                        (s as FrameworkElement).SetValue(Canvas.LeftProperty, x);
                        (s as FrameworkElement).SetValue(Canvas.TopProperty, y);

                        DrawPointers();

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
