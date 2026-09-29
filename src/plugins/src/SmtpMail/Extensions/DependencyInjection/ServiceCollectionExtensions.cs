using Light.Smtp;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Light.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="ISmtpMailSender"/> backed by <see cref="SmtpMailSender"/> (the built-in
        /// <see cref="System.Net.Mail.SmtpClient"/>, no authentication).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentException">The configured <see cref="SmtpMailOptions.Host"/> is empty or <see cref="SmtpMailOptions.Port"/> is out of range.</exception>
        public static IServiceCollection AddSmtpMail(this IServiceCollection services, Action<SmtpMailOptions> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            var options = new SmtpMailOptions();
            action.Invoke(options);

            Validate(options.Host, options.Port, nameof(AddSmtpMail));

            services.AddTransient<ISmtpMailSender>(sp => new SmtpMailSender(options.Host, options.Port)
            {
                UseSsl = options.UseSsl
            });

            return services;
        }

        /// <summary>
        /// Registers <see cref="ISmtpMailSender"/> backed by <see cref="SmtpMailKitSender"/> (MailKit, with authentication).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentException">The configured <see cref="SmtpMailKitOptions.Host"/> is empty or <see cref="SmtpMailKitOptions.Port"/> is out of range.</exception>
        public static IServiceCollection AddSmtpMailKit(this IServiceCollection services, Action<SmtpMailKitOptions> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            var options = new SmtpMailKitOptions();
            action.Invoke(options);

            Validate(options.Host, options.Port, nameof(AddSmtpMailKit));

            services.AddTransient<ISmtpMailSender>(sp =>
                new SmtpMailKitSender(options.Host, options.UserName, options.Password, options.Port)
                {
                    UseSsl = options.UseSsl,
                    SecureSocketOptions = options.SecureSocketOptions,
                });

            return services;
        }

        private static void Validate(string? host, int port, string method)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException($"{method}: SMTP Host must be configured.", "action");

            if (port <= 0 || port > 65535)
                throw new ArgumentException($"{method}: SMTP Port must be between 1 and 65535 (was {port}).", "action");
        }
    }
}
