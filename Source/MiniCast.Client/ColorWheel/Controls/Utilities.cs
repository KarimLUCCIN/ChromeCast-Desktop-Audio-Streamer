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
    using System;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public delegate void Handler<T>(object sender, EventArg<T> e);


    public static class Compat
    {
#if !SILVERLIGHT
        public static int GetBitmapStride(
            double                                      pixelWidth
        )
        {
            return (int) pixelWidth * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8);
        }
#endif

        public static WriteableBitmap CreateBitmap(
            double                                      width,
            double                                      height
        )
        {
            WriteableBitmap b;

            width  = Math.Max(1, width);
            height = Math.Max(1, height);

#if SILVERLIGHT
            b = new WriteableBitmap((int) width, (int) height);    
#else
            b = new WriteableBitmap((int) width, (int) height, 300, 300, PixelFormats.Bgra32, null);
#endif
            return b;
        }
    }


    public class EventArg<T> : EventArgs
    {
        public T Value
        {
            get;
            set;
        }
    }

    /// 
    /// <summary>
    /// Helper class for id-image-description type </summary>
    /// 
    public class ImageTextItem
    {
        public int Id
        {
            get;
            set;
        }

        public ImageSource Image
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public string Shortcut
        {
            get;
            set;
        }
    }
}
