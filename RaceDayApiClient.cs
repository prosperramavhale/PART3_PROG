using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace RaceDay.MVC.Services;
public class RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor)
{
    private HttpClient Client { get { var c = http; c.DefaultRequestHeaders.Authorization = null; var token = accessor.HttpContext?.Session.GetString("Token"); if (!string.IsNullOrWhiteSpace(token)) c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); return c; } }
    public async Task<JsonElement?> Get(string path) { var r=await Client.GetAsync(path); if(!r.IsSuccessStatusCode)return null; using var d=JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return d.RootElement.Clone(); }
    public async Task<(bool ok,string message,JsonElement? data)> Post(string path,object body) { var r=await Client.PostAsJsonAsync(path,body); var s=await r.Content.ReadAsStringAsync(); JsonElement? data=null; try { using var d=JsonDocument.Parse(s); data=d.RootElement.Clone(); } catch {} return (r.IsSuccessStatusCode, ExtractMessage(s,r),data); }
    public async Task<(bool ok,string message)> Put(string path,object body) { var r=await Client.PutAsJsonAsync(path,body); var s=await r.Content.ReadAsStringAsync(); return (r.IsSuccessStatusCode,ExtractMessage(s,r)); }
    public async Task<bool> Delete(string path) => (await Client.DeleteAsync(path)).IsSuccessStatusCode;

    public async Task<(bool ok,string message,JsonElement? data)> UploadFile(string path, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", Path.GetFileName(file.FileName));
        var response = await Client.PostAsync(path, content);
        var body = await response.Content.ReadAsStringAsync();
        JsonElement? data = null;
        try { using var doc = JsonDocument.Parse(body); data = doc.RootElement.Clone(); } catch { }
        return (response.IsSuccessStatusCode, ExtractMessage(body, response), data);
    }

    private static string ExtractMessage(string s,HttpResponseMessage r) { try { using var d=JsonDocument.Parse(s); if(d.RootElement.TryGetProperty("message",out var m))return m.GetString()??"Request failed."; } catch {} return r.IsSuccessStatusCode?"":$"The API returned {(int)r.StatusCode}. Check that the API is running."; }
}
