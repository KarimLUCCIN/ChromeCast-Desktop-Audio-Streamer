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

    public abstract class ColorWheelBase
    {
        public abstract DoubleColor GetColor(
            double                                      angle
        );

        public abstract double GetAngle(
            DoubleColor                                 c
        );

        public abstract double GetAngle(
            AHSB                                        c
        );

        public virtual DoubleColor ColorBySaturation(
            double                                      angle,
            double                                      saturation
        )
        {
            AHSB                                        hsb;

            hsb = GetColor(angle).ToAHSB();
            hsb.Saturation = saturation;

            return hsb.Double();
        }

        public virtual DoubleColor ColorByBrightness(
            double                                      angle,
            double                                      brightness
        )
        {
            AHSB                                        hsb;

            hsb = GetColor(angle).ToAHSB();
            hsb.Brightness = brightness;

            return hsb.Double();
        }

        public double FixAngle(
            double                                      angle
        )
        {
            angle = angle % 360;
            if (angle < 0)
            {
                angle += 360;
            }
            return angle;
        }

        #region Protected

        protected virtual double ToRgbAngle(
            double                                      angle
        )
        {
            return angle;
        }

        protected virtual double ToWheelAngle(
            double                                      angle
        )
        {
            return angle;
        }

        #endregion
    }

    /// 
    /// <summary>
    /// RBG color wheel</summary>
    /// 
    public class RGBColorWheel: ColorWheelBase
    {
        public override DoubleColor GetColor(
            double                                      angle
        )
        {
            return (DoubleColor) (new AHSB(1, FixAngle(angle), 1, 1));
        }

        public override double GetAngle(
            DoubleColor                                 c
        )
        {
            return c.HueDegree;
        }

        public override double GetAngle(
            AHSB                                        c
        )
        {
            return c.HueDegree;
        }
    }

    /// 
    /// <summary>
    /// RBY color wheel. Primary Colors: Red, yellow and blue</summary>
    /// 
    public class RYBColorWheel: RGBColorWheel
    {
        public override double GetAngle(
            DoubleColor                                 c
        )
        {
            return ToWheelAngle(c.ToAHSB().HueDegree);
        }

        public override double GetAngle(
            AHSB                                        c
        )
        {
            return ToWheelAngle(c.HueDegree);
        }

        public override DoubleColor GetColor(
            double                                      angle
        )
        {
            return base.GetColor(ToRgbAngle(FixAngle(angle)));
        }

        protected override double ToWheelAngle(
            double                                      angle
        )
        {
            Debug.Assert(angle >= 0 && angle <= 360);

            if (angle <= 60.0)
            {
                angle *= 2;
            }
            else if (angle <= 240)
            {
                angle = (angle - 60.0) * ( 2.0 / 3.0) + 120.0;
            }
            return angle;
        }

        protected override double ToRgbAngle(
            double                                      angle
        )
        {
            Debug.Assert(angle >= 0 && angle <= 360);

            if (angle <= 120.0)
            {
                angle /= 2;
            }
            else if (angle >= 120.0 && angle <= 240)
            {
                angle = (angle - 120.0) * ( 3.0 / 2.0) + 60.0;
            }
            return angle;
        }
    }

    /// 
    /// <summary>
    /// RYGB color wheel. Primary Colors: Red, Yellow, Green and Blue</summary>
    /// 
    public class RYGBColorWheel: RGBColorWheel
    {
        public override double GetAngle(
            DoubleColor                                 c
        )
        {
            return ToWheelAngle(c.ToAHSB().HueDegree);
        }

        public override DoubleColor GetColor(
            double                                      angle
        )
        {
            return (DoubleColor) (new AHSB(1, ToRgbAngle(FixAngle(angle)), 1, 1));
        }

        public override double GetAngle(
            AHSB                                        c
        )
        {
            return ToWheelAngle(c.HueDegree);
        }

        protected override double ToWheelAngle(
            double                                      angle
        )
        {
            if (angle <= 60.0)
            {
                angle *= 3.0 / 2.0;
            }
            else if (angle > 60.0 && angle <= 120.0)
            {
                angle = (angle - 60.0) * (3.0 / 2.0) + 90.0;
            }
            else if (angle > 120.0 && angle <= 240.0)
            {
                angle = (angle - 120.0) / (4.0 / 3.0) + 180.0;
            }
            else
            {
                angle = (angle - 240.0) / (4.0 / 3.0) + 270.0;
            }
            return angle;
        }

        protected override double ToRgbAngle(
            double                                      angle
        )
        {
            if (angle <= 90.0)
            {
                angle /= (3.0 / 2.0);
            }
            else if (angle > 90.0 && angle <= 180.0)
            {
                angle = (angle - 90.0) / (3.0 / 2.0) + 60.0;
            }
            else if (angle > 180.0 && angle <= 270.0)
            {
                angle = (angle - 180.0) * (4.0 / 3.0) + 120.0;
            }
            else
            {
                angle = (angle - 270.0) * (4.0 / 3.0) + 240.0;
            }
            return angle;
        }
    }
}
