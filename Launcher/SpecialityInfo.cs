using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilentThief
{
    public struct SpecialityInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int BudgetPlaces { get; set; }
        public int Quota1BudgetPlaces { get; set; }
        public int Quota2BudgetPlaces { get; set; }
    }
}
