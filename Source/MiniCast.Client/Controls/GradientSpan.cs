using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MiniCast.Client.Controls
{
    public class GradientSpan : ViewModelBase, INotifyPropertyChanged
    {
        public GradientStop Start { get; set; } = new GradientStop(new Color() { ScA = 1, ScR = 0, ScG = 0, ScB = 0 }, 0);
        public GradientStop End { get; set; } = new GradientStop(new Color() { ScA = 1, ScR = 1, ScG = 1, ScB = 1 }, 1);

        public GradientStopCollection Stops { get; } = new GradientStopCollection();

        public event Action<GradientSpan> GradientChanged;

        private GradientStop[] previousStops = new GradientStop[0];

        public GradientSpan()
        {
            Stops.Changed += Stops_Changed;
        }

        public IEnumerable<GradientStop> OrderedStops
        {
            get
            {
                yield return Start;

                foreach (var stop in Stops.OrderBy(s => s.Offset))
                {
                    yield return stop;
                }

                yield return End;
            }
        }

        public IEnumerable<GradientStop> InteriorStops
        {
            get
            {
                return Stops.OrderBy(s => s.Offset);
            }
        }

        private void Stops_Changed(object sender, EventArgs e)
        {
            foreach (var color in previousStops)
            {
                color.Changed -= Color_Changed;
            }

            previousStops = Stops.ToArray();

            foreach (var color in previousStops)
            {
                color.Changed += Color_Changed;
            }

            GradientChanged?.Invoke(this);
        }

        private void Color_Changed(object sender, EventArgs e)
        {
            GradientChanged?.Invoke(this);
        }

        public override void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.RaisePropertyChanged(propertyName);

            GradientChanged?.Invoke(this);

            if (propertyName == nameof(Start))
            {
                Start.Changed += delegate { GradientChanged?.Invoke(this); };
            }
            else if (propertyName == nameof(End))
            {
                End.Changed += delegate { GradientChanged?.Invoke(this); };
            }
        }
    }
}
