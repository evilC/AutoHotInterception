using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHotInterception.Helpers
{
    public class ScanCodeHelper
    {
        public TranslatedKey TranslateScanCodes(List<ManagedWrapper.Stroke> strokes)
        {
            return null;
        }
    }

    public class TranslatedKey
    {
        public ushort AhkCode { get; set; }
        public int State { get; set; }
    }
}
