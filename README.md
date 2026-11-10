# TranscriptionMedia

Aplicación de escritorio para Windows que convierte audio en español a texto. Podés grabar desde el micrófono o transcribir un archivo. El procesamiento y el historial quedan en tu computadora.

## Descargar

La versión portable se publicará en [Releases](https://github.com/KHiga007/TranscriptionMedia/releases). Todavía no hay una descarga disponible.

Cuando esté publicada, descargá el paquete, extraé su contenido y abrí **TranscriptionMedia.exe**. El paquete portable incluirá el entorno de ejecución de .NET y el modelo Whisper. Conservá los archivos extraídos juntos.


## Uso

- **Grabar audio** usa el micrófono de la computadora. Podés pausar, reanudar o detener la grabación.
- **Subir audio** permite seleccionar y confirmar un archivo. La app muestra el progreso y avisa cuando termina.
- **Historial** permite buscar y abrir transcripciones guardadas. También podés copiar el texto o exportarlo como TXT.

Formatos admitidos: WAV, MP3, M4A, AAC, WMA, FLAC, OGG, OGA y OPUS. El límite por archivo es de 2 GB y 12 horas. El motor intenta usar una GPU dedicada compatible con Vulkan, luego una integrada y finalmente la CPU.

El historial se guarda en **datos/** junto a la app, cifrado con SQLCipher y con una clave protegida por Windows DPAPI. Los TXT exportados quedan en texto plano. Excluí **datos/** al compartir la app.

## Arquitectura

| Carpeta | Responsabilidad |
| --- | --- |
| [Controllers](Controllers/) | Coordina el historial y las solicitudes de transcripción. |
| [Modelos](Modelos/) | Datos de la app, mensajes del motor y almacenamiento cifrado. |
| [Views](Views/) | Interfaz WPF y presentación. |
| [Services](Services/) | Motor, procesamiento de audio e integración con Windows. |
| [Diagnostics](Diagnostics/) | Pruebas de regresión disponibles al compilar con AUDIT_DIAGNOSTICS. Las pruebas con audio usan un archivo local llamado prueba-es.wav junto al informe. |

<details>
<summary>Compilar desde el código fuente</summary>

Instalá el SDK de .NET 8 y ejecutá el siguiente comando desde la raíz del repositorio.

~~~powershell
dotnet build TranscriptionMedia.sln -c Release
~~~

Descargá el modelo [Whisper large-v3 Q5_0](https://huggingface.co/ggerganov/whisper.cpp/blob/fb5ed9369c26eaf08392504d8446da431e0fffb9/ggml-large-v3-q5_0.bin) y guardalo en **bin/Release/net8.0-windows/Voice Model/**. Abrí **TranscriptionMedia.exe** desde **bin/Release/net8.0-windows/**.

</details>


## Licencia

El código de TranscriptionMedia se publica bajo la [licencia MIT](LICENSE). El modelo y las dependencias conservan sus licencias respectivas. El repositorio contiene el código fuente. El modelo, los ejecutables generados y el historial quedan fuera de Git.
