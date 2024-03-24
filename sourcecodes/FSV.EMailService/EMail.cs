// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
// Copyright (C) 2015 - 2024  Carsten Schäfer, Matthias Friedrich, and Ritesh Gite
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace FSV.EMailService
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Mail;
    using System.Text;

    internal class EMail
    {
        public string SenderAddress { get; set; }
        public string SenderName { get; set; }
        public List<string> Recipients { get; set; }
        public string ExchangeServer { get; set; }
        public int ExchangePort { get; set; }
        public string EMailSubject { get; set; }
        public string Body { get; set; }
        public bool UseSsl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public UsedEncoding EMailEncoding { get; set; }
        public bool IsBodyHtml { get; set; }

        public void Send()
        {
            //If the UseDefaultCredentials property is set to true, SmtpClient will use the credentials provided to the  DefaultCredentials property in the CredentialCache class.
            //If the UseDefaultCredentials property is set to false, then the value set in the Credentials property will be used for the credentials when connecting to the server. If the UseDefaultCredentials property is set to false and the Credentials property has not been set, then mail is sent to the server anonymously.
            //The setter for UseDefaultCredentials sets the Credentials property to null if you set it to false, or it sets it to the CredentialCache.DefaultNetworkCredentials property if set to true. 

            var client = new SmtpClient();
            client.UseDefaultCredentials = false; //always false!

            if (string.IsNullOrEmpty(this.Username) && string.IsNullOrEmpty(this.Password))
            {
                client.Credentials = new NetworkCredential(this.Username, this.Password);
            }

            client.Port = this.ExchangePort;
            client.Host = this.ExchangeServer;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.EnableSsl = this.UseSsl;

            var msg = new MailMessage();
            msg.From = new MailAddress(this.SenderAddress, this.SenderName);
            msg.Subject = this.EMailSubject;
            msg.IsBodyHtml = this.IsBodyHtml;
            msg.Body = this.Body.Replace("\r\n", "<br>");
            msg.SubjectEncoding = this.ConvertEncodingEnum(this.EMailEncoding);
            msg.BodyEncoding = this.ConvertEncodingEnum(this.EMailEncoding);

            foreach (string recipient in this.Recipients)
            {
                msg.To.Add(recipient);
            }

            //System.Net.Mime.ContentType contentType = new System.Net.Mime.ContentType();
            //contentType.MediaType = System.Net.Mime.MediaTypeNames.Application.Octet;
            //contentType.Name = "test.docx";
            //msg.Attachments.Add(new Attachment("I:/files/test.docx", contentType));

            //or as memory stream
            //MemoryStream stream = new MemoryStream(new byte[64000]);
            //Attachment attachment = new Attachment(stream, "my attachment");
            //msg.Attachments.Add(attachment);

            client.Send(msg);
        }

        private Encoding ConvertEncodingEnum(UsedEncoding enc)
        {
            switch (enc)
            {
                case UsedEncoding.Ascii:
                    return Encoding.ASCII;

                case UsedEncoding.Default:
                    return Encoding.Default;

                case UsedEncoding.Unicode:
                    return Encoding.Unicode;

                case UsedEncoding.Utf32:
                    return Encoding.UTF32;

                case UsedEncoding.Utf8:
                    return Encoding.UTF8;

                default:
                    return Encoding.Default;
            }
        }
    }
}