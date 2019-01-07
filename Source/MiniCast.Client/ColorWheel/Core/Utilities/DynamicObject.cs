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
    using System.ComponentModel;
    using System.Dynamic;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class DynamicObjectEx: DynamicObject, INotifyPropertyChanged
    {
        protected IDictionary<string, object>          m_bag = new Dictionary<string, object>();
        public event PropertyChangedEventHandler       PropertyChanged;

        [IndexerName("Item")]
        public virtual object this[string index]
        {
            get
            {
                object                                 result = null;

                m_bag.TryGetValue(index, out result);
                return result;
            }
            set 
            { 
                m_bag[index] = value; 
                FirePropertyChanged(index); 
            }
        }

        public void FirePropertyChanged(
        string                                          name = ""
        )
        {
            if (PropertyChanged != null)
            { 
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public override bool TryGetMember(
            GetMemberBinder                             binder, 
            out object                                  result
        )
        {
            return m_bag.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(
            SetMemberBinder                             binder, 
            object                                      value
        )
        {
            m_bag[binder.Name] = value;

            FirePropertyChanged(binder.Name);
            return true;
        }

        public virtual void Clear(
        )
        {
            m_bag.Clear();
            FirePropertyChanged();
        }
    }
}
