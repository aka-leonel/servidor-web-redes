# Servidor Web Concurrente en C# (.NET 9.0)

Proyecto final desarrollado para la materia **Programación sobre Redes** (IFTS N° 18). Consiste en un servidor web HTTP/TCP desarrollado desde cero en C#.

## 🚀 Características y Requisitos Implementados

1. **Concurrencia (Unidad 1):** Soporte para atender un número indefinido de solicitudes en paralelo utilizando tareas asíncronas (`Task.Run`).
2. **Archivo por Defecto (Unidad 3):** Sirve automáticamente el archivo `index.html` si la URL solicitada no especifica un recurso.
3. **Configuración Externa:** Lectura dinámica del puerto de escucha y la ruta raíz mediante el archivo `config.json`.
4. **Manejo de Errores 404:** Devuelve el código de estado HTTP 404 y un documento personalizado (`404.html`) cuando se solicita un recurso inexistente.
5. **Métodos GET y POST:** Capacidad de procesar solicitudes GET y capturar/loguear el cuerpo (*body*) de solicitudes POST.
6. **Parámetros de Consulta (Query String):** Detección y registro en consola y archivos de los parámetros enviados por URL (`?param=valor`).
7. **Compresión GZip (Unidad 3):** Soporte para compresión de archivos de respuesta utilizando flujos de compresión nativos de .NET (`GZipStream`) si el cliente lo solicita.
8. **Logueo Diario:** Registro automático de todas las solicitudes entrantes (incluyendo IP de origen, método, ruta, estado HTTP y uso de compresión) en archivos diarios dentro de la carpeta `logs/`.

## 🛠️ Requisitos Previos
- [.NET 9.0 SDK](https://dotnet.microsoft.com/)

## ⚙️ Configuración (`config.json`)
El servidor lee su configuración del archivo `config.json` en la raíz:
```json
{
  "Port": 8080,
  "WebRootPath": "wwwroot"
}
```

## Ejecución
1. Clonar el repositorio.
2. Restaurar dependencias y compilar:
   ```bash
   dotnet restore
   dotnet run
   ```
3. Abrir el navegador en http://localhost:8080/