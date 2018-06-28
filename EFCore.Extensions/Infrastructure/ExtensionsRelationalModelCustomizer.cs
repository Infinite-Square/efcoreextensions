using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EFCore.Extensions.Infrastructure
{
    public class ExtensionsRelationalModelCustomizer : RelationalModelCustomizer
    {
        public ExtensionsRelationalModelCustomizer(ModelCustomizerDependencies dependencies) : base(dependencies)
        {
        }

        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            modelBuilder.Entity<JsonResult<string>>();
            modelBuilder.Entity<JsonResult<int>>();
            modelBuilder.Entity<JsonResult<bool>>();
        }
    }
}
