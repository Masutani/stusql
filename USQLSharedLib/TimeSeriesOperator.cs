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
        public class SplitResamplerApplier : IApplier
        {
            private string startColumn;
            private string endColumn;
            private string startValueColumn;
            private string endValueColumn;
            static DateTime centuryBegin = new DateTime(2001, 1, 1);

            public SplitResamplerApplier(string startColumn, string endColumn, string startValueColumn, string endValueColumn)
            {
                this.startColumn = startColumn;
                this.endColumn = endColumn;
                this.startValueColumn = startValueColumn;
                this.endValueColumn = endValueColumn;
            }

            public override IEnumerable<IRow> Apply(IRow input, IUpdatableRow output)
            {
                DateTime startTime = input.Get<DateTime>(startColumn);
                DateTime endTime = input.Get<DateTime>(endColumn);
                double startValue = input.Get<double>(startValueColumn);
                double endValue = input.Get<double>(endValueColumn);

                var startHour = startTime.Date.AddHours(startTime.Hour);
                var endHour = endTime.Date.AddHours(endTime.Hour);

                var startTimeTick = startTime.Subtract(centuryBegin).TotalHours;
                var endTimeTick = endTime.Subtract(centuryBegin).TotalHours;
                var startHourTick = startHour.Subtract(centuryBegin).TotalHours;
                var endHourTick = endHour.Subtract(centuryBegin).TotalHours;

                var slope = (endValue - startValue) / (endTimeTick - startTimeTick);
                var intercept = startValue - slope * startTimeTick;

                var yhs1 = slope * (startHourTick + 1) + intercept;
                var yhe = slope * endHourTick + intercept;

                double after_value = 0;
                double split_delta = 0;
                for (var t = startHour; t <= endHour; t = t.AddHours(1))
                {
                    if (t == startHour)
                    {
                        if (t == endHour)
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
                    else if (t == endHour)
                    {
                        split_delta = endValue - yhe;
                        after_value = endValue;
                    }
                    else
                    {
                        split_delta = slope * 1;
                        after_value = after_value + slope * 1;
                    }
                    output.Set<DateTime>("datehour", t);
                    output.Set<double>("split_delta", split_delta);
                    output.Set<double>("after_value", after_value);

                    yield return output.AsReadOnly();
                }
            }
        }
    }
}
