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

        public ColorGrid()
        {
            InitializeComponent();

            this.SizeChanged += (s, e) =>
            {
                DrawPointers();
            };
        }

        public GradientStop CurrentStop
        {
            get { return (GradientStop)GetValue(CurrentStopProperty); }
            set { SetValue(CurrentStopProperty, value); }
        }

        public static readonly DependencyProperty CurrentStopProperty =
            DependencyProperty.Register("CurrentStop", typeof(GradientStop), typeof(ColorGrid), new PropertyMetadata(null));

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

                StartPin.PaletteColor = Gradient.Start;
                StartPin.CurrentColor = Gradient.Start.Color;

                EndPin.PaletteColor = Gradient.End;
                EndPin.CurrentColor = Gradient.End.Color;

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
                    canvasBrush.GradientStops.Add(stop);
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

        private static double NormalizeOffset(double offset)
        {
            return Math.Max(0.01f, Math.Min(.96f, offset));
        }

        const int radius = 16;

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
                        CurrentStop = null;
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
                        m_samplesb[i].SetValue(Canvas.TopProperty, height / 2 - radius / 2);
                    }
                }

                for (int i = 0; i < m_samplesb.Length; ++i)
                {
                    m_samplesb[i].SetValue(Canvas.LeftProperty, width * stops[i].Offset);
                    m_samplesb[i].SetValue(Canvas.TopProperty, height / 2 - radius / 2);
                }
            }
        }

        #region Private methods

        private void SelectColor(GradientStopPinpoint pp)
        {
            CurrentStop = pp.PaletteColor;
        }

        private void CanvasSpectrum_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var mousePos = e.GetPosition(canvasSpectrum);

                double width = canvasSpectrum.ActualWidth;
                var newOffset = mousePos.X / width;

                Gradient.Stops.Add(new GradientStop(new Color() { A = 1, R = 0, G = 0, B = 0 }, newOffset));
            }
        }

        private void SetPointEvents(
            GradientStopPinpoint e
        )
        {
            var drag = false;
            Point shift = new Point();

            e.MouseRightButtonDown += (s, ev) =>
            {
                var pinpoint = (GradientStopPinpoint)s;
                Gradient.Stops.Remove(pinpoint.PaletteColor);

                CurrentStop = null;
            };

            e.MouseLeftButtonDown += (s, ev) =>
            {
                drag = true;
                shift = ev.GetPosition(s as FrameworkElement);
                SelectColor(s as GradientStopPinpoint);
                //selected = Gradient.Colors[(int)(s as FrameworkElement).Tag];
                e.CaptureMouse();

            };

            e.MouseLeftButtonUp += (s, ev) =>
            {
                drag = false;
                e.ReleaseMouseCapture();

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
                        m_samplesb[i].CurrentColor = interiorColors[i].Color;
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
                }
            };
        }

        #endregion
    }
}
