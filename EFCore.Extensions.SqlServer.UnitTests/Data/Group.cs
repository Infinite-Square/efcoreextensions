using System;
using System.Collections.Generic;

namespace EFCore.Extensions.SqlServer.UnitTests.Data
{
    public class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public ICollection<GroupPerson> Persons { get; set; }

        public Guid TenantId { get; set; }
    }
}
