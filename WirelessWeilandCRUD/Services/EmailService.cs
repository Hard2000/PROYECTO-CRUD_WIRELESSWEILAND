using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

public class EmailService
{
    private readonly string _smtpServer;
    private readonly int _port;
    private readonly string _fromEmail;
    private readonly string _password;

    // Constructor para inyectar EmailSettings
    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _smtpServer = emailSettings.Value.Host;
        _port = emailSettings.Value.Port > 0 ? emailSettings.Value.Port : 587; // Default 587
        _fromEmail = emailSettings.Value.From;
        _password = emailSettings.Value.Password;

        // Validación de campos obligatorios
        if (string.IsNullOrEmpty(_smtpServer) || string.IsNullOrEmpty(_fromEmail) || string.IsNullOrEmpty(_password))
        {
            throw new ArgumentException("Configuración de correo electrónico incompleta. Verifique appsettings.json.");
        }
    }

    // Método para enviar correos electrónicos
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpClient = new SmtpClient(_smtpServer)
        {
            Port = _port,
            Credentials = new NetworkCredential(_fromEmail, _password),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_fromEmail),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);
    }
}
