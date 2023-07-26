using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilentThief
{
    public record struct SpecialityInfo(string Name, string Code, int BudgetPlaces, int Quota1BudgetPlaces, int Quota2BudgetPlaces);
}
