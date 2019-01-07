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
    using System.Text.RegularExpressions;
    using System.ComponentModel;

    public class TemplateColor: DynamicObjectEx
    {
        private Color?                                  m_color;
        private string                                  m_name = "";
        private string                                  m_title = "";
        private string                                  m_basedOn = String.Empty;
        private string                                  m_effect = String.Empty;
        private TemplateColor                           m_basedOnTemplate;

        public TemplateColor(
        )
        {
            ColorManager.Instance.PropertyChanged += OnInstancePropertyChanged;
        }

        void OnInstancePropertyChanged(
            object                                      sender, 
            PropertyChangedEventArgs                    e
        )
        {
            FirePropertyChanged();
        }

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
                ColorManager.AddTemplate(this);
            }
        }

        public string Title
        {
            get
            {
                if (String.IsNullOrEmpty(m_title))
                { 
                    m_title = ToFriendlyCase(m_name);
                }
                return m_title;
            }
            set
            {
                m_title = value;
            }
        }

        public TemplateColor BasedOnTemplate
        {
            get
            {
                return m_basedOnTemplate;
            }
            set
            {
                m_basedOnTemplate = value;
                if (m_basedOnTemplate != null)
                {
                    BasedOn = m_basedOnTemplate.Name;
                }
                else
                {
                    BasedOn = String.Empty;
                }
            }
        }

        public string BasedOn
        {
            get
            {
                return m_basedOn;
            }
            set
            {
                m_basedOn = value;
                FirePropertyChanged();
            }
        }

        public string Effect
        {
            get
            {
                return m_effect;
            }
            set
            {
                m_effect = value;
                FirePropertyChanged();
            }
        }

        public String Category
        {
            get;
            set;
        }

        public long Value
        {
            get
            {
                if (m_color == null)
                {
                    return long.MaxValue;
                }
                return m_color.Value.ToLong();
            }
            set
            {
                if (value == long.MaxValue)
                {
                    m_color = null;
                }
                m_color = value.ToColor();
                FirePropertyChanged();
            }
        }

        public Type Type
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public Color? Color
        {
            get
            {
                return m_color;
            }
            set
            {
                m_color = value;
                FirePropertyChanged();
            }
        }

        protected string ToFriendlyCase(
            string                                      str
        )
        {
            return Regex.Replace(str, "(?!^)([A-Z])", " $1");
        }

        public ColorManager Global
        {
            get
            {
                return ColorManager.Instance;
            }
        }

        public Brush Brush
        {
            get
            {
                return Global[this.Name] as Brush;
            }
        }
    }
}
