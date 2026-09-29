using System.Collections.Generic;

namespace Light.Infrastructure
{
    public class GraphOptions
    {
        /// <summary>Azure AD (Entra ID) tenant id. Required.</summary>
        public string? TenantId { get; set; }

        /// <summary>App registration (client) id. Required.</summary>
        public string? ClientId { get; set; }

        /// <summary>App registration client secret. Required.</summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Optional allow-list of mailbox addresses that <see cref="Graph.IGraphMailService.SendAsync"/>
        /// may send as (the <c>from</c> argument), compared case-insensitively.
        /// <see langword="null"/> or empty (the default) means no restriction is applied by this package.
        /// </summary>
        /// <remarks>
        /// This is a defence-in-depth check only. With app-only <c>Mail.Send</c> the application can send
        /// as <b>any</b> mailbox in the tenant; restrict it server-side with an Exchange Online
        /// <c>ApplicationAccessPolicy</c> (or RBAC for Applications).
        /// </remarks>
        public IList<string>? AllowedSenders { get; set; }
    }
}
