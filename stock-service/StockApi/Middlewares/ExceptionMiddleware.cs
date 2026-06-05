using System.Net;
using System.Text.Json;

namespace StockApi.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                // İstek boru hattında normal akışına devam etsin
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // 🚀 BÜYÜK SİHİR: Kodun neresinde hata patlarsa patlasın direkt buraya düşer!
                Console.WriteLine($"[KRİTİK HATA] Sistemde bir istisna oluştu: {ex.Message}");
                
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            // Kurumsal standart: Kullanıcıya 500 Internal Server Error dönüyoruz
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Güvenlik gereği iç detayları gizleyip, frontend'in anlayacağı temiz bir şablon sunuyoruz
            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Sistem arka planında beklenmeyen bir hata oluştu. Lütfen sistem yöneticinizle iletişime geçin.",
                Detailed = exception.Message // Geliştirme aşamasında hatayı görmek için buraya ekledik
            };

            var jsonResult = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResult);
        }
    }
}