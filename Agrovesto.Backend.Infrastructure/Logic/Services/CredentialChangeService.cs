using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public class CredentialChangeService
    {
        private readonly IEnumerable<ICredentialChangeHandler> _handlers;
        private readonly IConfiguration _configuration;
        public CredentialChangeService(IEnumerable<ICredentialChangeHandler> handlers,
            IConfiguration configuration)
        {
            _handlers = handlers;

            _configuration = configuration;
        }

        public async Task<(bool Status, string Message)> ChangeCredential(ChangeCredentialModel model)
        {
            var handler = _handlers.SingleOrDefault(h => h.Type == model.Type)
                ?? throw new BaseException(ExceptionType.BAD_REQUEST, "Unsupported credential type.");

            if (string.IsNullOrWhiteSpace(model.OldValue))
                throw new BaseException(ExceptionType.BAD_REQUEST, handler.RequiredFieldMissingMessage);

            if (string.IsNullOrWhiteSpace(model.NewValue))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Your new value is required.");

            if (!string.Equals(model.NewValue, model.ConfirmValue, StringComparison.Ordinal))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Confirmation must match the new value.");

            if (string.Equals(model.OldValue, model.NewValue, StringComparison.Ordinal))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Your new value must be different from the current one.");

            handler.ValidateNewValueFormat(model.NewValue);

            var user = await handler.FindUserAsync(model);

            await handler.ChangeAsync(user, model.OldValue, model.NewValue);

            return (true, $"{(model.Type == CredentialType.Password ? "Password" : "Pin")} changed successfully");
        }

        public async Task<(bool Status, string Message)> ForgotCredential(ForgotCredentialModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Env))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Environment is required.");

            var handler = _handlers.SingleOrDefault(h => h.Type == model.Type)
                ?? throw new BaseException(ExceptionType.BAD_REQUEST, "Unsupported credential type.");

            var user = await handler.FindUserAsync(new ChangeCredentialModel
            {
                Email=model.Email,
                PhoneNumber=model.PhoneNumber,
            });
            var artifact = await handler.GenerateArtifactAsync(user, model);

            var env = _configuration["Official:Env"];
            if (env == Env.Live.ToString())
            {
                await handler.DeliverLiveAsync(user, artifact, model);
                return (true, "A reset code has been sent");
            }


             return (true, handler.BuildNonLiveMessage(artifact));
        }

        public async Task<(bool Status, string Message)> ConfirmReset(ConfirmCredentialResetModel model)
        {
            var handler = _handlers.SingleOrDefault(h => h.Type == model.Type)
                ?? throw new BaseException(ExceptionType.BAD_REQUEST, "Unsupported credential type.");

            if (string.IsNullOrWhiteSpace(model.NewValue))
                throw new BaseException(ExceptionType.BAD_REQUEST, "A new value is required.");

            if (!string.Equals(model.NewValue, model.ConfirmValue, StringComparison.Ordinal))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Confirmation must match the new value.");

            handler.ValidateNewValueFormat(model.NewValue);

            var user = await handler.FindUserAsync(new ChangeCredentialModel
            {
                Email = model.UserId,
                PhoneNumber = model.PhoneNumber,
            });
            await handler.ConfirmAsync(user, model);

            return (true, $"{(model.Type == CredentialType.Password ? "Password" : "Pin")} reset successful");
        }
    }

}
