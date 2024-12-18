using ClosedXML.Excel;
using Demo.Shared.ValidationRules.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Shared.ValidationRules
{
    public class PhoneNumberValidationRule : IFileValidationRule
    {
        public void Validate(IXLWorksheet worksheet, List<string> headers)
        {
            if (!headers.Contains("PhoneNumber"))
            {
                throw new Exception("Missing 'PhoneNumber' column.");
            }

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var phoneCell = row.Cell(headers.IndexOf("PhoneNumber") + 1).Value.ToString();
                if (!long.TryParse(phoneCell, out _) || phoneCell.Length != 10)
                {
                    throw new Exception($"Invalid phone number: {phoneCell}");
                }
            }
        }
    }
}
