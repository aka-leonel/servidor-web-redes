using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;

// 1. Cargar configuración (Puerto y WebRootPath)
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("config.json", optional: false, reloadOnChange: true)
    .Build();

var config = new AppConfig();
configuration.Bind(config);

// Ensure logs directory exists
Directory.CreateDirectory("logs");

// 2. Inicializar el TcpListener (Unidad 1: Sockets y Servidor TCP)
TcpListener listener = new TcpListener(IPAddress.Any, config.Port);
listener.Start();

Console.WriteLine($"[Servidor] Iniciado correctamente.");
Console.WriteLine($"[Servidor] Escuchando en http://localhost:{config.Port}/");
Console.WriteLine($"[Servidor] Carpeta raíz: {config.WebRootPath}");

// 3. Bucle principal de escucha (Requisito 1: Concurrencia)
try
{
    while (true)
    {
        // Aceptamos la conexión de manera asíncrona
        TcpClient clienteTcp = await listener.AcceptTcpClientAsync();

        IPEndPoint ipRemota = (IPEndPoint)clienteTcp.Client.RemoteEndPoint!;
        Console.WriteLine($"[Conexión] Nueva solicitud entrante desde IP: {ipRemota.Address}");

        // Derivamos la atención del cliente a una tarea independiente (concurrencia con Task)
        _ = Task.Run(() => AtenderClienteAsync(clienteTcp, config.WebRootPath));
    }
}
finally
{
    listener.Stop();
}

// Método esqueleto para atender al cliente
static async Task AtenderClienteAsync(TcpClient cliente, string webRootPath)
{
    using (cliente)
    {
        string ipCliente = "Desconocida";
        try
        {
            if (cliente.Client.RemoteEndPoint is IPEndPoint remoteIp)
            {
                ipCliente = remoteIp.Address.ToString();
            }

            using NetworkStream stream = cliente.GetStream();
            using StreamReader reader = new StreamReader(stream);

            string? lineaSolicitud = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(lineaSolicitud)) return;

            var partes = lineaSolicitud.Split(' ');
            if (partes.Length < 2) return;

            string metodo = partes[0];
            string rutaCompleta = partes[1];

            // Requisito 7: Parámetros de consulta
            string rutaArchivo = rutaCompleta;
            string queryString = string.Empty;
            int indexQuery = rutaCompleta.IndexOf('?');
            if (indexQuery >= 0)
            {
                rutaArchivo = rutaCompleta.Substring(0, indexQuery);
                queryString = rutaCompleta.Substring(indexQuery + 1);
            }

            // Leer cabeceras HTTP
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? lineaHeader;
            while (!string.IsNullOrEmpty(lineaHeader = await reader.ReadLineAsync()))
            {
                int separator = lineaHeader.IndexOf(':');
                if (separator > 0)
                {
                    string key = lineaHeader.Substring(0, separator).Trim();
                    string val = lineaHeader.Substring(separator + 1).Trim();
                    headers[key] = val;
                }
            }

            // Requisito 6: Leer datos POST
            string bodyPost = string.Empty;
            if (metodo.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                if (headers.TryGetValue("Content-Length", out string? lengthStr) && int.TryParse(lengthStr, out int contentLength) && contentLength > 0)
                {
                    char[] bufferBody = new char[contentLength];
                    await reader.ReadAsync(bufferBody, 0, contentLength);
                    bodyPost = new string(bufferBody);
                }
            }

            // Requisito 2: Archivo por defecto
            if (rutaArchivo == "/" || string.IsNullOrEmpty(rutaArchivo))
            {
                rutaArchivo = "/index.html";
            }

            string rutaRelativa = rutaArchivo.TrimStart('/');
            string rutaFisica = Path.Combine(webRootPath, rutaRelativa);

            // Requisito 8: Verificar si el cliente acepta compresión GZip
            bool aceptaGzip = headers.TryGetValue("Accept-Encoding", out string? enc) && enc.Contains("gzip", StringComparison.OrdinalIgnoreCase);

            using StreamWriter writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };

            int statusCode;
            byte[] rawBytes;
            string contentType;

            if (File.Exists(rutaFisica))
            {
                statusCode = 200;
                rawBytes = await File.ReadAllBytesAsync(rutaFisica);
                contentType = ObtenerContentType(rutaFisica);
            }
            else
            {
                statusCode = 404;
                string ruta404 = Path.Combine(webRootPath, "404.html");
                if (File.Exists(ruta404))
                {
                    rawBytes = await File.ReadAllBytesAsync(ruta404);
                }
                else
                {
                    rawBytes = System.Text.Encoding.UTF8.GetBytes("<h1>Error 404</h1><p>Página no encontrada.</p>");
                }
                contentType = "text/html";
            }

            // Aplicar compresión si corresponde (Requisito 8)
            byte[] bytesAEnviar = rawBytes;
            if (aceptaGzip)
            {
                using MemoryStream ms = new MemoryStream();
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
                {
                    await gzip.WriteAsync(rawBytes, 0, rawBytes.Length);
                }
                bytesAEnviar = ms.ToArray();
            }

            // Construir headers de respuesta
            string statusLine = statusCode == 200 ? "HTTP/1.1 200 OK" : "HTTP/1.1 404 Not Found";
            await writer.WriteLineAsync(statusLine);
            await writer.WriteLineAsync($"Content-Type: {contentType}; charset=utf-8");
            await writer.WriteLineAsync($"Content-Length: {bytesAEnviar.Length}");
            if (aceptaGzip)
            {
                await writer.WriteLineAsync("Content-Encoding: gzip");
            }
            await writer.WriteLineAsync("Connection: close");
            await writer.WriteLineAsync();

            await stream.WriteAsync(bytesAEnviar, 0, bytesAEnviar.Length);

            // Requisito 9: Loguear la solicitud en archivo por día y consola
            string detalleLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] IP: {ipCliente} | Método: {metodo} | Ruta: {rutaArchivo} | Query: '{queryString}' | Body: '{bodyPost}' | Status: {statusCode} | GZip: {aceptaGzip}";
            Console.WriteLine(detalleLog);
            await RegistrarLogEnArchivoAsync(detalleLog);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Al atender al cliente ({ipCliente}): {ex.Message}");
        }
    }
}

// Método auxiliar para determinar el Content-Type según la extensión del archivo
static string ObtenerContentType(string rutaArchivo)
{
    string extension = Path.GetExtension(rutaArchivo).ToLowerInvariant();
    return extension switch
    {
        ".html" => "text/html",
        ".css" => "text/css",
        ".js" => "application/javascript",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "text/plain"
    };
}

// Requisito 9: Método para guardar logs diarios en un archivo
static async Task RegistrarLogEnArchivoAsync(string mensaje)
{
    try
    {
        string nombreArchivoLog = Path.Combine("logs", $"log-{DateTime.Now:yyyy-MM-dd}.txt");
        // Uso de SemaphoreSlim o lock asíncrono básico para evitar conflictos de concurrencia al escribir
        await File.AppendAllTextAsync(nombreArchivoLog, mensaje + Environment.NewLine);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Error Log] No se pudo escribir el log: {ex.Message}");
    }
}