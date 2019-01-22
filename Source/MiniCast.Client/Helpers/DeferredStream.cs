using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCast.Client.Helpers
{
    public class DeferredStream<T>
    {
        private struct Entry
        {
            public T Value { get; set; }
            public TimeSpan Time { get; set; }

            public override string ToString()
            {
                return $"{Time} - {Value}";
            }
        }

        public Func<T, T, double, T> Processor { get; }

        public TimeSpan Delay { get; set; }

        private Queue<Entry> queue = new Queue<Entry>();
        private Stopwatch runningTime = new Stopwatch();

        public DeferredStream(Func<T, T, double, T> processor)
        {
            Processor = processor ?? throw new ArgumentNullException(nameof(processor));
            runningTime.Start();
        }

        public void Add(T value)
        {
            queue.Enqueue(new Entry() { Value = value, Time = runningTime.Elapsed });
        }

        public T GetCurrent()
        {
            TimeSpan currentTime = runningTime.Elapsed;
            TimeSpan desiredTime = currentTime - Delay;
            TimeSpan tooOldTime = currentTime - TimeSpan.FromSeconds(Delay.TotalSeconds * 2);

            while (queue.Count > 0 && queue.Peek().Time < tooOldTime)
            {
                queue.Dequeue();
            }

            if (queue.Count == 0)
            {
                return default(T);
            }
            else if (queue.Count == 1)
            {
                return queue.Peek().Value;
            }
            else
            {
                int offset = 0;

                while ((queue.Count - offset) > 2)
                {
                    if ((queue.ElementAt(offset).Time < desiredTime) && (queue.ElementAt(offset + 1).Time >= desiredTime))
                    {
                        // X Y . . ., X < desiredTime < Y
                        var X = queue.ElementAt(offset);
                        var Y = queue.ElementAt(offset + 1);

                        var dist = (Y.Time - X.Time).TotalMilliseconds;
                        if (dist <= 0)
                        {
                            return X.Value;
                        }
                        else
                        {
                            return Processor(X.Value, Y.Value, (desiredTime - X.Time).TotalMilliseconds / dist);
                        }
                    }

                    offset++;
                }

                return queue.Peek().Value;
            }
        }
    }
}
