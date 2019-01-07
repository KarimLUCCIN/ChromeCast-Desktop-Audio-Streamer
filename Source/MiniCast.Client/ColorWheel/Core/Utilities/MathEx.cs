/* 
 * Copyright (c) 2011, Andrew Syrov
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
    using System.Net;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Ink;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Shapes;

    public static class MathEx
    {
        public static double Lerp(
            double                                      start, 
            double                                      end, 
            double                                      amount
        )
        {
            double diff = end - start;
            double adjusted = diff * amount;

            return start + adjusted;
        }

        public static double Lerp(
            double                                      x, 
            double                                      q00, 
            double                                      q01,
            double                                      x1 = 0.0, 
            double                                      x2 = 1.0
        ) 
        {  
            return ((x - x1) / (x2 - x1)) * (q01 - q00) - q00;
        }
        
        /// 
        /// <summary>
        /// 11 => xy</summary>
        /// 
        public static double BiLerp(
            double                                      x, 
            double                                      y, 
            double                                      q11, 
            double                                      q12, 
            double                                      q21, 
            double                                      q22, 
            double                                      x1 = 0.0, 
            double                                      x2 = 1.0, 
            double                                      y1 = 0.0, 
            double                                      y2 = 1.0
        ) 
        {  
            double r1 = Lerp(x, x1, x2, q11, q21);  
            double r2 = Lerp(x, x1, x2, q12, q22);  
            
            return Lerp(y, y1, y2, r1, r2);
        }

        //
        /// <summary>
        /// 000 => xyz</summary>
        ///
        public static double TriLerp(
            double                                      x, 
            double                                      y, 
            double                                      z, 
            double                                      q000, 
            double                                      q001, 
            double                                      q010, 
            double                                      q011, 
            double                                      q100, 
            double                                      q101, 
            double                                      q110, 
            double                                      q111, 
            double                                      x1 = 0.0, 
            double                                      x2 = 1.0, 
            double                                      y1 = 0.0, 
            double                                      y2 = 1.0, 
            double                                      z1 = 0.0, 
            double                                      z2 = 1.0
        ) 
        {  
            double x00 = Lerp(x, x1, x2, q000, q100);  
            double x10 = Lerp(x, x1, x2, q010, q110);  
            double x01 = Lerp(x, x1, x2, q001, q101);  
            double x11 = Lerp(x, x1, x2, q011, q111);  

            double r0 = Lerp(y, y1, y2, x00, x01);  
            double r1 = Lerp(y, y1, y2, x10, x11);  

            return Lerp(z, z1, z2, r0, r1);
        }

        public static double ToRadians(
            double                                      degree
        )
        {
            return degree * (Math.PI / 180);
        }
    }
}
