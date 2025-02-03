using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IAuthenticationRepository
    {
        Task<(UserModel model, bool status, string message)> GetUserById(string userId);
        Task<(bool Status, string Message)> Login(LoginModel model);
        Task<(UserModel model, bool status, string message)> RegisterAdmin(UserModel model, string roleName);
        Task<(UserModel model, bool status, string message)> RegisterBusinessUser(UserModel model, string roleName);
    }
}
