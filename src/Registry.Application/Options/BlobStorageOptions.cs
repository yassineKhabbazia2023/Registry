using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Options
{
    public class BlobStorageOptions
    {
        public string ContainerName { get; set; } = default!;

        public string BlobUri { get; set; } = default!;
    }
}
