using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class FileDownloadModel
    {
        public byte[] FileBytes { get; set; }

        public string MimeType { get; set; }

        public string FileName { get; set; }
    }
}
