using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class UserDevice
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string IPAddress { get; set; }
        public DateTimeOffset LastSeen { get; set; }
    }

}
