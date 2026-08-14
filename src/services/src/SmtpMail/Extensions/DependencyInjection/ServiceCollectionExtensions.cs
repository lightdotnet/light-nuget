using Light.Smtp;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Light.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="ISmtpMailSender"/> backed by <see cref="SmtpNetMailSender"/> (the built-in
        /// <see cref="System.Net.Mail.SmtpClient"/>, no authentication).
        /// </summary>
        public static IServiceCollection AddSmtpMail(this IServiceCollection services, Action<SmtpMailOptions> action)
        {
            var options = new SmtpMailOptions();
            action.Invoke(options);

            services.AddTransient<ISmtpMailSender>(sp => new SmtpNetMailSender(options.Host, options.Port)
            {
                UseSsl = options.UseSsl
            });

            return services;
        }

        /// <summary>
        /// Registers <see cref="ISmtpMailSender"/> backed by <see cref="SmtpMailKitSender"/> (MailKit, with authentication).
        /// </summary>
        public static IServiceCollection AddSmtpMailKit(this IServiceCollection services, Action<SmtpMailKitOptions> action)
        {
            var options = new SmtpMailKitOptions();
            action.Invoke(options);

            services.AddTransient<ISmtpMailSender>(sp =>
                new SmtpMailKitSender(options.Host, options.UserName, options.Password, options.Port)
                {
                    UseSsl = options.UseSsl
                });

            return services;
        }
    }
}
