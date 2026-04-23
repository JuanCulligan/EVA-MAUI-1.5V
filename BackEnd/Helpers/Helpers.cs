using Core.Entities.Models;
using Core.Entities.Request;
using Core.Enums;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public static class Helpers
    {

        public static void LogActions(ReqLogs req)
        {
            try
            {
                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    conexion.SP_InsertarBitacora(
                        req.log.Class,
                        req.log.Method,
                        (short?)req.log.type,
                        req.log.ErrorID,
                        req.log.Description,
                        req.log.Request,
                        req.log.Response
                    );
                }
            }
            catch (Exception ex)
            {
                //string logFolder = AppDomain.CurrentDomain.BaseDirectory + "Logs";

                //if (!System.IO.Directory.Exists(logFolder))
                //    System.IO.Directory.CreateDirectory(logFolder);

                //string logFile = System.IO.Path.Combine(logFolder, $"Log_{DateTime.Now:yyyy-MM-dd}.txt");

                //string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                //                  $"Class: {req.log.Class} | " +
                //                  $"Method: {req.log.Method} | " +
                //                  $"Type: {req.log.type} | " +
                //                  $"ErrorID: {req.log.ErrorID} | " +
                //                  $"Description: {req.log.Description}" +
                //                  $"\nRequest: {req.log.Request}" +
                //                  $"\nResponse: {req.log.Response}" +
                //                  $"\n{new string('-', 80)}\n";

                //System.IO.File.AppendAllText(logFile, logEntry);
                //Falla en azure
            }
        }
        public static Error CreateError(EnumErrors enumErrors)
        {
            Error error = new Error();
            error.code = enumErrors;
            error.message = enumErrors.ToString();
            return error;
        }

        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            return System.Text.RegularExpressions.Regex.IsMatch(password,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$");
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return email.Contains("@") &&
                   email.LastIndexOf("@") == email.IndexOf("@") &&
                   email.Contains(".") &&
                   email.IndexOf("@") < email.LastIndexOf(".");
        }

        public static string createToken()
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, 6)
                .Select(_ => caracteres[random.Next(caracteres.Length)])
                .ToArray());
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string plainText, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(plainText, hashedPassword);
        }


        //La fórmula de Haversine
        public static double GetDistanceMeters(RoutePoint p1, RoutePoint p2)
        {
            var R = 6371e3;

            var phi1 = p1.Latitude * Math.PI / 180;
            var phi2 = p2.Latitude * Math.PI / 180;

            var dPhi = (p2.Latitude - p1.Latitude) * Math.PI / 180;
            var dLambda = (p2.Longitude - p1.Longitude) * Math.PI / 180;

            var a =
                Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        public static bool SendVerificationEmail(string Name, string email, string token)
        {

            string senderEmail = Environment.GetEnvironmentVariable("email_sender");
            string appPassword = Environment.GetEnvironmentVariable("email_password");

            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, "EVA ACTIVATE ACCOUNT"),
                    Subject = "Verify your account",
                    IsBodyHtml = true,
                    Body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
    <div style='max-width: 500px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
        <h2 style='color: #333; text-align: center;'>Welcome!</h2>
        <p style='color: #555; font-size: 16px;'>Hi <strong>{Name}</strong>,</p>
        <p style='color: #555; font-size: 16px;'>Thank you for signing up. To activate your account, use the following verification code:</p>
        <div style='background: #007bff; color: white; padding: 15px; text-align: center; font-size: 24px; letter-spacing: 5px; border-radius: 5px; margin: 20px 0;'>
            {token}
        </div>
        <p style='color: #888; font-size: 14px; text-align: center;'>This code expires in 24 hours.</p>
        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='color: #aaa; font-size: 12px; text-align: center;'>If you did not request this verification, please ignore this email.</p>
    </div>
</body>
</html>"
                };
                message.To.Add(email);
                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(senderEmail, appPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(message);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
