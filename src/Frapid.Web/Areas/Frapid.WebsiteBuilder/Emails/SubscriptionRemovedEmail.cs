﻿using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Frapid.Messaging;
using Frapid.Messaging.DTO;
using Frapid.WebsiteBuilder.ViewModels;

namespace Frapid.WebsiteBuilder.Emails
{
    public class SubscriptionRemovedEmail
    {
        private const string TemplatePath =
            "~/Tenants/{0}/Areas/Frapid.WebsiteBuilder/EmailTemplates/email-subscription-removed.html";

        private string GetMessage(string tenant, Subscribe model)
        {
            string siteUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
            string domain = HttpContext.Current.Request.Url.Host;

            string file = HostingEnvironment.MapPath(string.Format(CultureInfo.InvariantCulture, TemplatePath, tenant));

            if (file == null || !File.Exists(file))
            {
                return string.Empty;
            }

            string message = File.ReadAllText(file, Encoding.UTF8);

            message = message.Replace("{{Domain}}", domain);
            message = message.Replace("{{SiteUrl}}", siteUrl);
            message = message.Replace("{{Email}}", model.EmailAddress);

            return message;
        }

        private EmailQueue GetEmail(string tenant, Subscribe model)
        {
            var config = EmailProcessor.GetDefaultConfig(tenant);
            string domain = HttpContext.Current.Request.Url.Host;
            string subject = string.Format(CultureInfo.InvariantCulture, "You are now unsubscribed on {0}", domain);

            return new EmailQueue
            {
                AddedOn = DateTime.UtcNow,
                FromName = config.FromName,
                ReplyTo = config.FromEmail,
                Subject = subject,
                Message = this.GetMessage(tenant, model),
                SendTo = model.EmailAddress
            };
        }

        public async Task SendAsync(string tenant, Subscribe model)
        {
            try
            {
                var email = this.GetEmail(tenant, model);
                var manager = new MailQueueManager(tenant, email);
                manager.Add();

                var processor = EmailProcessor.GetDefault(tenant);

                if (string.IsNullOrWhiteSpace(email.ReplyTo))
                {
                    email.ReplyTo = processor.Config.FromEmail;
                }

                await manager.ProcessMailQueueAsync(processor);
            }
            catch
            {
                throw new HttpException(500, "Internal Server Error");
            }
        }
    }
}