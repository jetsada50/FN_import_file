using Demo.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace Demo.Pages.ClassBase
{
    public class ReuseNumberBase : ComponentBase
    {
        public List<FileDocModel> FileDoc { get; set; }
        public List<string> DataColumns = new();
        public List<List<string>> DataRows = new();
        public void OnUpload(List<FileDocModel> files)
        {
            this.FileDoc = files;
            Console.WriteLine($"File: {files[0].Name}");
        }
        public void SetColumns(List<string> columns)
        {
            DataColumns = columns;
        }

        public void SetData(List<List<string>> rows)
        {
            DataRows = rows;
        }

    }
}
