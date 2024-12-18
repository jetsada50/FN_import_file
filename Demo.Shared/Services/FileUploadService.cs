using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using Demo.Shared.Models;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using System.IO;

public class FileUploadService
{
    private readonly HttpClient _httpClient;

    public FileUploadService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private string GetColumnName(string cellReference)
    {
        return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
    }

    public async Task<(bool Success, string Message)> ValidateAndUploadFileAsync(IBrowserFile file)
    {
        var stopwatch = Stopwatch.StartNew();
        var validationStopwatch = new Stopwatch();
        var uploadStopwatch = new Stopwatch();

        var data = new List<FileDocModel>();
        var errors = new List<ErrorValidateModel>();

        if (!ValidateFileType(file.Name))
            return (false, "Invalid file type. Only .xlsx is supported.");

        try
        {
            // อ่านไฟล์จาก IBrowserFile เป็น MemoryStream
            using var stream = new MemoryStream();
            await file.OpenReadStream(10 * 1024 * 1024).CopyToAsync(stream);
            stream.Position = 0;

            // เปิดไฟล์ Excel
            using var document = SpreadsheetDocument.Open(stream, false);
            var sharedStringTable = document.WorkbookPart.SharedStringTablePart?.SharedStringTable;

            var sheetData = document.WorkbookPart.WorksheetParts.First().Worksheet.Elements<SheetData>().First();

            //return (false, "Test");

            var rows = sheetData.Elements<Row>().Skip(1).ToList();

            

            var regex = new Regex(@"^(?:\d{3}|\d{4}|0[689]\d{8}|(?:\+66|0066)?[689]\d{8}|\+?\d{9,15})$");

            // เริ่ม Validation Timer
            validationStopwatch.Start();

            data = rows.AsParallel().Select(row =>
            {
                var cellDictionary = row.Elements<Cell>()
                                        .ToDictionary(c => GetColumnName(c.CellReference), c => c);

                var result = new FileDocModel
                {
                    Prefix = GetCellValue(cellDictionary, "A", sharedStringTable),
                    Name = GetCellValue(cellDictionary, "B", sharedStringTable),
                    Surname = GetCellValue(cellDictionary, "C", sharedStringTable),
                    Department = GetCellValue(cellDictionary, "D", sharedStringTable),
                    Affiliation = GetCellValue(cellDictionary, "E", sharedStringTable),
                    PhoneNumber = GetCellValue(cellDictionary, "F", sharedStringTable),
                    Status = GetCellValue(cellDictionary, "G", sharedStringTable)
                };

                var rowIndex = row.RowIndex?.Value.ToString() ?? "Unknown";

                // Validation
                if (string.IsNullOrWhiteSpace(result.Prefix))
                    errors.Add(new ErrorValidateModel { Row = rowIndex, Message = "Prefix is required" });
                if (string.IsNullOrWhiteSpace(result.Name))
                    errors.Add(new ErrorValidateModel { Row = rowIndex, Message = "Name is required" });
                if (string.IsNullOrWhiteSpace(result.Surname))
                    errors.Add(new ErrorValidateModel { Row = rowIndex, Message = "Surname is required" });
                if (string.IsNullOrWhiteSpace(result.Department))
                    errors.Add(new ErrorValidateModel { Row = rowIndex, Message = "Department is required" });
                if (string.IsNullOrWhiteSpace(result.Affiliation))
                    errors.Add(new ErrorValidateModel { Row = rowIndex, Message = "Affiliation is required" });
                if (!regex.IsMatch(result.PhoneNumber))
                    errors.Add(new ErrorValidateModel { Row = rowIndex, Message = $"Invalid phone number: {result.PhoneNumber}" });
                if (string.IsNullOrWhiteSpace(result.Status))
                    errors.Add(new ErrorValidateModel { Row = rowIndex, Message = "Status is required" });

                return result;
            }).ToList();
            validationStopwatch.Stop();

            if (errors.Any())
            {
                stopwatch.Stop();
                var errorMessages = string.Join("\n", errors.Select(e => $"Row {e.Row}: {e.Message}"));
                return (false, $"Validation failed:\n{errorMessages}");
            }

            // Upload File to API
            uploadStopwatch.Start();
            stream.Position = 0; // รีเซ็ตตำแหน่ง Stream สำหรับ Upload
            var uploadResult = await UploadFileToApiAsync(stream, file.Name, "/api/file/import");
            uploadStopwatch.Stop();

            if (!uploadResult.Success)
            {
                stopwatch.Stop();
                return (false, uploadResult.Message);
            }

            stopwatch.Stop();
            return (true, $"File validated in {validationStopwatch.ElapsedMilliseconds} ms and uploaded in {uploadStopwatch.ElapsedMilliseconds} ms.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return (false, $"An error occurred: {ex.Message}. Total Time: {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private string GetCellValue(Dictionary<string, Cell> cellDictionary, string column, SharedStringTable sharedStringTable)
    {
        if (cellDictionary.ContainsKey(column))
        {
            var cell = cellDictionary[column];
            if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                return sharedStringTable?.ElementAt(int.Parse(cell.CellValue.Text))?.InnerText ?? string.Empty;

            return cell.CellValue?.Text ?? string.Empty;
        }
        return string.Empty;
    }


    private async Task<(bool Success, string Message)> UploadFileToApiAsync(Stream fileStream, string fileName, string apiUrl)
    {
        var uploadStopwatch = Stopwatch.StartNew(); // เริ่มจับเวลา Upload

        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            content.Add(fileContent, "file", fileName);

            var response = await _httpClient.PostAsync(apiUrl, content);

            uploadStopwatch.Stop(); // หยุดจับเวลาเมื่อ Upload เสร็จสิ้น
            Console.WriteLine($"File upload completed in {uploadStopwatch.ElapsedMilliseconds} ms");

            if (response.IsSuccessStatusCode)
                return (true, $"File uploaded successfully in {uploadStopwatch.ElapsedMilliseconds} ms");
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                return (false, $"Failed to upload file. API Response: {response.StatusCode} - {errorResponse}. Upload Time: {uploadStopwatch.ElapsedMilliseconds} ms");
            }
        }
        catch (Exception ex)
        {
            uploadStopwatch.Stop();
            return (false, $"An error occurred during file upload: {ex.Message}. Upload Time: {uploadStopwatch.ElapsedMilliseconds} ms");
        }
    }


    private string GetCellValue(DocumentFormat.OpenXml.Spreadsheet.Cell cell, SharedStringTable sharedStringTable)
    {
        if (cell?.CellValue == null)
            return string.Empty;

        if (cell.DataType != null && cell.DataType == DocumentFormat.OpenXml.Spreadsheet.CellValues.SharedString)
        {
            // ดึงข้อมูลจาก SharedStringTable ตาม Index
            if (int.TryParse(cell.CellValue.Text, out var sharedStringIndex) && sharedStringTable != null)
            {
                return sharedStringTable.ElementAt(sharedStringIndex)?.InnerText ?? string.Empty;
            }
        }

        // Return ค่าปกติ (ไม่ใช่ Shared String)
        return cell.CellValue.Text;
    }


    private bool IsValidPhoneNumber(string phoneNumber)
    {
        return !string.IsNullOrEmpty(phoneNumber) && System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\d{10}$");
    }

    private bool ValidateFileType(string fileName)
    {
        return Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
    }
}
