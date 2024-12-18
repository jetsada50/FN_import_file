//using System.IO;
//using System.Text.RegularExpressions;

//namespace Demo.Shared.Services
//{
//    public class FileValidationService
//    {
//        public (int RowCount, int ColumnCount, List<string> Headers) ValidateCSVFile(Stream fileStream)
//        {
//            var headers = new List<string>();
//            var rowCount = 0;
//            var columnCount = 0;

//            using var reader = new StreamReader(fileStream);
//            while (!reader.EndOfStream)
//            {
//                var line = reader.ReadLine();
//                if (line == null) continue;

//                var values = line.Split(',');

//                // เก็บ Headers
//                if (rowCount == 0)
//                {
//                    headers = values.ToList();
//                    columnCount = values.Length;
//                }
//                else
//                {
//                    // ตรวจสอบข้อมูลในคอลัมน์ เบอร์โทรศัพท์ (คอลัมน์ที่ 4 เป็นตัวอย่าง)
//                    for (int i = 0; i < values.Length; i++)
//                    {
//                        if (string.IsNullOrWhiteSpace(values[i]))
//                        {
//                            values[i] = "X"; // เพิ่ม X เมื่อข้อมูลว่าง
//                        }
//                        else if (i == 4 && !Regex.IsMatch(values[i], @"^0\d{9,}$"))
//                        {
//                            values[i] = "X"; // ตรวจสอบเบอร์โทรศัพท์
//                        }
//                    }
//                }

//                rowCount++;
//            }

//            return (rowCount, columnCount, headers);
//        }
//    }
//}

using Demo.Shared.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Demo.Shared.Services
{
    public class FileValidationService
    {
        public List<string> ValidateData(List<FileDocModel> data)
        {
            var errors = new ConcurrentBag<string>();

            Parallel.ForEach(data, (item, _, index) =>
            {
                var rowNumber = index + 2;

                if (string.IsNullOrWhiteSpace(item.Prefix))
                    errors.Add($"Row {rowNumber}: Prefix is required");

                if (string.IsNullOrWhiteSpace(item.Name))
                    errors.Add($"Row {rowNumber}: Name is required");

                if (string.IsNullOrWhiteSpace(item.Surname))
                    errors.Add($"Row {rowNumber}: Surname is required");

                if (!string.IsNullOrWhiteSpace(item.PhoneNumber) && !Regex.IsMatch(item.PhoneNumber, @"^\d{10}$"))
                    errors.Add($"Row {rowNumber}: Invalid phone number");
            });

            return errors.ToList();
        }
    }
}
