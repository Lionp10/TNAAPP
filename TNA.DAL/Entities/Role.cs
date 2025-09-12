using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TNA.DAL.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public required string Description { get; set; }
    }
}
