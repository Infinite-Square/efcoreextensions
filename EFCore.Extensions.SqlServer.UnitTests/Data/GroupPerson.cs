using System;

namespace EFCore.Extensions.SqlServer.UnitTests.Data
{
    public class GroupPerson
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid PersonId { get; set; }

        public Group Group { get; set; }
        public Person Person { get; set; }

        public Guid TenantId { get; set; }
    }
}
