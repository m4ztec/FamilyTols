using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HomeInventory.webui;

public static class JSRuntimeExtension
{
    extension(IJSRuntime jsRuntime)
    {
        public async Task OpenDialog(ElementReference dialog) => await jsRuntime.InvokeVoidAsync("openDialog", dialog);

        public async Task CloseDialog(ElementReference dialog) => await jsRuntime.InvokeVoidAsync("closeDialog", dialog);
    }
}