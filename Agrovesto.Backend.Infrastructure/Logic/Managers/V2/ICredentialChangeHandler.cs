using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface ICredentialChangeHandler
    {
        CredentialType Type { get; }
        string RequiredFieldMissingMessage { get; } // e.g. "Your current password is required."

        Task<User> FindUserAsync(ChangeCredentialModel model);
        void ValidateNewValueFormat(string newValue); // throws BaseException if invalid
        Task ChangeAsync(User user, string oldValue, string newValue); // verifies old, applies new, persists — throws BaseException on wrong old value


        Task<ResetArtifact> GenerateArtifactAsync(User user, ForgotCredentialModel model);
        Task DeliverLiveAsync(User user, ResetArtifact artifact, ForgotCredentialModel model);
        string BuildNonLiveMessage(ResetArtifact artifact);

        Task ConfirmAsync(User user, ConfirmCredentialResetModel model); // verifies proof, applies new value, persists, throws on failure

    }


}
