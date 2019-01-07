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
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using ColorWheel.Core;

    public enum WheelPaintMethod
    {
        Saturation       = 0,
        Luminance        = 1,
        Brightness       = 2,
        InverseLuminance = 3
    }

    public class WheelPaintMethodConverter: IValueConverter
    {

        public object Convert(
            object                                      value, 
            Type                                        targetType, 
            object                                      parameter, 
            CultureInfo                                 culture
        )
        {
            return (int) value;
        }

        public object ConvertBack(
            object                                      value, 
            Type                                        targetType, 
            object                                      parameter, 
            CultureInfo                                 culture
        )
        {
            return ((WheelPaintMethod) (int) value);
        }
    }

    public partial class ColorWheelControl: UserControl
    {
        #region Private Fields

        private const int                               COLOR_POINTER_ZORDER          = 100;
        private const int                               COLOR_LINE_ZORDER             = 80;
        private const int                               COLOR_POINTER_ZORDER_SELECTED = 200;
        private const int                               COLOR_POINTER_DIAMETER        = 16;

        private bool                                    m_rotatingWheel = false;
        private Point                                   m_prevPoint;

        // (-90 make red on top by default)
        private double                                  m_angleShift = 0;               

        private ColorPinpoint                           m_main;
        private ColorPinpoint[]                         m_schemaElements;
        private Line[]                                  m_lines;

        private List<UIElement>                         m_examples = new List<UIElement>();
        private bool                                    m_allowSaturationLevel = true;

        public event Handler<int>                       ColorSelected;
        public event EventHandler                       ColorsUpdated;

        #endregion

        public ColorWheelControl(
        )
        {
            InitializeComponent();
            this.IsTabStop = true;

            wheel.MouseLeftButtonDown += OnWheelMouseLeftButtonDown;

            this.SizeChanged += (s, e) =>
            {
                Resize();
            };
        }

        #region Palette Dependency Property

        public static readonly DependencyProperty PaletteProperty =
           DependencyProperty.Register("Palette", typeof(Palette), typeof(ColorWheelControl),  
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
            ColorWheelControl                           me;
            Palette                                     value;

            me = source as ColorWheelControl;
            value = e.NewValue as Palette;

            if (e.OldValue != e.NewValue)
            {
                if (me.Palette != null)
                {
                    me.UnsubscribeFromPalette();
                }
                me.SubscribeToPalette();
                me.Refresh();
            }
        }

        #endregion 

        #region PaintMethod Dependency Property

        public static readonly DependencyProperty PaintMethodProperty =
           DependencyProperty.Register("PaintMethod", typeof(WheelPaintMethod), typeof(ColorWheelControl),  
           new PropertyMetadata(WheelPaintMethod.Saturation, new PropertyChangedCallback(OnPaintMethodChanged)));

        public WheelPaintMethod PaintMethod
        {
            get
            {
                return (WheelPaintMethod) GetValue(PaintMethodProperty);
            }
            set
            {
                SetValue(PaintMethodProperty, value);
            }
        }

        public static void OnPaintMethodChanged(
            DependencyObject                            source, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            ColorWheelControl                           me;
            WheelPaintMethod                            value;

            me = source as ColorWheelControl;
            value = (WheelPaintMethod) e.NewValue;

            if (e.OldValue != e.NewValue)
            {
                me.Refresh();
            }
        }

        #endregion 

        #region Public Properties and Methods

        public bool AllowSaturationEdit
        {
            get
            {
                return m_allowSaturationLevel;
            }
            set
            {
                if (m_allowSaturationLevel != value)
                {
                    m_allowSaturationLevel = value;

                    Redraw();
                    DrawPointers();
                }
            }
        }

        public ColorWheelBase Wheel
        {
            get
            {
                return (Palette != null ? Palette.Wheel : null);
            }
        }

        public void Refresh(
        )
        {
            Redraw();
            DrawPointers();
        }

        #endregion

        #region Update colors when palette is changed

        private void SelectColor(
            ColorPinpoint                               pp
        )
        {
            for (int i = 0; i < Palette.Colors.Count; ++i)
            {
                Palette.Colors[i].IsSelected = (pp == m_schemaElements[i]);
            }

            if (ColorSelected != null)
            {
                ColorSelected(this, new EventArg<int>()
                {
                    Value = (int) ((pp).Tag)
                });
            }
        }

        private void SubscribeToPalette(
        )
        {
            Palette.PropertyChanged += OnPalettePropertyChanged;
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
            if (String.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Wheel")
            {
                Refresh();
            }
            else
            {
                DrawPointers();
            }
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

        /// 
        /// <summary>
        /// Draw color wheel</summary>
        /// 
        protected void Redraw(
        )
        {
            int                                         radius  = (int) (wheelRoot.ActualHeight / 2);
            WriteableBitmap                             bm;
            double                                      sectorWidth = SectorWidth;
            ColorWheelBase                              wl;

            DateTime d = DateTime.Now;
            wl         = Wheel ?? new RYGBColorWheel();
            bm         = Compat.CreateBitmap(radius * 2, radius * 2);

#if !SILVERLIGHT
            Int32[]                                     pixels = new Int32[radius * 2 * radius * 2];
#endif
            for (double x = -radius; x < radius; x++)
            {
                int height = (int) Math.Sqrt(radius * radius - x * x);

                for (double y = -height; y < height; y++)
                {
                    int offset;
                    double degree;
                    var val = GetColorFromPoint(radius, x, y, wl, out offset, out degree);
#if SILVERLIGHT 
                    bm.Pixels[offset] = val;
#else
                    pixels[offset] = val;
#endif
                }
            }

#if !SILVERLIGHT
            bm.WritePixels(new Int32Rect(0, 0, radius * 2, radius * 2), pixels, Compat.GetBitmapStride(radius * 2), 0);
#endif
            wheel.Source = bm;
            DrawPointers();

            Debug.WriteLine("ColorWheelControl.Redraw: " + (DateTime.Now - d).TotalMilliseconds);
        }

        private int GetColorFromPoint(
            int                                             radius,
            double                                          x,
            double                                          y,
            ColorWheelBase                                  wl,
            out int                                         offset,
            out double                                      degree
        )
        {
            double                                          step;

            double curRadius  = Math.Sqrt(x * x + y * y);
            double radians    = Math.Atan2(y, x);
            double gradRadius = radius - (SectorWidth * 3.0);

            int sector = (int) ((radius - curRadius) / SectorWidth);
            degree     = wl.FixAngle((360.0 - radians * (180 / Math.PI)) + m_angleShift);

            offset  = (int) (((int) (y + radius)) * (radius * 2) + (int) (x + radius));
            int val = 0;

            switch (sector)
            {
                case 0:
                    step = 360 / 6;
                    degree = ((int)((degree + step / 2) / step)) * step;
                    break;

                case 1:
                    step = 360 / 12;
                    degree = ((int)((degree + step / 2) / step)) * step;
                    break;

                case 2:
                    step = 360 / 24;
                    degree = ((int)((degree + step / 2) / step)) * step;
                    break;
            }

            if (sector <= 2)
            {
                val = wl.GetColor(degree).ToARGB32();
            }
            else
            {
                val = GetARGB32Color(wl, degree, curRadius, gradRadius);
            }

            return val;
        }

        protected int GetARGB32Color(
            ColorWheelBase                              wl,
            double                                      degree,
            double                                      currentRadius,
            double                                      gradRadius
        )
        {
            return GetDoubleColor(wl, degree, currentRadius, gradRadius).ToARGB32();
        }

        protected DoubleColor GetDoubleColor(
            ColorWheelBase                              wl,
            double                                      degree,
            double                                      currentRadius,
            double                                      gradRadius
        )
        {
            DoubleColor val;
            AHSL        hsl;
            DoubleColor dc = wl.GetColor(degree);

            switch (PaintMethod)
            {
                case WheelPaintMethod.Brightness:
                    val = dc.ToAHSB().Alter(null, 1.0, currentRadius / gradRadius).Double();
                    break;

                case WheelPaintMethod.InverseLuminance:
                    hsl = dc.ToAHSL();
                    hsl.Luminance = currentRadius / gradRadius;
                    val = hsl.Double();
                    break;

                case WheelPaintMethod.Luminance:
                    hsl = dc.ToAHSL();
                    hsl.Luminance = 1.0 - currentRadius / gradRadius;
                    val = hsl.Double();
                    break;

                case WheelPaintMethod.Saturation:
                    val = dc.ToAHSB().Alter(null, currentRadius / gradRadius).Double();
                    break;

                default:
                    throw new NotImplementedException();
            }

            return val;
        }

        #endregion

        #region Utilities

        protected double GetAngleFromPoint(
            Point                                       p
        )
        {
            return Math.Atan2(-p.Y + Radius, p.X - Radius) * (180 / Math.PI) + m_angleShift;
        }

        protected DoubleColor GetColorFromPoint(
            AHSB                                        orgColor,
            Point                                       pprev,
            Point                                       pnew
        )
        {
            double      y2   = -pnew.Y + Radius;
            double      x2   = pnew.X - Radius;
            double      coff = Math.Min(1.0, Math.Max(0.0, Math.Sqrt(y2 * y2 + x2 * x2) / ValueRadius));
            DoubleColor val;
            AHSL        hsl;

            switch (PaintMethod)
            {
                case WheelPaintMethod.Brightness:
                    val = orgColor.Alter(null, 1.0, coff).Double();
                    break;

                case WheelPaintMethod.Luminance:
                    coff = 1 - coff;
                    goto case WheelPaintMethod.InverseLuminance;

                case WheelPaintMethod.InverseLuminance:
                    hsl = orgColor.Double().ToAHSL();
                    hsl.Luminance = coff;
                    val = hsl.Double();
                    break;

                case WheelPaintMethod.Saturation:
                    if (orgColor.Brightness == 0)
                    {
                        // orgColor.Brightness = 1 / 1000.0;
                    }
                    val = orgColor.Alter(null, coff).Double();
                    break;

                default:
                    throw new NotImplementedException();
            }

            return val;
        }

        #endregion

        #region Event Handlers

        private void OnWheelMouseLeftButtonDown(
            object                                      sender, 
            MouseButtonEventArgs                        e
        )
        {
            if (e.ClickCount == 2)
            {
                int radius  = (int) (wheel.ActualWidth / 2);
                Point p     = e.GetPosition(wheel);

                double y = (p.Y - radius);
                double x = p.X - radius;

                int    offset;
                double degree;

                GetColorFromPoint(radius, x, y, Wheel ?? new RGBColorWheel(), out offset, out degree);
                if (Palette.Schema != PaletteSchemaType.Custom)
                {
                    Palette.BaseAngle = degree;
                }
            }
        }

        private void OuterWheelMouseMove(
            object                                      sender, 
            System.Windows.Input.MouseEventArgs         e
        )
        {
            if (m_rotatingWheel)
            {
                Point p = e.GetPosition(this);

                double dx = p.X - m_prevPoint.X;
                double dy = p.Y - m_prevPoint.Y;

                m_prevPoint = p;

                double prevAngle = m_angleShift;

                m_angleShift -= dx / 1.0;
                m_angleShift += dy / 1.0;

                if (prevAngle != m_angleShift)
                {
#if SILVERLIGHT
                    this.Dispatcher.BeginInvoke(Redraw);
#else
                    Redraw();
#endif
                }
            }
        }

        private void Resize(
        )
        {
            wheel.Width  = Math.Max(0, Math.Min(wheelRoot.ActualHeight, wheelRoot.ActualWidth) - 2);
            wheel.Height = wheel.Width;
            SectorWidth  = (wheel.Width / 2) / 20;

            outerEll.Height     = wheel.Width + 2;
            outerEll.Width      = wheel.Width + 2;

            editBorder.SetValue(Canvas.LeftProperty, SectorWidth * 3.0 - 1);
            editBorder.SetValue(Canvas.TopProperty, SectorWidth * 3.0 - 1);

            editBorder.Height   = wheel.Width + 3 - (SectorWidth * 6.0);
            editBorder.Width    = wheel.Width + 3 - (SectorWidth * 6.0);

            Redraw();
        }

        #endregion

        #region Private Properties

        /// 
        /// <summary>
        /// Wheel radius</summary>
        /// 
        private int Radius
        {
            get
            {
                return (int) (wheel.Height / 2);
            }
        }

        /// 
        /// <summary>
        /// Max radius for color pointers</summary>
        /// 
        private double ValueRadius
        {
            get
            {
                return (double) Radius - (SectorWidth * 3.0) + COLOR_POINTER_DIAMETER / 2.0;
            }
        }

        private double SectorWidth
        {
            get;
            set;
        }

        #endregion

        #region Draw Color pointers and set their Event Handlers

        /// 
        /// <summary>
        /// Draw all color pointers for palette</summary>
        /// 
        protected void DrawPointers(
        )
        {
            int      radius = (int) Radius;
            double   ewidth = COLOR_POINTER_DIAMETER;
            double   pinradius = ewidth / 2;

            if (Palette != null)
            {
                double gradRadius = ValueRadius;
                double[] angles = new double[Palette.Colors.Count];

                double[]      radCoff = new double[Palette.Colors.Count];
                DoubleColor[] wheelColors = new DoubleColor[Palette.Colors.Count];

                for (int i = 0; i < angles.Length; ++i)
                {
                    PaletteColor pc   = Palette.Colors[i];
                    double       coff = 1;

                    switch (PaintMethod)
                    {
                        case WheelPaintMethod.Brightness:
                            coff = pc.DoubleColor.ToAHSB().Brightness;
                            var c = pc.DoubleColor.ToAHSB();
                            c.Saturation = 1.0;
                            wheelColors[i] = c.Double();
                            break;

                        case WheelPaintMethod.InverseLuminance:

                            coff = pc.DoubleColor.ToAHSL().Luminance;
                            var c1 = pc.DoubleColor.ToAHSL();
                            c1.Saturation = 1.0;
                            wheelColors[i] = c1.Double();
                            break;

                        case WheelPaintMethod.Saturation:
                            coff = pc.DoubleColor.Saturation;
                            var c2 = pc.DoubleColor.ToAHSB();
                            c2.Brightness = 1.0;
                            wheelColors[i] = c2.Double();
                            break;

                        case WheelPaintMethod.Luminance:
                            coff = 1.0 - pc.DoubleColor.ToAHSL().Luminance;
                            var c3 = pc.DoubleColor.ToAHSL();
                            c3.Saturation = 1.0;
                            wheelColors[i] = c3.Double();
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    angles[i]  = Wheel.GetAngle(pc.DoubleColor);
                    radCoff[i] = coff;
                }

                if (m_schemaElements == null || m_schemaElements.Length != angles.Length)
                {
                    if (m_schemaElements != null)
                    {
                        foreach (Line l in m_lines)
                        {
                            canvas.Children.Remove(l);
                        }

                        foreach (ColorPinpoint e in m_schemaElements)
                        {
                            canvas.Children.Remove(e);
                        }
                    }

                    m_schemaElements = new ColorPinpoint[angles.Length];
                    m_lines = new Line[angles.Length];

                    for (int i = 0; i < m_schemaElements.Length; ++i)
                    {
                        m_schemaElements[i] = new ColorPinpoint()
                        {
                            Opacity          = 0.9,
                            IsHitTestVisible = true,
                            Width            = ewidth,
                            Height           = ewidth,
                            CurrentColor     = Colors.Black,
                            Tag              = i,
                            PaletteColor     = Palette.Colors[i]
                        };

                        m_schemaElements[i].SetValue(Canvas.ZIndexProperty, COLOR_POINTER_ZORDER);
                        m_schemaElements[i].SetValue(ToolTipService.ToolTipProperty, Palette.Colors[i].Name);

                        canvas.Children.Add(m_schemaElements[i]);

                        m_lines[i] = new Line()
                        {
                            Opacity          = 0.0,
                            StrokeThickness  = 2,
                            Stroke           = Colors.Black.ToBrush()
                        };
                    
                        m_lines[i].SetValue(Canvas.ZIndexProperty, COLOR_LINE_ZORDER);
                        canvas.Children.Add(m_lines[i]);

                        if (i == 0)
                        {
                            m_main = m_schemaElements[i];
                            m_main.IsMain = true;
                            SetMainPointEvents(m_main);
                        }
                        else
                        {
                            SetSecondaryPointEvents(m_schemaElements[i]);
                        }
                    }
                }

                for (int i = 0; i < m_schemaElements.Length; ++i)
                {
                    double angle = Wheel.FixAngle(angles[i] - m_angleShift);
                    double rad  = MathEx.ToRadians(angle);
                    double coff = radCoff[i];

                    double x = Math.Cos(rad) * (gradRadius - pinradius) * coff + radius;
                    double y = -Math.Sin(rad) * (gradRadius - pinradius) * coff + radius;

                    AHSB hsb = Palette.Colors[i].Color;
                    
                    x -= pinradius;
                    y -= pinradius;

                    m_schemaElements[i].SetValue(Canvas.LeftProperty, x);
                    m_schemaElements[i].SetValue(Canvas.TopProperty, y);
                    m_schemaElements[i].CurrentColor = hsb.Double().ToColor();

                    m_lines[i].X1 = radius;
                    m_lines[i].Y1 = radius;

                    m_lines[i].X2 = x + pinradius;
                    m_lines[i].Y2 = y + pinradius;
                }
            }
        }

        /// 
        /// <summary>
        /// Show/Hide lines between color pointers</summary>
        /// 
        private void ShowLines(
            bool                                        show
        )
        {
            foreach (Line l in m_lines)
            {
                l.Opacity = show ? 0.3 : 0;
            }

            Debug.WriteLine("ColorWheelControl.ShowLines");
        }

        /// 
        /// <summary>
        /// Main color pointer event handle initializer</summary>
        /// 
        private void SetSecondaryPointEvents(
            ColorPinpoint                              e
        )
        {
            var                                         drag = false;
            Point?                                      prev = null;

            e.MouseEnter += (s, ev) =>
            {
                ShowLines(true);
                (s as UIElement).SetValue(Canvas.ZIndexProperty, COLOR_POINTER_ZORDER_SELECTED);
            };

            e.MouseLeave += (s, ev) =>
            {
                if (!drag)
                {
                    ShowLines(false);
                    (s as UIElement).SetValue(Canvas.ZIndexProperty, COLOR_POINTER_ZORDER);
                }
            };

            e.MouseLeftButtonDown += (s, ev) =>
            {
                if (ev.ClickCount == 2)
                {
                    int id = (int) (s as FrameworkElement).Tag;

                    if (Palette.MaxVectorIndex > 0)
                    {
                        Palette.Colors[id].VectorIndex ++;
                        Palette.Colors[id].VectorIndex %= Palette.MaxVectorIndex;
                        Palette.Refresh();
                    }
                }
                else
                {
                    drag = true;
                    prev = null;

                    e.CaptureMouse();
                    e.Cursor = Cursors.Hand;

                    ShowLines(true);
                    SelectColor(s as ColorPinpoint);
                }
            };

            e.MouseLeftButtonUp += (s, ev) =>
            {
                drag = false;
                prev = null;

                e.ReleaseMouseCapture();
                ShowLines(false);

                (s as UIElement).SetValue(Canvas.ZIndexProperty, COLOR_POINTER_ZORDER);
            };

            e.MouseMove += (s, ev) =>
            {
                double diam = (s as ColorPinpoint).ActualWidth;
                if (drag)
                {
                    Point p = ev.GetPosition(wheel);
                    if (prev != null)
                    {
                        ChangeSecondaryColorsForPointer((int) ((s as FrameworkElement).Tag), prev.Value, p);
                    }
                    prev = p;
                }
            };
        }

        /// 
        /// <summary>
        /// Main color pointer event handle initialize</summary>
        /// 
        private void SetMainPointEvents(
            ColorPinpoint                              e
        )
        {
            var                                         drag = false;
            Point?                                      prev = null;

            e.MouseEnter += (s, ev) =>
            {
                ShowLines(true);

                (s as UIElement).SetValue(Canvas.ZIndexProperty, COLOR_POINTER_ZORDER_SELECTED);
                m_main.SetValue(Canvas.ZIndexProperty, COLOR_POINTER_ZORDER_SELECTED + 1);
            };

            e.MouseLeave += (s, ev) =>
            {
                if (!drag)
                {
                    ShowLines(false);

                    (s as UIElement).SetValue(Canvas.ZIndexProperty, COLOR_POINTER_ZORDER);
                    m_main.SetValue(Canvas.ZIndexProperty, COLOR_POINTER_ZORDER);
                }
            };

            e.MouseLeftButtonDown += (s, ev) =>
            {
                drag = true;
                e.CaptureMouse();

                e.Cursor = Cursors.Hand;
                ShowLines(true);
                SelectColor(m_main);
            };

            e.MouseLeftButtonUp += (s, ev) =>
            {
                drag = false;
                prev = null;

                e.ReleaseMouseCapture();
                e.Cursor = Cursors.Arrow;
                ShowLines(false);

                (s as UIElement).SetValue(Canvas.ZIndexProperty, COLOR_POINTER_ZORDER);
                m_main.SetValue(Canvas.ZIndexProperty, COLOR_POINTER_ZORDER);
            };

            e.MouseMove += (s, ev) =>
            {
                if (drag)
                {
                    Point p = ev.GetPosition(wheel);
                    if (prev != null)
                    {
                        ChangePrimaryColorForPointer(prev.Value, p);
                    }
                    prev = p;
                }
            };
        }

        #endregion

        #region Change color palette when color pointer is dragged

        /// 
        /// <summary>
        /// Change primary color when user drags main color pointer. prevPoint is previous mouse location,
        /// currentPoint is current mouse point location</summary>
        /// 
        private void ChangePrimaryColorForPointer(
            Point                                       prevPoint,
            Point                                       currentPoint
        )
        {
            PaletteColor                                pc;
            AHSB                                        hsb;

            pc = Palette.Colors[0];
            if (Palette.Schema == PaletteSchemaType.Custom)
            {
                ChangeSecondaryColorsForPointer(0, prevPoint, currentPoint);
            }
            else
            {
                Palette.BaseAngle = GetAngleFromPoint(currentPoint);

                hsb = Palette.Colors[0].Color;
                pc.DoubleColor = GetColorFromPoint(hsb, prevPoint, currentPoint);
            }

            DrawPointers();
            if (ColorsUpdated != null)
            {
                ColorsUpdated(this, EventArgs.Empty);
            }
        }

        /// 
        /// <summary>
        /// Change secondary color when user drags main color pointer. prevPoint is previous mouse location,
        /// currentPoint is current mouse point location</summary>
        /// 
        private void ChangeSecondaryColorsForPointer(
            int                                         i,
            Point                                       prevPoint,
            Point                                       currentPoint
        )
        {
            PaletteColor                                pc;
            double                                      angle1;
            double                                      angle2;
            AHSB                                        hsb;
            AHSB                                        hsbNew;

            pc = Palette.Colors[i];

            switch (Palette.Schema)
            {
                case PaletteSchemaType.Monochromatic:
                    Palette.BaseAngle = GetAngleFromPoint(currentPoint);
                    break;

                case PaletteSchemaType.Custom:
                    angle1   = GetAngleFromPoint(prevPoint);
                    angle2   = GetAngleFromPoint(currentPoint);
                    hsb      = Palette.Colors[i].Color;
                    hsbNew   = (Wheel.GetColor(angle2)).ToAHSB();
                    pc.Color = hsb.Alter(hue: hsbNew.HueDegree);
                    break;

                default:
                    if (pc.VectorIndex == 0)
                    {
                        Palette.BaseAngle = GetAngleFromPoint(currentPoint);
                    }
                    else
                    {
                        switch (Palette.Schema)
                        {
                            case PaletteSchemaType.Analogous:
                            case PaletteSchemaType.SplitAnalogous:
                            case PaletteSchemaType.SplitComplementary:
                            case PaletteSchemaType.Tetrads:
                                Palette.Angle += GetAngleFromPoint(currentPoint) - GetAngleFromPoint(prevPoint);
                                break;

                            case PaletteSchemaType.Complementary:
                            case PaletteSchemaType.Triad:
                            case PaletteSchemaType.Quadrants:
                                Palette.BaseAngle += GetAngleFromPoint(currentPoint) - GetAngleFromPoint(prevPoint);
                                break;
                        }
                    }
                    break;
            }

            hsb = Palette.Colors[i].Color;
            pc.DoubleColor = GetColorFromPoint(hsb, prevPoint, currentPoint);

            DrawPointers();
            if (ColorsUpdated != null)
            {
                ColorsUpdated(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}
