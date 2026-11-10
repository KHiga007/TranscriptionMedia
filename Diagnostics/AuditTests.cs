using System.IO;
using System.Text;
using System.Windows;


namespace TranscriptionMedia;
public partial class MainWindow
{
    public async Task AuditTest(string path)
    {
        var report=new StringBuilder();
        try{
            if(!controller.HistoryAvailable)throw new Exception("No se abrió el historial aislado de pruebas.");
            var packets=new List<short[]>();int sample=0;
            for(int i=0;i<130;i++){var packet=new short[16000];for(int j=0;j<packet.Length;j++)packet[j]=(short)((sample++%4000)-2000);packets.Add(packet);}
            long consumed=0;foreach(var chunk in RecordingChunks.Split(packets)){if(chunk.Length>16000*29)throw new Exception("Fragmento demasiado grande");foreach(var value in chunk){short original=packets[(int)(consumed/16000)][(int)(consumed%16000)];if(value!=original/32768f)throw new Exception("Se alteraron o perdieron muestras");consumed++;}}
            if(consumed!=130L*16000)throw new Exception("Se perdieron muestras al fragmentar");report.AppendLine("PASS: grabación de 130 s fragmentada sin pérdida, manteniendo una sola ventana float de 29 s.");
            recording=true;SetBusy(true);cancellation=new CancellationTokenSource();cancellation.Cancel();var cancelledPacket=new short[16000];cancelledPacket[0]=1000;recorded.Add(cancelledPacket);recordedSamples=cancelledPacket.Length;
            Stop(this,new RoutedEventArgs());await Task.Delay(150);if(recording||busy||cancellation!=null||recorded.Count!=0)throw new Exception("Detener/cancelar dejó la interfaz ocupada");report.AppendLine("PASS: detención/cancelación libera grabación, memoria y controles sin un micrófono físico.");
            using(var cancelled=new EngineClient()){var cts=new CancellationTokenSource();cts.Cancel();try{await cancelled.Run(new EngineRequest(null,Convert.ToBase64String(new byte[6400])),null,cts.Token);throw new Exception("Cancelación ignorada");}catch(OperationCanceledException){}report.AppendLine("PASS: solicitud previamente cancelada no inicia procesamiento.");}
            var id=Save("Auditoría","Texto confidencial de prueba áéíóú.");Refresh();var entry=entries.Single(e=>e.Id==id);OpenEntry(entry);if(transcript.Text!=entry.Text)throw new Exception("No se recuperó el historial");
            report.AppendLine("PASS: SQLCipher "+controller.ReadCipherVersion()+", lectura/escritura del historial y apertura de entrada.");
            controller.DeleteHistory(false, id);
            var invalid=Path.Combine(Path.GetDirectoryName(path)!,"archivo-invalido.ogg");File.WriteAllText(invalid,"No es un contenedor OGG.");int resets=0;
            try{await controller.TranscribeFile(invalid,part=>{if(part=="\0RESET")resets++;},CancellationToken.None);throw new Exception("Se aceptó un archivo inválido");}catch(RequestRejectedException){}
            if(resets!=0)throw new Exception("Se reintentó un archivo inválido");report.AppendLine("PASS: archivo OGG inválido rechazado sin reintentos de GPU/CPU.");
            var audio = Path.Combine(Path.GetDirectoryName(path)!, "prueba-es.wav");
            if(File.Exists(audio)){
                suppressCompletionAlert=true;ShowUpload();SelectAudio(audio);ShowWorkspace(false,Path.GetFileName(audio));await TranscribeSelected(false);
                var result=transcript.Text;if(string.IsNullOrWhiteSpace(result)||progressBar.Value!=100)throw new Exception("No se completó el flujo de transcripción");
                await Task.Delay(1200);if(transcript.Text!=result)throw new Exception("Desapareció el texto");report.AppendLine($"PASS: archivo transcrito por {controller.Backend}, {result.Length} caracteres; texto persistente, progreso y guardado.");
                using var cpu=new EngineClient(true);var shortAudio=Path.Combine(Path.GetDirectoryName(path)!,"prueba-es.wav");if(File.Exists(shortAudio)){var cpuText=await cpu.Run(new EngineRequest(shortAudio,null),null,CancellationToken.None);if(cpuText.Length==0)throw new Exception("CPU devolvió texto vacío");report.AppendLine("PASS: transcripción por CPU.");}
            }else report.AppendLine("PENDIENTE: no se encontró audio de referencia.");
            ShowHome();ValidateCompactUi(path);report.AppendLine("PASS: navegación y diseño de la versión instalada conservados.");
            report.AppendLine("PASS FINAL");
        }catch(Exception ex){report.AppendLine("FAIL: "+ex);}
        finally{File.WriteAllText(path,report.ToString());controller.Dispose();Application.Current.Shutdown();}
    }
}
