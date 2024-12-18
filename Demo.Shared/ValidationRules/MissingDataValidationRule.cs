using ClosedXML.Excel;
using Demo.Shared.ValidationRules.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Shared.ValidationRules
{
    public class MissingDataValidationRule : IFileValidationRule
    {
        public void Validate(IXLWorksheet worksheet, List<string> headers)
        {
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                foreach (var header in headers)
                {
                    var cell = row.Cell(headers.IndexOf(header) + 1);
                    if (string.IsNullOrEmpty(cell.GetValue<string>()))
                    {
                        throw new Exception($"Missing data in column '{header}' at row {row.RowNumber()}.");
                    }
                }
            }
        }
    }
}
