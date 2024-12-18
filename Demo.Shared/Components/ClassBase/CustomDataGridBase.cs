using Demo.Shared.Models;
using Demo.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace Demo.Shared.Components.ClassBase
{
    public class CustomDataGridBase : ComponentBase
    {
        [Parameter] public List<string>? Columns { get; set; }
        [Parameter] public List<List<string>>? Rows { get; set; }

        [Parameter]
        public List<FileDocModel> FileDocs { get; set; }
        public bool isLoading = true;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            this.isLoading = false;
        }
    }
}
