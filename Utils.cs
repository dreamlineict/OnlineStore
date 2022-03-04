﻿using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Claims;

namespace OnlineStore
{
    public class Utils
    {
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

        public bool ProcessEmailAsync(Email emailData)
        {
            try
            {
                string smtpserver = ConfigurationManager.AppSettings["smtpserver"].ToString();

                SmtpClient Smtp = new SmtpClient(smtpserver);

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

                Smtp.Send(mMailMessage);
                return true;
            }
            catch (Exception ex)
            {
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