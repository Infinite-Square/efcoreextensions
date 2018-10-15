using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.Extensions.SqlServer.UnitTests.Data
{
    public class Person
    {
        public Guid Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        /// <summary>
        /// JSON array of strings ["k1", "k2"]
        /// </summary>
        public string Kinds { get; set; }
    }
}
