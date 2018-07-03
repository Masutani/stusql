using Microsoft.Analytics.Interfaces;
using Microsoft.Analytics.Types.Sql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace USQLSharedLib
{
    public class TimeSeriesOperator
    {
        [SqlUserDefinedApplier]
        public class SplitLocfApplier : IApplier
        {
            string startColumn;
            string endColumn;
            string startValueColumn;
            TimeSpan binSize;

            public SplitLocfApplier(string startColumn, string endColumn, string startValueColumn, TimeSpan binSize)
            {
                this.startColumn = startColumn;
                this.endColumn = endColumn;
                this.startValueColumn = startValueColumn;
                this.binSize = binSize;
            }

            public override IEnumerable<IRow> Apply(IRow input, IUpdatableRow output)
            {
                DateTime startTime = input.Get<DateTime>(startColumn);
                DateTime endTime = input.Get<DateTime>(endColumn);
                var startValueCol = (from x in input.Schema where x.Name == startValueColumn select x).First();

                if (startValueCol.Type == typeof(bool))
                {
                    var startValue = input.Get<bool>(startValueColumn);
                    return locf<bool>(startTime, endTime, startValue, output);
                }
                else if (startValueCol.Type == typeof(int))
                {
                    var startValue = input.Get<int>(startValueColumn);
                    return locf<int>(startTime, endTime, startValue, output);
                }
                else if (startValueCol.Type == typeof(double))
                {
                    var startValue = input.Get<double>(startValueColumn);
                    return locf<double>(startTime, endTime, startValue, output);
                }
                else if (startValueCol.Type == typeof(string))
                {
                    var startValue = input.Get<string>(startValueColumn);
                    return locf<string>(startTime, endTime, startValue, output);
                }
                else { return null; }
            }

            IEnumerable<IRow> locf<Type>(DateTime startTime, DateTime endTime, Type startValue, IUpdatableRow output)
            {
                var startBin = getOffsetBin(startTime, this.binSize);
                var endBin = getOffsetBin(endTime, this.binSize);

                TimeSpan range;
                for (var t = startBin; t <= endBin; t = t + this.binSize)
                {
                    if (t == startBin)
                    {
                        if (t == endBin)
                        {
                            range = endTime - startTime;
                        }
                        else
                        {
                            range = t + this.binSize - startTime;
                        }
                    }
                    else if (t == endBin)
                    {
                        range = endTime - t;
                    }
                    else
                    {
                        range = this.binSize;
                    }

                    if (range > new TimeSpan(this.binSize.Ticks / 2))
                    {
                        output.Set<DateTime>("bin", t);
                        output.Set<Type>("value", startValue);
                        yield return output.AsReadOnly();
                    }
                }
            }
        }

        [SqlUserDefinedApplier]
        public class SplitResamplerApplier : IApplier
        {
            string startColumn;
            string endColumn;
            string startValueColumn;
            string endValueColumn;
            TimeSpan binSize;

            public SplitResamplerApplier(string startColumn, string endColumn, string startValueColumn, string endValueColumn, TimeSpan binSize)
            {
                this.startColumn = startColumn;
                this.endColumn = endColumn;
                this.startValueColumn = startValueColumn;
                this.endValueColumn = endValueColumn;
                this.binSize = binSize;
            }

            public override IEnumerable<IRow> Apply(IRow input, IUpdatableRow output)
            {
                DateTime startTime = input.Get<DateTime>(startColumn);
                DateTime endTime = input.Get<DateTime>(endColumn);
                var startValueCol = (from x in input.Schema where x.Name == startValueColumn select x).First();
                var endValueCol = (from x in input.Schema where x.Name == endValueColumn select x).First();

                if (startValueCol.Type == typeof(double))
                {
                    double startValue = input.Get<double>(startValueColumn);
                    double endValue = input.Get<double>(endValueColumn);
                    return interpolate(startTime, endTime, startValue, endValue, output);
                }
                else if (startValueCol.Type == typeof(int))
                {
                    double startValue = input.Get<int>(startValueColumn);
                    double endValue = input.Get<int>(endValueColumn);
                    return interpolate(startTime, endTime, startValue, endValue, output);
                }
                else
                {
                    return null;
                }
            }


            IEnumerable<IRow> interpolate(DateTime startTime, DateTime endTime, double startValue, double endValue, IUpdatableRow output)
            {
                var v = setupSpanVariables(startTime, endTime, startValue, endValue);

                var yhs1 = v.slope * (v.startBin.Ticks + this.binSize.Ticks) + v.intercept;
                var yhe = v.slope * v.endBin.Ticks + v.intercept;

                double after_value = 0;
                double split_delta = 0;
                for (DateTime t = v.startBin; t <= v.endBin; t = t + this.binSize)
                {
                    if (t == v.startBin)
                    {
                        if (t == v.endBin)
                        {
                            split_delta = endValue - startValue;
                            after_value = endValue;
                        }
                        else
                        {
                            split_delta = yhs1 - startValue;
                            after_value = yhs1;
                        }
                    }
                    else if (t == v.endBin)
                    {
                        split_delta = endValue - yhe;
                        after_value = endValue;
                    }
                    else
                    {
                        split_delta = v.slope * this.binSize.Ticks;
                        after_value = after_value + v.slope * this.binSize.Ticks;
                    }
                    output.Set<DateTime>("bin", t);
                    output.Set<double>("split_delta", split_delta);
                    output.Set<double>("after_value", after_value);

                    yield return output.AsReadOnly();
                }

            }

            struct SetupVariables
            {
                public DateTime startBin;
                public DateTime endBin;
                public double slope;
                public double intercept;
            }

            SetupVariables setupSpanVariables(DateTime startTime, DateTime endTime, double startValue, double endValue)
            {
                var slope = (endValue - startValue) / (endTime.Ticks - startTime.Ticks);
                return (new SetupVariables()
                {
                    startBin = getOffsetBin(startTime, this.binSize),
                    endBin = getOffsetBin(endTime, this.binSize),
                    slope = slope,
                    intercept = startValue - slope * startTime.Ticks
                });
            }

        }

        static DateTime getOffsetBin(DateTime time, TimeSpan binSize)
        {
            var dayOffset = time.Subtract(time.Date);
            long binIndex = dayOffset.Ticks / binSize.Ticks;
            var binOffset = new TimeSpan(binIndex * binSize.Ticks);
            DateTime binStart = time.Date + binOffset;
            return (binStart);
        }
    }
}
