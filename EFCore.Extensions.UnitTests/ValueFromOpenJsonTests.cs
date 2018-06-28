using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using Xunit;

namespace EFCore.Extensions.UnitTests
{
    public class Entity
    {
        public bool P1 { get; set; }
        public int P2 { get; set; }
        public string P3 { get; set; }

        public bool? P4 { get; set; }
        public int? P5 { get; set; }
        public string P6 { get; set; }

        public bool[] P7 { get; set; }
        public int[] P8 { get; set; }
        public string[] P9 { get; set; }

        //public Entity Parent { get; set; }
        public Entity[] Children { get; set; }

        private static Random _random = new Random();
        public static Entity Create(int childrenDepth = 0)
        {
            var result = new Entity
            {
                P1 = Convert.ToBoolean(_random.Next(0, 1)),
                P2 = _random.Next(),
                P3 = Guid.NewGuid().ToString(),
                P4 = null,
                P5 = null,
                P6 = null,
                P7 = new[] { true, false, true, false },
                P8 = new[] { _random.Next(), _random.Next(), _random.Next(), _random.Next() },
                P9 = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
                Children = childrenDepth > 0
                    ? Enumerable.Range(1, _random.Next(1, 4)).Select(_ => Create(childrenDepth - 1)).ToArray()
                    : null
            };
            return result;
        }
    }

    public class ValueFromOpenJsonTests
    {
        private static Entity _entity = Entity.Create(3);
        private static string _entityJson = JsonConvert.SerializeObject(_entity);

        private static Entity[] _entities = new[] { Entity.Create(3), Entity.Create(3), Entity.Create(3) };
        private static string _entitiesJson = JsonConvert.SerializeObject(_entities);

        [Fact]
        public void ParseArrayOfEntities()
        {
            var result = ExtensionsDbFunctionsExtensions.ValueFromOpenJson<string>(null, _entitiesJson, "$.P3").ToList();
            Assert.Equal(3, result.Count());
            Assert.Equal(_entities[0].P3, result[0].Value);
            Assert.Equal(_entities[1].P3, result[1].Value);
            Assert.Equal(_entities[2].P3, result[2].Value);
        }

        [Fact]
        public void ParseArrayOfStrings()
        {

        }
    }
}
