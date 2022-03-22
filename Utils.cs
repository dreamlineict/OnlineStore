using iTextSharp.text;
using iTextSharp.text.pdf;
using OnlineStore.APIAuth;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Claims;

namespace OnlineStore
{
    public class Utils
    {
        private readonly string apiUrl = ConfigurationManager.AppSettings["WebAPIurl"].ToString();

        public Guid GetToken()
        {
            try
            {
                var identity = (ClaimsIdentity)ClaimsPrincipal.Current.Identity;
                var user = identity.FindFirst(ClaimTypes.NameIdentifier).Value;

                return new Guid(user.ToString());
            }
            catch (Exception)
            {
                return Guid.Empty;
            }
        }

        public string GetUserName()
        {
            try
            {
                var identity = (ClaimsIdentity)ClaimsPrincipal.Current.Identity;
                var user = identity.FindFirst(ClaimTypes.Email).Value;

                return user.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public void Log(string msg)
        {
            using (var client = new HttpClient())
            {
                JwtAuth jwtAuth = new JwtAuth();
                var jwtAuthToken = jwtAuth.JwtAuthToken().Result;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtAuthToken.Token);
                var httpContent = new HttpRequestMessage(HttpMethod.Post, @"" + apiUrl + "api/log?message=" + msg);
                var response = client.SendAsync(httpContent).Result;
            }
        }

        public bool ProcessEmail(Email emailData)
        {
            try
            {
                string smtpserver = ConfigurationManager.AppSettings["smtpserver"].ToString();

                MailMessage mMailMessage = new MailMessage
                {
                    From = new MailAddress(emailData.Sender)
                };
                mMailMessage.To.Add(new MailAddress(emailData.Recipient));
                mMailMessage.Subject = emailData.Subject;
                mMailMessage.Body = emailData.EmailBody;
                mMailMessage.IsBodyHtml = true;
                mMailMessage.Priority = MailPriority.Normal;

                var memStream = emailData.Attachment;
                memStream.Position = 0;
                var contentType = new ContentType(MediaTypeNames.Application.Pdf);
                var reportAttachment = new Attachment(memStream, contentType);
                reportAttachment.ContentDisposition.FileName = emailData.FileName + ".pdf";
                mMailMessage.Attachments.Add(reportAttachment);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using (SmtpClient client = new SmtpClient("smtp.mail.yahoo.com", 465))
                {
                    //client.UseDefaultCredentials = false;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.Credentials = new NetworkCredential("sandile.shongwe@yahoo.com", "Fashion@54321");
                    client.EnableSsl = true;
                    

                    client.Send(mMailMessage);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log(string.Concat("ProcessEmail ", ex.Message));
                return false;
            }
        }

        public PdfPCell newCellSpace()
        {
            PdfPCell cell = new PdfPCell(new Phrase(" "));
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.BorderWidthLeft = 1f;
            cell.BorderWidthRight = 1f;
            cell.BorderWidthTop = 1f;
            cell.BorderWidthBottom = 1f;
            cell.Colspan = 3;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            return cell;
        }

        public PdfPCell newCellHeader(string phrase)
        {
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);
            PdfPCell cell = new PdfPCell(new Phrase(phrase, boldFont));
            cell.BackgroundColor = BaseColor.LIGHT_GRAY;
            cell.BorderColor = BaseColor.LIGHT_GRAY;
            cell.BorderWidthLeft = 1f;
            cell.BorderWidthRight = 1f;
            cell.BorderWidthTop = 1f;
            cell.BorderWidthBottom = 1f;
            cell.Colspan = 3;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            return cell;
        }

        public PdfPCell newCellCol1(string col1)
        {
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);
            PdfPCell cell = new PdfPCell(new Phrase(col1, boldFont));
            cell.BorderColorLeft = BaseColor.WHITE;
            cell.BorderColorRight = BaseColor.WHITE;
            cell.BorderColorTop = BaseColor.WHITE;
            cell.BorderColorBottom = BaseColor.LIGHT_GRAY;
            cell.BorderWidthLeft = 1f;
            cell.BorderWidthRight = 1f;
            cell.BorderWidthTop = 1f;
            cell.BorderWidthBottom = 1f;
            cell.HorizontalAlignment = Element.ALIGN_LEFT;
            return cell;
        }

        public PdfPCell newCellCol2(string col2)
        {
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
            PdfPCell cell = new PdfPCell(new Phrase(col2, boldFont));
            cell.BorderColorLeft = BaseColor.WHITE;
            cell.BorderColorRight = BaseColor.WHITE;
            cell.BorderColorTop = BaseColor.WHITE;
            cell.BorderColorBottom = BaseColor.LIGHT_GRAY;
            cell.BorderWidthLeft = 1f;
            cell.BorderWidthRight = 1f;
            cell.BorderWidthTop = 1f;
            cell.BorderWidthBottom = 1f;
            cell.HorizontalAlignment = Element.ALIGN_LEFT;
            cell.Colspan = 2;
            return cell;
        }
    }

    public class Email
    {
        public string Recipient { get; set; }
        public MemoryStream Attachment { get; set; }
        public string EmailBody { get; set; }
        public string Subject { get; set; }

        public string Sender { get; set; }

        public string FileName { get; set; }
    }
}