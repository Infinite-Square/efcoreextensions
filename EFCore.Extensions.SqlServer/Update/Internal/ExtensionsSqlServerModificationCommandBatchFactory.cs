using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Update.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace EFCore.Extensions.SqlServer.Update.Internal
{
    public interface ISqlServerModificationCommandBatchEvents
    {
        void AddingCommand(ModificationCommand command, ModificationCommandBatch batch, ISqlServerModificationCommandBatchAppender appender);
        void AddedCommand(ModificationCommand command, bool added, ModificationCommandBatch batch, ISqlServerModificationCommandBatchAppender appender);
    }

    public interface ISqlServerModificationCommandBatchAppender
    {
        void Append(ModificationCommand command);
    }

    public class ExtensionsSqlServerModificationCommandBatchFactory : SqlServerModificationCommandBatchFactory
    {
        private readonly IRelationalCommandBuilderFactory _commandBuilderFactory;
        private readonly ISqlGenerationHelper _sqlGenerationHelper;
        private readonly ISqlServerUpdateSqlGenerator _updateSqlGenerator;
        private readonly IRelationalValueBufferFactoryFactory _valueBufferFactoryFactory;
        private readonly IDbContextOptions _options;
        private readonly ISqlServerModificationCommandBatchEvents _events;

        public ExtensionsSqlServerModificationCommandBatchFactory(IRelationalCommandBuilderFactory commandBuilderFactory
            , ISqlGenerationHelper sqlGenerationHelper
            , ISqlServerUpdateSqlGenerator updateSqlGenerator
            , IRelationalValueBufferFactoryFactory valueBufferFactoryFactory
            , IDbContextOptions options
            , ISqlServerModificationCommandBatchEvents events = null)
            : base(commandBuilderFactory, sqlGenerationHelper, updateSqlGenerator, valueBufferFactoryFactory, options)
        {
            _commandBuilderFactory = commandBuilderFactory;
            _sqlGenerationHelper = sqlGenerationHelper;
            _updateSqlGenerator = updateSqlGenerator;
            _valueBufferFactoryFactory = valueBufferFactoryFactory;
            _options = options;
            _events = events;
        }

        public override ModificationCommandBatch Create()
        {
            var optionsExtension = _options.Extensions.OfType<SqlServerOptionsExtension>().FirstOrDefault();

            return new ExtensionsSqlServerModificationCommandBatch(_commandBuilderFactory
                , _sqlGenerationHelper
                , _updateSqlGenerator
                , _valueBufferFactoryFactory
                , optionsExtension?.MaxBatchSize
                , _events);
        }

        private class ExtensionsSqlServerModificationCommandBatch : SqlServerModificationCommandBatch
            , ISqlServerModificationCommandBatchAppender
        {
            private readonly ISqlServerModificationCommandBatchEvents _events;
            private StringBuilder _cachedCommandText;

            public ExtensionsSqlServerModificationCommandBatch(IRelationalCommandBuilderFactory commandBuilderFactory
                , ISqlGenerationHelper sqlGenerationHelper
                , ISqlServerUpdateSqlGenerator updateSqlGenerator
                , IRelationalValueBufferFactoryFactory valueBufferFactoryFactory
                , int? maxBatchSize
                , ISqlServerModificationCommandBatchEvents events)
                : base(commandBuilderFactory, sqlGenerationHelper, updateSqlGenerator, valueBufferFactoryFactory, maxBatchSize)
            {
                _events = events;
            }

            public override bool AddCommand(ModificationCommand modificationCommand)
            {
                _events?.AddingCommand(modificationCommand, this, this);
                var added = base.AddCommand(modificationCommand);
                if (_cachedCommandText != null)
                    CachedCommandText = _cachedCommandText.Append(CachedCommandText);
                _events?.AddedCommand(modificationCommand, added, this, this);
                return added;
            }

            public void Append(ModificationCommand command)
            {
                switch (command.EntityState)
                {
                    case Microsoft.EntityFrameworkCore.EntityState.Added:
                        UpdateSqlGenerator.AppendInsertOperation(CachedCommandText ?? (_cachedCommandText = new StringBuilder()), command, 0);
                        break;
                    case Microsoft.EntityFrameworkCore.EntityState.Modified:
                        UpdateSqlGenerator.AppendUpdateOperation(CachedCommandText ?? (_cachedCommandText = new StringBuilder()), command, 0);
                        break;
                    case Microsoft.EntityFrameworkCore.EntityState.Deleted:
                        UpdateSqlGenerator.AppendDeleteOperation(CachedCommandText ?? (_cachedCommandText = new StringBuilder()), command, 0);
                        break;
                }
            }
        }
    }
}
