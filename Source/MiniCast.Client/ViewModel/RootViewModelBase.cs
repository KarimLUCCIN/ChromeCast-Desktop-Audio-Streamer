﻿using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCast.Client.ViewModel
{
    public class RootViewModelBase : ViewModelBase
    {
        public virtual bool HasGlobalSpectrum { get; } = true;
    }
}
