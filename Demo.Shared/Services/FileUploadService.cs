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
using DocumentFormat.OpenXml;

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
        var readFileStopwatch = new Stopwatch();

        var data = new List<FileDocModel>();
        var errors = new List<ErrorValidateModel>();

        if (!ValidateFileType(file.Name))
            return (false, "Invalid file type. Only .xlsx is supported.");

        using (var stream = new MemoryStream())
        {
            await file.OpenReadStream(10 * 1024 * 1024).CopyToAsync(stream);
            using (var spreadsheetDocument = SpreadsheetDocument.Open(stream, false))
            {
                readFileStopwatch.Start();

                WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;

                Sheet sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault();

                WorksheetPart worksheetPart = (WorksheetPart)(spreadsheetDocument.WorkbookPart.GetPartById(sheet.Id));

                var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

                readFileStopwatch.Stop();

                validationStopwatch.Start();
                using (OpenXmlReader reader = OpenXmlReader.Create(worksheetPart))
                {
                    bool isFirstRow = true;
                    var regexPrefix = new Regex(@"^(นาย|นาง|นางสาว|Mr\.|Mrs\.|Miss)$");
                    var regexMoblie = new Regex(@"^(?:\d{3}|\d{4}|0[689]\d{8}|0[689]\d{1}-\d{4}-\d{4}|(?:\+66|0066)?[689]\d{8}|\+?\d{9,15})$");


                    while (reader.Read())
                    {
                        if(reader.ElementType == typeof(Row) && reader.IsStartElement)
                        {
                            if (isFirstRow)
                            {
                                isFirstRow = false;
                                reader.ReadFirstChild();
                                continue;
                            }

                            var row = (Row)reader.LoadCurrentElement();
                            var cellDictionary = row.Elements<Cell>().ToDictionary(c => GetColumnName(c.CellReference), c => c);

                            var result = new FileDocModel
                            {
                                Prefix = cellDictionary.ContainsKey("A") ? GetCellValue(spreadsheetDocument, cellDictionary["A"], sharedStringTable) : string.Empty,
                                Name = cellDictionary.ContainsKey("B") ? GetCellValue(spreadsheetDocument, cellDictionary["B"], sharedStringTable) : string.Empty,
                                Surname = cellDictionary.ContainsKey("C") ? GetCellValue(spreadsheetDocument, cellDictionary["C"], sharedStringTable) : string.Empty,
                                Department = cellDictionary.ContainsKey("D") ? GetCellValue(spreadsheetDocument, cellDictionary["D"], sharedStringTable) : string.Empty,
                                Affiliation = cellDictionary.ContainsKey("E") ? GetCellValue(spreadsheetDocument, cellDictionary["E"], sharedStringTable) : string.Empty,
                                PhoneNumber = cellDictionary.ContainsKey("F") ? GetCellValue(spreadsheetDocument, cellDictionary["F"], sharedStringTable) : string.Empty,
                                Status = cellDictionary.ContainsKey("G") ? GetCellValue(spreadsheetDocument, cellDictionary["G"], sharedStringTable) : string.Empty,
                            };

                            if (!string.IsNullOrWhiteSpace(result.Prefix) && !regexPrefix.IsMatch(result.Prefix))
                            {
                                errors.Add(new ErrorValidateModel
                                {
                                    Row = row.RowIndex,
                                    Message = "Prefix is must นาย|นาง|นางสาว|Mr\\.|Mrs\\.|Miss."
                                });
                            }
                            if (string.IsNullOrWhiteSpace(result.Name))
                            {
                                errors.Add(new ErrorValidateModel
                                {
                                    Row = row.RowIndex,
                                    Message = "Name is required"
                                });
                            }
                            if (string.IsNullOrWhiteSpace(result.Surname))
                            {
                                errors.Add(new ErrorValidateModel
                                {
                                    Row = row.RowIndex,
                                    Message = "Surname is required"
                                });
                            }
                            if (string.IsNullOrWhiteSpace(result.Department))
                            {
                                errors.Add(new ErrorValidateModel
                                {
                                    Row = row.RowIndex,
                                    Message = "Department is required"
                                });
                            }
                            if (string.IsNullOrWhiteSpace(result.Affiliation))
                            {
                                errors.Add(new ErrorValidateModel
                                {
                                    Row = row.RowIndex,
                                    Message = "Affiliation is required"
                                });
                            }
                            if (string.IsNullOrWhiteSpace(result.PhoneNumber))
                            {
                                errors.Add(new ErrorValidateModel
                                {
                                    Row = row.RowIndex,
                                    Message = "PhoneNumber is required"
                                });
                            }
                            // Validate PhoneNumber using regex
                            if (!string.IsNullOrWhiteSpace(result.PhoneNumber) && !regexMoblie.IsMatch(result.PhoneNumber))
                            {
                                errors.Add(new ErrorValidateModel
                                {
                                    Row = row.RowIndex,
                                    Message = $"Invalid PhoneNumber: {result.PhoneNumber}"
                                });
                            }
                            if (string.IsNullOrWhiteSpace(result.Status))
                            {
                                errors.Add(new ErrorValidateModel
                                {
                                    Row = row.RowIndex,
                                    Message = "Status is required"
                                });
                            }

                            data.Add(result);
                        }
                    }
                    validationStopwatch.Stop();
                }
            }

            if (errors.Any())
            {
                stopwatch.Stop();
                var errorMessages = string.Join("\n", errors.Select(e => $"Row {e.Row}: {e.Message}"));
                Console.WriteLine($"Read file in {readFileStopwatch.ElapsedMilliseconds} ms. - File validated in {validationStopwatch.ElapsedMilliseconds} ms");
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
            return (true, $"Read file in {readFileStopwatch.ElapsedMilliseconds} ms. - File validated in {validationStopwatch.ElapsedMilliseconds} ms and uploaded in {uploadStopwatch.ElapsedMilliseconds} ms.");

        }


    }

    private string GetCellValue(SpreadsheetDocument document, Cell cell, SharedStringTable sharedStringTable)
    {
        if (cell == null || cell.CellValue == null)
        {
            return string.Empty;
        }

        var value = cell.CellValue.InnerText;

        // If the value is a shared string, get the actual string from the SharedStringTable
        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            return sharedStringTable != null ? sharedStringTable.ChildElements[int.Parse(value)].InnerText : string.Empty;
        }

        return value;
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



    private bool ValidateFileType(string fileName)
    {
        return Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
    }
}
