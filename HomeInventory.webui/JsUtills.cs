using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public class JsUtills(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async Task OpenDialog(ElementReference dialog)
    {
        await _jsRuntime.InvokeVoidAsync("openDialog", dialog);
    }

    public async Task CloseDialog(ElementReference dialog)
    {
        await _jsRuntime.InvokeVoidAsync("closeDialog", dialog);
    }
}