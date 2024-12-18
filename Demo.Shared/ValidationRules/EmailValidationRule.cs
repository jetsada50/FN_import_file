using ClosedXML.Excel;
using Demo.Shared.ValidationRules.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Shared.ValidationRules
{
    public class EmailValidationRule : IFileValidationRule
    {
        public void Validate(IXLWorksheet worksheet, List<string> headers) 
        {
            if(!headers.Contains("Eamil"))
            {
                throw new Exception("Missing 'Email' column");
            }

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var emailCell = row.Cell(headers.IndexOf("Email") + 1).Value.ToString();
                if(!emailCell.Contains("@") || !emailCell.Contains(".") )
                {
                    {
                        throw new Exception($"Invalid email: {emailCell}");
                    }
                }
            }
        }
    }
}
