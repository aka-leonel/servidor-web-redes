using Microsoft.Extensions.Configuration;

// Construir la configuración leyendo el archivo externo config.json
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("config.json", optional: false, reloadOnChange: true)
    .Build();

// Mapear a nuestra clase o leer directamente
var config = new AppConfig();
configuration.Bind(config);

Console.WriteLine($"[Config] Puerto configurado: {config.Port}");
Console.WriteLine($"[Config] Carpeta web configurada: {config.WebRootPath}");