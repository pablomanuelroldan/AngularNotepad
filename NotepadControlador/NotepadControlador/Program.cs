using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NotepadControlador;

var builder = WebApplication.CreateBuilder(args);

// Configurar Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=PABLO-DESKTOP\\SQLSERVER2022;Database=NotepadControlador;User Id=sa;Password=12345678;TrustServerCertificate=True"));

var app = builder.Build();

// Inicializa el WindowManager
var windowManager = new WindowManager();

// Recuperar estados de las ventanas al iniciar
var windowStates = new List<WindowState>();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    windowStates = dbContext.Windows.ToList();
}

// Inicia las ventanas con el estado restaurado
foreach (var state in windowStates)
{
    windowManager.StartWindow(state.Id); // Inicia Notepad.exe si no está ya abierto
    windowManager.UpdateWindow(state.Id, state.X, state.Y, state.Width, state.Height); // Actualiza posición y tamaño
}

// Middleware WebSocket
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("Conexión WebSocket establecida");

        try
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("Mensaje recibido del cliente: " + message);

                // Manejar mensajes
                try
                {
                    var command = JsonSerializer.Deserialize<dynamic>(message);

                    // Manejo de inicialización
                    if (command.action == "initialize")
                    {
                        var response = new
                        {
                            action = "initialize",
                            windows = windowStates
                        };

                        await webSocket.SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response))),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }

                    // Manejo de mover o redimensionar
                    if (command.action == "move" || command.action == "resize")
                    {
                        if (!windowManager.IsOverlapping((int)command.windowId, (int)command.x, (int)command.y, (int)command.width, (int)command.height))
                        {
                            windowManager.UpdateWindow((int)command.windowId, (int)command.x, (int)command.y, (int)command.width, (int)command.height);

                            // Guardar el estado en la base de datos
                            using (var scope = app.Services.CreateScope())
                            {
                                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                                var windowId = (int)command.windowId;

                                var state = dbContext.Windows.FirstOrDefault(w => w.Id == windowId)
                                    ?? new WindowState { Id = windowId };
                                state.X = (int)command.x;
                                state.Y = (int)command.y;
                                state.Width = (int)command.width;
                                state.Height = (int)command.height;

                                dbContext.Update(state);
                                dbContext.SaveChanges();
                            }
                        }
                    }

                    // Manejo de cierre
                    if (command.action == "close")
                    {
                        windowManager.CloseWindow((int)command.windowId);

                        // Enviar confirmación al cliente
                        var response = new { action = "closed", windowId = (int)command.windowId };
                        await webSocket.SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response))),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al procesar el mensaje: " + ex.Message);
                }

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            Console.WriteLine("Conexión WebSocket cerrada");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error en el WebSocket: " + ex.Message);
        }
    }
    else
    {
        await next();
    }
});

// Ejecutar la aplicación
app.Run();
