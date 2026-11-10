# TranscriptionMedia

Aplicación de escritorio para Windows que convierte audio en español a texto. Podés grabar desde el micrófono o transcribir un archivo. El procesamiento y el historial quedan en tu computadora.

## Requisitos

- Windows 11 de 64 bits y SDK de .NET 8 para compilar. Si usás Visual Studio, instalá la carga de trabajo **Desarrollo de escritorio de .NET**.
- Se recomiendan 8 GB de RAM. Windows 10 y equipos con 4 GB quedan pendientes de validación.
- Modelo [Whisper large-v3 Q5_0](https://huggingface.co/ggerganov/whisper.cpp/blob/fb5ed9369c26eaf08392504d8446da431e0fffb9/ggml-large-v3-q5_0.bin), de aproximadamente 1,1 GB. Descargalo una vez. Luego la transcripción funciona sin conexión.

## Compilar y ejecutar

1. Desde la raíz del repositorio, ejecutá el siguiente comando o abrí [TranscriptionMedia.sln](TranscriptionMedia.sln) en Visual Studio.

   ~~~powershell
   dotnet build TranscriptionMedia.sln -c Release
   ~~~

2. Creá la carpeta **Voice Model** dentro de **bin/Release/net8.0-windows/** y guardá ahí el archivo **ggml-large-v3-q5_0.bin**.
3. Abrí **TranscriptionMedia.exe** desde **bin/Release/net8.0-windows/**. Si compilaste en Debug, usá **bin/Debug/net8.0-windows/**.

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

## Licencia

El código de TranscriptionMedia se publica bajo la [licencia MIT](LICENSE). El modelo y las dependencias conservan sus licencias respectivas. El repositorio contiene el código fuente. El modelo, los ejecutables generados y el historial quedan fuera de Git.
