# Servidor Web Simple en C# (.NET 9.0)

Proyecto final para la materia **Programación sobre Redes** (IFTS N° 18). Consiste en la implementación de un servidor web HTTP/TCP concurrente desarrollado desde cero en C#.

## Características Principales
- **Concurrencia:** Soporte para múltiples solicitudes simultáneas (Unidad 1).
- **Configuración Externa:** Lectura dinámica de parámetros como el puerto de escucha y la ruta de archivos estáticos mediante `config.json` (Unidad 3).
- **Archivos Estáticos:** Servido de páginas web instituidas (`index.html`, estilos, scripts e imágenes).
- **Control de Versiones:** Repositorio estructurado con Git y buenas prácticas de desarrollo.

## Requisitos Previos
- [.NET 9.0 SDK](https://dotnet.microsoft.com/)

## Ejecución
1. Clonar el repositorio.
2. Restaurar dependencias y compilar:
   ```bash
   dotnet restore
   dotnet run