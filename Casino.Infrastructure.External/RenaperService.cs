using Casino.Application.Interfaces;

namespace Casino.Infrastructure.External;

public class RenaperService : IRenaperService
{
    // A FUTURO: Acá inyectaremos un HttpClient para pegarle a la API real
    // private readonly HttpClient _httpClient;
    // public RenaperService(HttpClient httpClient) { _httpClient = httpClient; }

    public async Task<bool> EsMayorDeEdadAsync(string dni)
    {
        // ====================================================================
        // TODO: ESPERANDO API DEL RENAPER (GRUPO EXTERNO)
        // ====================================================================
        // Acá irá la llamada HTTP real cuando los chicos la terminen. 
        // Ejemplo de cómo será:
        // var response = await _httpClient.GetAsync($"https://api-renaper-grupoX.com/validar/{dni}");
        // var data = await response.Content.ReadFromJsonAsync<RenaperDTO>();
        // return data.Edad >= 18;
        // ====================================================================

        // Por ahora simulamos la latencia de red y devolvemos siempre true
        // (Podés meter un if acá si querés que ciertos DNI fallen para probar)
        await Task.Delay(200);

        return true;
    }
}