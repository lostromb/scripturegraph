using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas
{
    public class Conference
    {
        public ConferencePhase Phase { get; set; }
        public int Year { get; set; }

        public Conference(ConferencePhase phase, int year)
        {
            Phase = phase;
            Year = year;
        }

        public override string ToString()
        {
            return Phase.ToString() + " " + Year.ToString();
        }

        public override bool Equals(object? obj)
        {
            Conference? other = obj as Conference;
            if (other == null ||
                other is not Conference)
            {
                return false;
            }

            return Phase == other.Phase &&
                Year == other.Year;
        }

        public override int GetHashCode()
        {
            return Phase.GetHashCode() + Year.GetHashCode();
        }
    }
}
