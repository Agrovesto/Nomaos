using Agrovesto.Backend.Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class KYCRepository //: IKYCRepository
    {
        private readonly DataContext _context;

        public KYCRepository(DataContext context)
        {
            _context = context;
        }

       // public async
    }
}
