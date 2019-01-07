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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Media;
    using System.Diagnostics;
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.Collections;
    using System.Windows.Media.Imaging;

    public class ColorTreeNode: INotifyPropertyChanged, IComparer, IEqualityComparer<ColorTreeNode>
    {
        public event PropertyChangedEventHandler        PropertyChanged;
        private string                                  m_name = "";
        private TemplateColor                           m_tc;
        private object                                  m_tag;
        private bool?                                   m_checked = false;

        public ColorTreeNode(
        )
        {
            Children = new ObservableCollection<ColorTreeNode>();
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
                FirePropertyChanged("Name");
            }
        }

        public TemplateColor Color
        {
            get
            {
                return m_tc;
            }
            set
            {
                m_tc = value;
                FirePropertyChanged("Color");
            }
        }

        public ColorTreeNode Parent
        {
            get;
            set;
        }

        public ObservableCollection<ColorTreeNode> Children
        {
            get;
            set;
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

        public void FirePropertyChanged(
            string                                      name = ""
        )
        {
            if (PropertyChanged != null)
            { 
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public int Compare(
            object                                      x, 
            object                                      y
        )
        {
            return (x as ColorTreeNode).Name.CompareTo((y as ColorTreeNode).Name);
        }

        public bool Equals(
            ColorTreeNode                               x, 
            ColorTreeNode                               y
        )
        {
            return x.Name == y.Name;
        }

        public override int GetHashCode(
        )
        {
            return Name.GetHashCode();
        }

        public int GetHashCode(
            ColorTreeNode                               obj
        )
        {
            return this.GetHashCode();
        }

        public bool? IsChecked
        {
            get
            {
                return m_checked;
            }
            set
            {
                SetIsChecked(value, true, true);
                FirePropertyChanged("IsChecked");
            }
        }

        #region Privates

        private void SetIsChecked(
            bool?                                       value, 
            bool                                        updateChildren, 
            bool                                        updateParent
        )
        {
            if (value != m_checked)
            { 
                m_checked = value;

                if (updateChildren && m_checked.HasValue && Children != null)
                {
                    foreach (var c in Children)
                    {
                        c.SetIsChecked(m_checked, true, false);
                    }
                }

                if (updateParent && Parent != null)
                { 
                    Parent.VerifyCheckState();
                }

                FirePropertyChanged("IsChecked");
            }
        }

        private void VerifyCheckState(
        )
        {
            bool? state = null;
            for (int i = 0; i < this.Children.Count; ++i)
            {
                bool? current = this.Children[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            this.SetIsChecked(state, false, true);
        }

        #endregion 
    }

    public class ColorManager: DynamicObjectEx
    {
        public const int                                MAX_LEVEL = 200;

        public delegate object ColorFunction(object colorOrBrush, string func, string[] parameters);

        protected static Dictionary<string, TemplateColor> 
                                                        g_templates = new Dictionary<string, TemplateColor>();
        protected static ColorManager                   g_global = new ColorManager();

        protected static List<ColorFunction>            g_modFunctions = new List<ColorFunction>();

        protected static ObservableCollection<ColorTreeNode>
                                                        g_colorTree = new ObservableCollection<ColorTreeNode>();
        
        public void ClearCache()
        {
            base.Clear();
            this.FirePropertyChanged();
        }

        public static ColorManager Instance
        {
            get
            {
                return g_global;
            }
        }

        public ColorManager Global
        {
            get
            {
                return g_global;
            }
        }

        public static Dictionary<string, TemplateColor> Templates
        {
            get
            {
                return g_templates;
            }
        }

        public static void AddTemplate(
            TemplateColor                               a
        )
        {
            g_templates[a.Name] = a;
        }

        public static TemplateColor GetTemplate(
            string                                      name
        )
        {
            return g_templates[name];
        }

        public void AttachColorFunc(
            ColorFunction                               f
        )
        {
            if (!g_modFunctions.Contains(f))
            {
                g_modFunctions.Add(f);
            }
        }

        public void DetachColorFunc(
            ColorFunction                               f
        )
        {
            if (g_modFunctions.Contains(f))
            {
                g_modFunctions.Remove(f);
            }
        }

        public static object GetAt(
            string                                      name
        )
        {
            return g_global[name];
        }

        public static Brush GetSolidBrush(
            string                                      name
        )
        {
            return g_global[name + "-brush"] as Brush;
        }

        public override object this[
            string                                      fullname
        ]
        {
            get
            {
                object ret = g_global.At(fullname);

                if (ret is Color? || ret is Color)
                {
                    ret = new SolidColorBrush((Color) ret);
                }

                Debug.Assert(ret != null);
                return ret;
            }
        }

        public object At(
            string                                      fullname
        )
        {
            object                                      value = null;
            int                                         level = 0;

            if (base.m_bag.ContainsKey(fullname))
            {
                value = base[fullname];
            }
            else
            {
                string[]                                mods;

                mods = fullname.Split('-');
                for (int i = 0; i < mods.Length; ++i)
                {
                    value = ModifyLevel(value, mods[i], level);     
                }

                if (value != null)
                {
                    base[fullname] = value;
                }
                else
                { 
                    throw new InvalidOperationException("ColorManager failed. Could not create color for: " + fullname);
                }
            }

            Debug.Assert(value != null);
            return value;
        }

        private string GetColorFunction(
            TemplateColor                               tc
        )
        {
            string                                      val = tc.BasedOn;

            if (!String.IsNullOrEmpty(tc.Effect))
            {
                val += (!String.IsNullOrEmpty(val) ? "-" : "") + tc.Effect;
            }

            return val;
        }

        public static Brush Modify(
            object                                      brushOrColor,
            string                                      func,
            int                                         level = 0
        )
        {
            if (!String.IsNullOrEmpty(func))
            {
                string[] mods = func.Split('-');

                for (int i = 0; i < mods.Length; ++i)
                {
                    brushOrColor = Instance.ModifyLevel(brushOrColor, mods[i], level);     
                }
            }
            return (brushOrColor is Color) ? new SolidColorBrush((Color) brushOrColor) : (Brush) brushOrColor;
        }

        private object ModifyLevel(
            object                                      o,
            string                                      func,
            int                                         level
        )
        {
            TemplateColor                               tc = null;
            object                                      value = null;
            string                                      modifier;
            string[]                                    parameters = null;
            string[]                                    sp;
            string                                      mod;

            if (level > MAX_LEVEL)
            {
                throw new InvalidOperationException("Max color dependency level reached");
            }

            if (o == null)
            {
                if (!func.Contains("-"))
                {
                    while (value == null)
                    {
                        tc  = g_templates[func];
                        mod = GetColorFunction(tc);

                        if (tc.Color != null)
                        {
                            if (!String.IsNullOrEmpty(mod))
                            {
                                value = Modify(tc.Color, mod, level);
                            }
                            else
                            {
                                value = tc.Color;
                            }
                        }
                        else if (!String.IsNullOrEmpty(mod))
                        {
                            value = At(mod);
                        }
                    }
                }
                else
                {
                    value = At(func);
                }
            }
            else
            {
                sp = func.Split(new char[] { '(', ',', ')' });
                modifier = sp[0];

                var p = sp.ToList();
                p.RemoveAt(0);
                parameters = p.Where((s) => !String.IsNullOrEmpty(s)).ToArray();

                if (o as Color? != null)
                {
                    value = ModifyColor(o, modifier, parameters);
                }

                if (value == null)
                {
                    value = ModifyBrush(o, modifier, parameters, value as Color?);
                }
            }

            return value;
        }

        #region Color Transformation

        protected object ModifyColor(
            object                                      o, 
            string                                      modifier, 
            string[]                                    p = null
        )
        {
            object                                      value = null;
            Color                                       c;
            LinearGradientBrush                         gb;

            if (o as Color? != null)
            {
                c = (o as Color?).Value;

                switch (modifier.ToLower())
                {
                    case "i": case "invert":
                        value = c.Invert();
                        break;

                    case "il": case "invertluminosity":
                        value = c.InvertLiminosity();
                        break;

                    case "b": case "brush":
                        value = c.ToBrush();
                        break;

                    case "l": case "light": case "lighter":
                        value = c.Lighter(DblParam(p, 0, 0.25));
                        break;

                    case "d": case "dark": case "darker":
                        value = c.Darker(DblParam(p, 0, 0.25));
                        break;

                    case "t": case "transparent":
                        c.A = (byte) (c.A * DblParam(p, 0, 0.75));
                        value = c;
                        break;

                    case "h": case "horizontal":
                        gb = new LinearGradientBrush()
                        {
                            StartPoint = new Point(0.5, 0),
                            EndPoint = new Point(0.5, 1)
                        };
                        value = gb;
                        break;

                    case "v": case "vertical":
                        gb = new LinearGradientBrush()
                        {
                            StartPoint = new Point(0.5, 0),
                            EndPoint = new Point(0.5, 1)
                        };
                        value = gb;
                        break;

                    default:
                        foreach (ColorFunction f in g_modFunctions)
                        {
                            value = f(c, modifier, p);
                            if (value != null)
                            {
                                break;
                            }
                        }
                        break;
                }
            }

            return value;
        }

        protected double DblParam(
            string[]                                    parameters, 
            int                                         id,
            double                                      def = 0
        ) 
        {
            if (parameters == null || parameters.Length <= id)
            {
                return def; 
            }
            return Double.Parse(parameters[id]);
        }

        protected Brush ModifyBrush(
            object                                      o, 
            string                                      modifier, 
            string[]                                    p = null,
            Color?                                      baseColor = null
        )
        {
            GradientBrush                               gb = null;
            Brush                                       value = null;
            double                                      coff = 1;
            Color                                       c = Colors.Transparent; 
            
            if (o as Color? != null)
            {
                c = (Color) o;
                gb = new LinearGradientBrush()
                {
                    StartPoint = new Point(0.5, 0),
                    EndPoint = new Point(0.5, 1)
                };
            }
            else if (o as GradientBrush != null)
            {
                gb = (o as GradientBrush);
                if (baseColor != null)
                {
                    c = baseColor.Value;
                }
                else if ((o as GradientBrush).GradientStops.Count > 0)
                {
                    c = (o as GradientBrush).GradientStops[0].Color;
                }
            }
            else if (baseColor != null)
            {
                c = baseColor.Value;
            }

            switch (modifier.ToLower())
            {
                case "il": case "invertluminosity":
                    value = gb.Clone((cl) => cl.InvertLiminosity());
                    break;

                case "i": case "invert":
                    value = gb.Clone((cl) => cl.Invert());
                    break;

                case "l": case "light":
                    value = gb.Clone((cl) => cl.Lighter(DblParam(p, 0, 0.25)));
                    break;

                case "d": case "dark":
                    value = gb.Clone((cl) => cl.Darker(DblParam(p, 0, 0.25)));
                    break;

                case "r": case "radial":
                    var rgb = new RadialGradientBrush()
                    {
                        Center = new Point(0.1, 0.1),
                        GradientOrigin = new Point(0.04, 0.04),
                        RadiusX = 1.1,
                        RadiusY = 1.1
                    };
                    coff = DblParam(p, 0, 0.5);
                    rgb.GradientStops.Add(new GradientStop() { Color = c, Offset = 1 });
                    rgb.GradientStops.Add(new GradientStop() { Color = c.Lighter(coff), Offset = 0 });
                    value = rgb;
                    break;
#if !SILVERLIGHT
                case "tile":
                    value = CreateTile(p == null || p.Length != 1 ? "" : p[0]);
                    break;
#endif
                case "g": case "glass":
                    gb.GradientStops.Clear();
                    coff = DblParam(p, 0, 1);
                    gb.GradientStops.Add(new GradientStop() { Color = c.Lighter(coff * 0.4), Offset = 0 });
                    gb.GradientStops.Add(new GradientStop() { Color = c.Lighter(coff * 0.25), Offset = 0.48 });
                    gb.GradientStops.Add(new GradientStop() { Color = c.Lighter(coff * 0.3), Offset = 0.5 });
                    gb.GradientStops.Add(new GradientStop() { Color = c.Darker(coff * 0.25), Offset = 0.52 });
                    gb.GradientStops.Add(new GradientStop() { Color = c.Darker(coff * 0.19), Offset = 0.90 });
                    gb.GradientStops.Add(new GradientStop() { Color = c.Lighter(coff * 0.13), Offset = 1 });
                    value = gb;
                    break;

                case "t": case "tube":
                    gb.GradientStops.Clear();
                    coff = DblParam(p, 0, 0.35);
                    gb.GradientStops.Add(new GradientStop() { Color = c.Lighter(coff), Offset = 0 });
                    gb.GradientStops.Add(new GradientStop() { Color = c.Darker(coff), Offset = 0.5 });
                    gb.GradientStops.Add(new GradientStop() { Color = c.Lighter(coff), Offset = 1 });
                    value = gb;
                    break;

                case "dl": case "darklight":
                    gb.GradientStops.Clear();
                    coff = DblParam(p, 0, 0.25);
                    gb.GradientStops.Add(new GradientStop() { Color = c.Darker(coff), Offset = 0 });
                    gb.GradientStops.Add(new GradientStop() { Color = c.Lighter(coff), Offset = 1 });
                    value = gb;
                    break;

                case "3d": case "ld": case "lightdark":
                    gb.GradientStops.Clear();
                    coff = DblParam(p, 0, 0.25);
                    gb.GradientStops.Add(new GradientStop() { Color = c.Lighter(coff), Offset = 0 });
                    gb.GradientStops.Add(new GradientStop() { Color = c.Darker(coff), Offset = 1 });
                    value = gb;
                    break;

                default:
                    foreach (ColorFunction f in g_modFunctions)
                    {
                        value = f(o, modifier, p) as Brush;
                        if (value != null)
                        {
                            break;
                        }
                    }
                    if (value == null)
                    {
                        value = o as Brush;
                    }
                    break;
            }

            if (value == null || value as Brush == null)
            {
                value = o as Brush;
            }
            return value;
        }


#if !SILVERLIGHT
        public Brush CreateTile(
            string                                      name
        )
        {
            string                                      fmt = "pack://application:,,,/ColorWheel.Core;component/Backgrounds/{0}";
            Rect                                        viewport = new Rect();

            switch (name.ToLower())
            {
                case "darkabctile":
                    name += ".png";
                    viewport = new Rect(0, 0, 150, 150);
                    break;

                case "lightabctile":
                    name += ".png";
                    viewport = new Rect(0, 0, 150, 150);
                    break;

                case "lightgraytile":
                    name += ".gif";
                    viewport = new Rect(0, 0, 264, 264);
                    break;

                case "lightlinentile":
                    name += ".png";
                    viewport = new Rect(0, 0, 256, 256);
                    break;

                default:
                    name = "DarkLinenTile.png";
                    viewport = new Rect(0, 0, 256, 256);
                    break;
            }

            var ib = new ImageBrush(new BitmapImage(new Uri(String.Format(fmt, name), UriKind.RelativeOrAbsolute)));

            ib.Viewport      = viewport;
            ib.TileMode      = TileMode.Tile;
            ib.Stretch       = Stretch.UniformToFill;
            ib.ViewportUnits = BrushMappingMode.Absolute;
            ib.AlignmentX    = AlignmentX.Left;
            ib.AlignmentY    = AlignmentY.Top;

            return ib;
        }
#endif

        #endregion

        #region Support for Color Management UI

        public ObservableCollection<ColorTreeNode> GetColorTree(
            bool                                        refresh = false
        )
        {
            TemplateColor                               tc;
            string[]                                    categories;
            ColorTreeNode                               node;
            ObservableCollection<ColorTreeNode>         level;
            ColorTreeNode                               prev = null;

            if (g_colorTree.Count > 0 && refresh)
            {
                g_colorTree.Clear();
            }

            if (g_colorTree.Count == 0)
            { 
                foreach (var kv in ColorManager.Templates)
                {
                    tc         = kv.Value;
                    categories = tc.Category.Split('\\').Where(c => !String.IsNullOrEmpty(c)).ToArray();
                    level      = g_colorTree;

                    foreach (string category in categories)
                    {
                        node = g_colorTree.FirstOrDefault(c => c.Name == category);
                        if (node == null)
                        {
                            node = new ColorTreeNode()
                            {
                                Name = category
                            };
                            level.Add(node);
                        }
                        level = node.Children;
                        prev = node;
                    }

                    level.Add(new ColorTreeNode() 
                    {
                        Name     = tc.Title,
                        Color    = tc,
                        Parent   = prev,
                        Children = null
                    });
                }
            }

            return g_colorTree;
        }

        #endregion
    }
}
