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
        public Color Start { get; set; } = new Color() { ScA = 1, ScR = 0, ScG = 0, ScB = 0 };
        public Color End { get; set; } = new Color() { ScA = 1, ScR = 1, ScG = 1, ScB = 1 };

        public GradientStopCollection Stops { get; } = new GradientStopCollection();

        public event Action<GradientSpan> GradientChanged;

        private GradientStop[] previousStops = new GradientStop[0];

        public GradientSpan()
        {
            Stops.Changed += Stops_Changed;
        }

        public IEnumerable<(Color color, double offset)> OrderedStops
        {
            get
            {
                yield return (Start, 0);

                foreach (var stop in Stops.OrderBy(s => s.Offset))
                {
                    yield return (stop.Color, stop.Offset);
                }

                yield return (End, 0);
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
            foreach(var color in previousStops)
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
        }
    }
}
