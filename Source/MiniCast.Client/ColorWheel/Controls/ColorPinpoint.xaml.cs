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
    using System.Windows.Controls;
    using System.ComponentModel;
    using System.Windows.Media;
    using ColorWheel.Core;
    using System.Windows;
    using System.Windows.Shapes;
    using System.Windows.Data;

    public partial class ColorPinpoint: UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler        PropertyChanged;
        private Color                                   m_color = Colors.Transparent;
        private bool                                    m_isMain = false;
        private PaletteColor                            m_pc = new PaletteColor();

        public ColorPinpoint(
        )
        {
            InitializeComponent();
            DataContext = this;
        }

        public PaletteColor PaletteColor
        {
            get
            {
                return m_pc;
            }
            set
            {
                if (m_pc != value)
                {
                    m_pc = value;
                    FirePropertyChanged("PaletteColor");
                }
            }
        }

        public Color CurrentColor
        {
            get
            {
                return m_color;
            }
            set
            {
                if (value != m_color)
                {
                    m_color = value;

                    FirePropertyChanged("CurrentColor");
                    FirePropertyChanged("CurrentBorderColor");
                }
            }
        }

        public bool IsMain
        {
            get
            {
                return m_isMain;
            }
            set
            {
                if (m_isMain != value)
                {
                    m_isMain = value;

                    FirePropertyChanged("IsMain");
                    FirePropertyChanged("IsMainVisibility");
                }
            }
        }

        public Visibility IsMainVisibility
        {
            get
            {
                return m_isMain ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Color CurrentBorderColor
        {
            get
            {
                return m_color.GetForeground();
            }
        }

        private void FirePropertyChanged(
            string                                      name = ""
        )
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class IsSelectedThickness: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || !(value is bool))
            {
                return 1;
            }
            return (double) (((bool) value) ? 2.0 : 1.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
