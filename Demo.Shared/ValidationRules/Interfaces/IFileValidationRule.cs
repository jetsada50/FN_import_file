using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Shared.ValidationRules.Interfaces
{
    public interface IFileValidationRule
    {
        void Validate(IXLWorksheet worksheet, List<string> headers);
    }
}
