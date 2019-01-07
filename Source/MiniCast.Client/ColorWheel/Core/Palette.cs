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
    using System.Windows.Media;
    using System.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Collections.ObjectModel;

    public enum PaletteSchemaType
    {
        Monochromatic       = 0, 
        Analogous           = 1, 
        Complementary       = 2, 
        SplitAnalogous      = 3, 
        SplitComplementary  = 4, 
        Triad               = 5, 
        Tetrads             = 6, 
        Quadrants           = 7, 
        Custom              = 8  // number of vectors defined by color number in palette
    }

    public class PaletteColor: INotifyPropertyChanged
    {
        private DoubleColor                                 m_color = Colors.Black.Double();
        private DoubleColor                                 m_previous = Colors.Black.Double();

        private string                                      m_name;
        public event PropertyChangedEventHandler            PropertyChanged;
        private int                                         m_vindex = 0;
        private object                                      m_tag = null;
        private bool                                        m_selected = false;

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
                FirePropertyChanged("Name");
            }
        }

        /// 
        /// <summary>
        /// 0 is main vector, for PaletteSchemaType.Custom this value is not used</summary>
        /// 
        public int VectorIndex
        {
            get
            {
                return m_vindex;
            }
            set
            {
                m_vindex = value;
                FirePropertyChanged("VectorIndex");
            }
        }

        public DoubleColor DoubleColor
        {
            get
            {
                return m_color;
            }
            set
            {
                if (m_color != value)
                {
                    m_previous = m_color;
                    m_color = value;

                    ColorUpdated();
                    UpdateColorComponents();
                }
            }
        }

        public Color RgbColor
        {
            get
            {
                return DoubleColor.ToColor();
            }
            set
            {
                DoubleColor = value.Double();
            }
        }

        public AHSB Color
        {
            get
            {
                return DoubleColor.ToAHSB();
            }
            set
            {
                this.DoubleColor = value.Double();

                m_color.HueDegree  = value.HueDegree;            
                m_color.Saturation = value.Saturation;
            }
        }

        public bool IsSelected
        {
            get
            {
                return m_selected;
            }
            set
            {
                if (m_selected != value)
                {
                    m_selected = value;
                    FirePropertyChanged("IsSelected");
                }
            }
        }

        #region Color Components

        public double R
        {
            get
            {
                return DoubleColor.R;
            }
            set
            {
                if (value != DoubleColor.R)
                {
                    DoubleColor = m_color.Alter(value);
                    FirePropertyChanged("R");
                    ColorUpdated();
                }
            }
        }

        public double G
        {
            get
            {
                return DoubleColor.G;
            }
            set
            {
                if (value != DoubleColor.G)
                {
                    DoubleColor = m_color.Alter(null, value);
                    FirePropertyChanged("G");
                    ColorUpdated();
                }
            }
        }

        public double B
        {
            get
            {
                return DoubleColor.B;
            }
            set
            {
                if (value != DoubleColor.B)
                {
                    DoubleColor = m_color.Alter(null, null, value);
                    FirePropertyChanged("B");
                    ColorUpdated();
                }
            }
        }

        public double A
        {
            get
            {
                return DoubleColor.A;
            }
            set
            {
                if (value != DoubleColor.A)
                {
                    DoubleColor = m_color.Alter(null, null, null, value);
                    FirePropertyChanged("A");
                    ColorUpdated();
                }
            }
        }

        public double Hue360
        {
            get
            {
                return m_color.HueDegree;
            }
            set
            {
                if (value != m_color.HueDegree)
                {
                    AHSB hsb          = DoubleColor.ToAHSB();
                    hsb.HueDegree     = value;
                    Color             = hsb;

                    //
                    // this is to preserve hue for gray color
                    //
                    m_color.HueDegree = value;

                    ColorUpdated();
                    UpdateColorComponents();
                }
            }
        }

        public byte Alpha255
        {
            get
            {
                return m_color.ToAHSB().Alpha255;
            }
            set
            {
                if (value != m_color.ToAHSB().Alpha255)
                {
                    AHSB hsb     = Color;
                    hsb.Alpha255 = value;
                    Color        = hsb;

                    ColorUpdated();
                    UpdateColorComponents();
                }
            }
        }

        public byte Saturation255
        {
            get
            {
                return m_color.ToAHSB().Saturation255;
            }
            set
            {
                if (value != m_color.ToAHSB().Saturation255)
                {
                    AHSB hsb          = Color;
                    hsb.Saturation255 = value;
                    Color             = hsb;

                    ColorUpdated();
                    UpdateColorComponents();
                }
            }
        }

        public byte Brightness255
        {
            get
            {
                return m_color.ToAHSB().Brightness255;
            }
            set
            {
                if (value != m_color.ToAHSB().Brightness255)
                {
                    AHSB hsb          = Color;
                    hsb.Brightness255 = value;
                    Color             = hsb;

                    ColorUpdated();
                    UpdateColorComponents();
                }
            }
        }

        public object Tag
        {
            get
            {
                return m_tag;
            }
            set
            {
                m_tag = value;
                FirePropertyChanged("Tag");
            }
        }

        #endregion

        #region Property Changed

        private void FirePropertyChanged(
            string name
        )
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }


        protected virtual void UpdateColorComponents(
        )
        {
            UpdateRgbComponents();
            UpdateHSBComponents();
            UpdateAlphaComponent();
        }

        protected virtual void UpdateRgbComponents(
        )
        {
            DoubleColor p = m_previous;
            DoubleColor c = m_color;

            if (p.R != c.R)
            {
                FirePropertyChanged("R");
            }

            if (p.G != c.G)
            {
                FirePropertyChanged("G");
            }

            if (p.B != c.B)
            {
                FirePropertyChanged("B");
            }
        }

        protected virtual void UpdateAlphaComponent(
        )
        {
            if (m_previous.A != m_color.A)
            {
                FirePropertyChanged("A");
            }
        }

        protected virtual void ColorUpdated(
        )
        {
            FirePropertyChanged("DoubleColor");
            FirePropertyChanged("RgbColor");
            FirePropertyChanged("Color");
        }

        protected virtual void UpdateHSBComponents(
        )
        {
            AHSB p = m_previous.ToAHSB();
            AHSB c = m_color.ToAHSB();

            if (p.HueDegree != c.HueDegree)
            {
                FirePropertyChanged("Hue360");
            }

            if (p.Saturation255 != c.Saturation255)
            {
                FirePropertyChanged("Saturation255");
            }

            if (p.Brightness255 != c.Brightness255)
            {
                FirePropertyChanged("Brightness255");
            }
        }

        #endregion
    }

    public class Palette: INotifyPropertyChanged
    {
        protected ObservableCollection<PaletteColor>       m_colors = new ObservableCollection<PaletteColor>();
        protected PaletteSchemaType                        m_schema = PaletteSchemaType.Monochromatic; 

        protected double                                   m_angle = 30.0;
        protected double                                   m_baseAngle = 0.0;
        private ColorWheelBase                             m_wheel;
        private object                                     m_tag = null;

        public event PropertyChangedEventHandler           PropertyChanged;

        public ColorWheelBase Wheel
        {
            get
            {
                return m_wheel;
            }
            set
            {
                Debug.Assert(value != null);

                m_wheel = value;
                if (m_colors.Count > 0)
                {
                    BaseAngle = m_wheel.GetAngle(m_colors[0].Color);
                }

                FirePropertyChanged("Wheel");
                FirePropertyChanged(String.Empty);
            }
        }

        protected Palette(
            ColorWheelBase                                  wheel
        )
        {
            Debug.Assert(wheel != null);

            m_colors = new ObservableCollection<PaletteColor>();
            m_wheel  = wheel;
        }

        public double Angle
        {
            get
            {
                return m_angle;
            }
            set
            {
                m_angle = value;
                BaseAngle = BaseAngle;
                FirePropertyChanged("Angle");
            }
        }

        public double BaseAngle
        {
            get
            {
                return m_baseAngle;
            }
            set
            {
                UpdateBaseAngle(value);
                m_baseAngle = value;
                FirePropertyChanged("BaseAngle");
            }
        }

        public void Refresh(
        )
        {
            UpdateBaseAngle(BaseAngle);
        }

        public ObservableCollection<PaletteColor> Colors
        {
            get
            {
                return m_colors;
            }
        }

        public int MaxVectorIndex
        {
            get;
            protected set;
        }

        public int SchemaIndex
        {
            get
            {
                return (int) Schema;
            }
            set
            {
                Schema = (PaletteSchemaType) value;
            }
        }

        public PaletteSchemaType Schema
        {
            get
            {
                return m_schema;
            }
            set
            {
                m_schema = value;
                MaxVectorIndex = 0;

                switch (value)
                {
                    case PaletteSchemaType.Custom:
                    case PaletteSchemaType.Monochromatic:
                        for (int i = 0; i < m_colors.Count; ++i)
                        {
                            m_colors[i].VectorIndex = 0;
                        }
                        MaxVectorIndex = 0;
                        break;

                    case PaletteSchemaType.Quadrants:
                    case PaletteSchemaType.Tetrads:
                        for (int i = 0; i < m_colors.Count; ++i)
                        {
                            m_colors[i].VectorIndex = i % 4;
                        }
                        MaxVectorIndex = 4;
                        break;

                    case PaletteSchemaType.SplitComplementary:
                    case PaletteSchemaType.SplitAnalogous:
                    case PaletteSchemaType.Triad:
                        for (int i = 0; i < m_colors.Count; ++i)
                        {
                            m_colors[i].VectorIndex = i % 3;
                        }
                        MaxVectorIndex = 3;
                        break;
                
                    case PaletteSchemaType.Complementary:
                    case PaletteSchemaType.Analogous:
                        for (int i = 0; i < m_colors.Count; ++i)
                        {
                            m_colors[i].VectorIndex = i % 2;
                        }
                        MaxVectorIndex = 2;
                        break;
                }

                if (value == PaletteSchemaType.Complementary)
                {
                    Angle = 180.0;
                }
                else if (value == PaletteSchemaType.Quadrants)
                {
                    Angle = 90.0;
                }
                else if (value == PaletteSchemaType.SplitAnalogous)
                {
                    Angle = 0.0;
                }
                else if (value == PaletteSchemaType.Triad)
                {
                    Angle = 120.0;
                }
                else
                {
                    Angle = 30.0;
                }

                FirePropertyChanged("Schema");
                FirePropertyChanged("SchemaIndex");
                FirePropertyChanged("MaxVectorIndex");
            }
        }

        public object Tag
        {
            get
            {
                return m_tag;
            }
            set
            {
                m_tag = value;
                FirePropertyChanged("Tag");
            }
        }

        /// 
        /// <summary>
        /// Generate palette by base color, schema and number of colors.
        /// Number of colors should be more than 1</summary>
        /// 
        public static Palette Create(
            ColorWheelBase                                  wheel,
            Color                                           baseColor,
            PaletteSchemaType                               schema      = PaletteSchemaType.Complementary,
            int                                             colorCount  = 5,
            double                                          fromSat     = 1.0,
            double                                          toSat       = 0.2,
            double                                          fromBri     = 0.2,
            double                                          toBri       = 0.9
        )
        {
            double                                          angle = 30;
            double                                          baseAngle = 0;
            double                                          alpha = baseColor.A / 255.0;
            double                                          incs = 0;
            double                                          incb = 0;
            AHSB                                            c;
            int                                             index = 0;

            wheel = wheel ?? new RGBColorWheel();
            baseAngle = wheel.GetAngle(baseColor.Double());

            if (schema == PaletteSchemaType.Complementary)
            {
                angle = 180.0;
            }
            else if (schema == PaletteSchemaType.Quadrants)
            {
                angle = 90.0;
            }
            else if (schema == PaletteSchemaType.Triad)
            {
                angle = 120.0;
            }

            Palette p = new Palette(wheel)
            {
                 Schema = schema,
                 Angle = angle
            };

            if (colorCount > 1)
            {
                incs = (toSat - fromSat) / (colorCount - 1);
                incb = (toBri - fromBri) / (colorCount - 1); 
            }

            for (int i = 0; i < colorCount; ++i)
            {
                switch (p.Schema)
                {
                    case PaletteSchemaType.Monochromatic:
                        break;

                    case PaletteSchemaType.Quadrants:
                    case PaletteSchemaType.Tetrads:
                        index = i % 4;
                        break;

                    case PaletteSchemaType.Custom:
                    case PaletteSchemaType.SplitComplementary:
                    case PaletteSchemaType.SplitAnalogous:
                    case PaletteSchemaType.Triad:
                        index = i % 3;
                        break;
                
                    case PaletteSchemaType.Complementary:
                    case PaletteSchemaType.Analogous:
                        index = i % 2;
                        break;

                    default:
                        throw new ArgumentException();
                }
                c = new AHSB(alpha, baseAngle, fromSat, fromBri);

                fromSat = AHSB.Fix(fromSat + incs);
                fromBri = AHSB.Fix(fromBri + incb);

                p.Colors.Add(new PaletteColor()
                {
                    Name        = i.ToString(),
                    VectorIndex = index,
                    Color       = c
                });
            }

            p.BaseAngle = baseAngle;
            return p;
        }

        #region Private methods

        private void FirePropertyChanged(
            string name
        )
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public void UpdateBaseAngle(
            double                                          value
        )
        {
            double hue  = value;
            double comp = 0.0;
            double hue1 = 0.0;
            double hue2 = 0.0;
            double hue3 = 0.0;

            if (m_wheel != null)
            {
                value = m_wheel.FixAngle(value);
                hue = m_wheel.GetColor(value).ToAHSB().HueDegree;
            }

            switch (m_schema)
            {
                case PaletteSchemaType.Monochromatic:
                    for (int i = 0; i < m_colors.Count; ++i)
                    {
                        AHSB hsb = m_colors[i].Color;
                        hsb.HueDegree = hue;
                        m_colors[i].Color = hsb;
                    }
                    break;

                case PaletteSchemaType.Complementary:
                case PaletteSchemaType.Analogous:
                    for (int i = 0; i < m_colors.Count; ++i)
                    {
                        AHSB hsb = m_colors[i].Color;
                        hsb.HueDegree = m_colors[i].VectorIndex == 0 ? 
                            hue :  
                            m_wheel.GetColor(value + m_angle).ToAHSB().HueDegree;

                        m_colors[i].Color = hsb;
                    }
                    break;

                case PaletteSchemaType.SplitComplementary:
                    comp = 180;
                    goto case PaletteSchemaType.SplitAnalogous;

                case PaletteSchemaType.Triad:
                case PaletteSchemaType.SplitAnalogous:
                    if (m_wheel != null)
                    {
                        hue1 = m_wheel.GetColor(value + comp + m_angle).ToAHSB().HueDegree;
                        hue2 = m_wheel.GetColor(value + comp - m_angle).ToAHSB().HueDegree;
                    }
                    for (int i = 0; i < m_colors.Count; ++i)
                    {
                        AHSB hsb = m_colors[i].Color;
                        int idx = m_colors[i].VectorIndex;

                        hsb.HueDegree = idx % 3 == 0 ? hue : (idx % 3 == 1 ? hue1 : hue2);
                        m_colors[i].Color = hsb;
                    }
                    break;

                case PaletteSchemaType.Quadrants:
                    hue1 = m_wheel.GetColor(value + 90).ToAHSB().HueDegree; 
                    hue2 = m_wheel.GetColor(value + 180).ToAHSB().HueDegree;
                    hue3 = m_wheel.GetColor(value + 270).ToAHSB().HueDegree;

                    for (int i = 0; i < m_colors.Count; ++i)
                    {
                        AHSB hsb = m_colors[i].Color;
                        int idx = m_colors[i].VectorIndex;

                        hsb.HueDegree = idx % 4 == 0 ? hue : (idx % 4 == 1 ? hue1 : (idx % 4 == 2 ? hue2 : hue3));
                        m_colors[i].Color = hsb;
                    }
                    break;

                case PaletteSchemaType.Tetrads:
                    hue1 = m_wheel.GetColor(value + m_angle).ToAHSB().HueDegree;
                    hue2 = m_wheel.GetColor(value + 180).ToAHSB().HueDegree; 
                    hue3 = m_wheel.GetColor(value + 180.0 + m_angle).ToAHSB().HueDegree;

                    for (int i = 0; i < m_colors.Count; ++i)
                    {
                        AHSB hsb = m_colors[i].Color;
                        int idx = m_colors[i].VectorIndex;

                        hsb.HueDegree = idx % 4 == 0 ? hue : (idx % 4 == 1 ? hue1 : (idx % 4 == 2 ? hue2 : hue3));
                        m_colors[i].Color = hsb;
                    }
                    break;
            }
        }

        #endregion
    }
}
