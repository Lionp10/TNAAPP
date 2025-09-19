using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TNA.BLL.DTOs
{
    public class AwsS3OptionsDTO
    {
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string? ServiceUrl { get; set; }
        public string? AccessKeyId { get; set; }
        public string? SecretAccessKey { get; set; }
    }
}
