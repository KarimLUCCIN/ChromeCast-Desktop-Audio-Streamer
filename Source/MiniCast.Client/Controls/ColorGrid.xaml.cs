using ColorWheel.Controls;
using ColorWheel.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MiniCast.Client.Controls
{
    /// <summary>
    /// Interaction logic for ColorGrid.xaml
    /// </summary>
    public partial class ColorGrid : UserControl
    {
        private GradientStopPinpoint[] m_samplesb = null;

        public event Handler<int> SelectColored;
        public event EventHandler ColorsUpdated;

        public ColorGrid()
        {
            InitializeComponent();

            this.Loaded += (s, e) => Draw();
            this.SizeChanged += (s, e) =>
            {
                Draw();
                DrawPointers();
            };
        }

        #region Saturation dependency property

        public static readonly DependencyProperty SaturationProperty =
           DependencyProperty.Register("Saturation", typeof(double), typeof(ColorGrid),
           new PropertyMetadata(1.0, new PropertyChangedCallback(OnSaturationChanged)));

        public double Saturation
        {
            get
            {
                return (double)GetValue(SaturationProperty);
            }
            set
            {
                SetValue(SaturationProperty, value);
            }
        }

        public static void OnSaturationChanged(
            DependencyObject source,
            DependencyPropertyChangedEventArgs e
        )
        {
            ColorGrid me;
            double value;

            me = source as ColorGrid;
            value = (double)e.NewValue;

            if (e.OldValue != e.NewValue)
            {
                me.Draw();
                me.DrawPointers();
            }
        }


        #endregion

        #region Palette property

        public static readonly DependencyProperty GradientProperty =
            DependencyProperty.Register("Gradient", typeof(GradientSpan), typeof(ColorGrid),
            new PropertyMetadata(null, new PropertyChangedCallback(OnGradientChanged)));

        public GradientSpan Gradient
        {
            get
            {
                return (GradientSpan)GetValue(GradientProperty);
            }
            set
            {
                SetValue(GradientProperty, value);
            }
        }

        public static void OnGradientChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            ColorGrid me;
            GradientSpan value;

            me = source as ColorGrid;
            value = e.NewValue as GradientSpan;

            if (e.OldValue != e.NewValue)
            {
                if (me.Gradient != null)
                {
                    me.UnsubscribeFromPalette();
                }
                me.SubscribeToPalette();
                me.DrawPointers();
            }
        }

        private void SubscribeToPalette()
        {
            if (Gradient != null)
            {
                Gradient.GradientChanged += Gradient_GradientChanged;
                Gradient_GradientChanged(Gradient);
            }
        }

        private void Gradient_GradientChanged(GradientSpan src)
        {
            DrawPointers();

            canvasBrush.GradientStops.Clear();
            if (src != null)
            {
                foreach (var stop in src.OrderedStops)
                {
                    canvasBrush.GradientStops.Add(new GradientStop(stop.color, stop.offset));
                }
            }
        }

        private void UnsubscribeFromPalette()
        {
            Gradient.GradientChanged -= Gradient_GradientChanged;
        }

        void ColorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DrawPointers();
        }

        #endregion

        public void Draw()
        {
            //            AHSL hsl;
            //            int width = (int)imgBorder.ActualWidth;
            //            int height = (int)imgBorder.ActualHeight;
            //#if !SILVERLIGHT
            //            int[] pixels = new int[width * height];
            //#endif
            //            var bitmap = Compat.CreateBitmap(width, height);

            //            for (int x = 0; x < width; ++x)
            //            {
            //                for (int y = 0; y < height; ++y)
            //                {
            //                    hsl = new AHSL(x / (width / 360.0), Saturation, 1.0 - (((double)y) / height), 1.0);
            //#if !SILVERLIGHT
            //                    pixels[y * width + x] = hsl.Double().ToARGB32();
            //#else
            //                    bitmap.Pixels[y * width + x] = hsl.Double().ToARGB32();
            //#endif
            //                }
            //            }
            //#if !SILVERLIGHT
            //            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, Compat.GetBitmapStride(width), 0);
            //#endif
            //            spectrum.Height = height;
            //            spectrum.Width = width;

            //            spectrum.Source = bitmap;
        }

        private static double NormalizeOffset(double offset)
        {
            return Math.Max(0.01f, Math.Min(.96f, offset));
        }

        /// 
        /// <summary>
        /// Draw color schema tool (Monochromatic, Analogous ...)</summary>
        /// 
        public void DrawPointers(bool colorOnly = false)
        {
            double width = imgBorder.ActualWidth;
            double height = imgBorder.ActualHeight;
            var p = Gradient;
            double diam = 16;
            //AHSL hsl;

            if (Gradient != null)
            {
                var stops = Gradient.InteriorStops.ToArray();

                if (m_samplesb == null || m_samplesb.Length != stops.Length)
                {
                    if (m_samplesb != null)
                    {
                        canvasSpectrum.Children.Clear();
                    }

                    m_samplesb = new GradientStopPinpoint[stops.Length];

                    for (int i = 0; i < m_samplesb.Length; ++i)
                    {
                        m_samplesb[i] = new GradientStopPinpoint()
                        {
                            Opacity = 0.8,
                            IsHitTestVisible = true,
                            Width = diam,
                            Height = diam,
                            CurrentColor = stops[i].Color,
                            Tag = i,
                            Cursor = Cursors.Hand,
                            PaletteColor = stops[i]
                        };

                        canvasSpectrum.Children.Add(m_samplesb[i]);
                        SetPointEvents(m_samplesb[i]);

                        m_samplesb[i].SetValue(Canvas.LeftProperty, width * stops[i].Offset);
                        m_samplesb[i].SetValue(Canvas.TopProperty, height / 2);
                    }
                }

                for (int i = 0; i < m_samplesb.Length; ++i)
                {
                    m_samplesb[i].SetValue(Canvas.LeftProperty, width * stops[i].Offset);
                    m_samplesb[i].SetValue(Canvas.TopProperty, height / 2);
                    //var x = (double)m_samplesb[i].GetValue(Canvas.LeftProperty);
                    //var y = (double)m_samplesb[i].GetValue(Canvas.LeftProperty);

                    //hsl = p.Colors[i].DoubleColor.ToAHSL();

                    //double x;
                    //double y = height * (1 - hsl.Luminance);

                    //if (hsl.Luminance == 0 || hsl.Luminance == 1)
                    //{
                    //    x = (double)m_samplesb[i].GetValue(Canvas.LeftProperty);
                    //}
                    //else
                    //{
                    //    x = (width / 360.0) * hsl.HueDegree;
                    //}

                    //m_samplesb[i].CurrentColor = p.Colors[i].DoubleColor.ToColor();

                    //x -= diam / 2;
                    //y -= diam / 2;

                    //if (!colorOnly)
                    //{
                    //    m_samplesb[i].SetValue(Canvas.LeftProperty, x);
                    //    m_samplesb[i].SetValue(Canvas.TopProperty, y);
                    //}
                }
            }
        }

        #region Private methods

        private void SelectColor(GradientStopPinpoint pp)
        {
            //for (int i = 0; i < Gradient.Colors.Count; ++i)
            //{
            //    Gradient.Colors[i].IsSelected = (pp == m_samplesb[i]);
            //}

            //if (SelectColored != null)
            //{
            //    SelectColored(this, new EventArg<int>()
            //    {
            //        Value = (int)pp.Tag
            //    });
            //}
        }

        private void SetPointEvents(
            GradientStopPinpoint e
        )
        {
            var drag = false;
            Point shift = new Point();

            e.MouseLeftButtonDown += (s, ev) =>
            {
                drag = true;
                shift = ev.GetPosition(s as FrameworkElement);
                SelectColor(s as GradientStopPinpoint);
                //selected = Gradient.Colors[(int)(s as FrameworkElement).Tag];
                e.CaptureMouse();

                var pinpoint = (GradientStopPinpoint)s;
                foreach (var pin in m_samplesb)
                {
                    if (pinpoint != pin)
                    {
                        pin.IsHitTestVisible = false;
                    }
                }

            };

            e.MouseLeftButtonUp += (s, ev) =>
            {
                drag = false;
                e.ReleaseMouseCapture();

                foreach (var pin in m_samplesb)
                {
                    pin.IsHitTestVisible = true;
                }

                bool outOfOrder = false;
                for (int i = 0; i < m_samplesb.Length - 1; i++)
                {
                    if (m_samplesb[i].PaletteColor.Offset > m_samplesb[i + 1].PaletteColor.Offset)
                    {
                        outOfOrder = true;
                        break;
                    }
                }

                if (outOfOrder)
                {
                    var interiorColors = Gradient.InteriorStops.ToArray();

                    for (int i = 0; i < m_samplesb.Length; i++)
                    {
                        m_samplesb[i].PaletteColor = interiorColors[i];
                        m_samplesb[i].PaletteColor = interiorColors[i];
                    }
                }
            };

            e.MouseMove += (s, ev) =>
            {
                if (Gradient == null)
                {
                    return;
                }

                if (drag)
                {
                    double width = canvasSpectrum.ActualWidth;
                    double height = canvasSpectrum.ActualHeight;

                    var pinpoint = (GradientStopPinpoint)s;

                    if (!pinpoint.IsHitTestVisible)
                    {
                        return;
                    }

                    double diam = pinpoint.ActualWidth;

                    if (width <= 0)
                    {
                        return;
                    }

                    Point p = ev.GetPosition(canvasSpectrum);
                    p.X = p.X - shift.X + diam / 2;
                    p.Y = p.Y - shift.Y + diam / 2;

                    var newOffset = p.X / width;
                    pinpoint.PaletteColor.Offset = NormalizeOffset(newOffset);

                    //if (outOfOrder)
                    //{
                    //    var interiorColors = Gradient.InteriorStops.ToArray();

                    //    for (int i = 0; i < m_samplesb.Length; i++)
                    //    {
                    //        m_samplesb[i].PaletteColor = interiorColors[i];
                    //        m_samplesb[i].CurrentColor = interiorColors[i].Color;
                    //    }
                    //}

                    //AHSL hsl = selected.DoubleColor.ToAHSL();

                    //double x = Math.Min(Math.Max(p.X, 0), width);
                    //double y = Math.Min(Math.Max(p.Y, 0), height);

                    //hsl.Luminance = 1 - (y / height);
                    //hsl.HueDegree = 360 * (x / width);
                    //hsl.Saturation = Saturation;

                    //if (prev != null)
                    //{
                    //    selected.DoubleColor = hsl.Double();
                    //    if (ColorsUpdated != null)
                    //    {
                    //        ColorsUpdated(this, EventArgs.Empty);
                    //    }
                    //}
                    //prev = p;
                }
            };
        }

        #endregion
    }
}
