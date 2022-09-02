using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Update.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        void Prepend(ModificationCommand command);
        void Before(ModificationCommand related, ModificationCommand command);
        void After(ModificationCommand related, ModificationCommand command);
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
            private readonly IRelationalCommandBuilderFactory _commandBuilderFactory;
            private readonly ISqlGenerationHelper _sqlGenerationHelper;
            private readonly ISqlServerUpdateSqlGenerator _updateSqlGenerator;
            private readonly IRelationalValueBufferFactoryFactory _valueBufferFactoryFactory;
            private readonly int? _maxBatchSize;
            //private StringBuilder _cachedCommandText;

            private readonly List<ModificationCommand> _appends = new List<ModificationCommand>();
            private readonly List<ModificationCommand> _prepends = new List<ModificationCommand>();
            private readonly Dictionary<ModificationCommand, List<ModificationCommand>> _befores = new Dictionary<ModificationCommand, List<ModificationCommand>>();
            private readonly Dictionary<ModificationCommand, List<ModificationCommand>> _afters = new Dictionary<ModificationCommand, List<ModificationCommand>>();

            public ExtensionsSqlServerModificationCommandBatch(IRelationalCommandBuilderFactory commandBuilderFactory
                , ISqlGenerationHelper sqlGenerationHelper
                , ISqlServerUpdateSqlGenerator updateSqlGenerator
                , IRelationalValueBufferFactoryFactory valueBufferFactoryFactory
                , int? maxBatchSize
                , ISqlServerModificationCommandBatchEvents events)
                : base(commandBuilderFactory, sqlGenerationHelper, updateSqlGenerator, valueBufferFactoryFactory, maxBatchSize)
            {
                _events = events;
                _commandBuilderFactory = commandBuilderFactory;
                _sqlGenerationHelper = sqlGenerationHelper;
                _updateSqlGenerator = updateSqlGenerator;
                _valueBufferFactoryFactory = valueBufferFactoryFactory;
                _maxBatchSize = maxBatchSize;
            }

            public override bool AddCommand(ModificationCommand modificationCommand)
            {
                _events?.AddingCommand(modificationCommand, this, this);
                var added = base.AddCommand(modificationCommand);
                //if (_cachedCommandText != null)
                //{
                //    CachedCommandText = _cachedCommandText.Append(CachedCommandText);
                //    _cachedCommandText = null;
                //}
                _events?.AddedCommand(modificationCommand, added, this, this);
                return added;
            }

            public override async Task ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken = default)
            {
                if (false
                    || _prepends.Count > 0
                    || _appends.Count > 0
                    || _befores.Count > 0
                    || _afters.Count > 0)
                    foreach (var batch in Batch(() => new SqlServerModificationCommandBatch(_commandBuilderFactory
                        , _sqlGenerationHelper
                        , _updateSqlGenerator
                        , _valueBufferFactoryFactory
                        , _maxBatchSize)))
                        await batch.ExecuteAsync(connection, cancellationToken);
                else
                    await base.ExecuteAsync(connection, cancellationToken);
            }

            public override void Execute(IRelationalConnection connection)
            {
                if (false
                    || _prepends.Count > 0
                    || _appends.Count > 0
                    || _befores.Count > 0
                    || _afters.Count > 0)
                    foreach (var batch in Batch(() => new SqlServerModificationCommandBatch(_commandBuilderFactory
                        , _sqlGenerationHelper
                        , _updateSqlGenerator
                        , _valueBufferFactoryFactory
                        , _maxBatchSize)))
                        batch.Execute(connection);
                else
                    base.Execute(connection);
            }

            private IEnumerable<SqlServerModificationCommandBatch> Batch(Func<SqlServerModificationCommandBatch> factory)
            {
                var batch = new SqlServerModificationCommandBatch(_commandBuilderFactory, _sqlGenerationHelper, _updateSqlGenerator, _valueBufferFactoryFactory, _maxBatchSize);

                foreach (var prepend in _prepends)
                    if (!batch.AddCommand(prepend))
                    {
                        var newBatch = new SqlServerModificationCommandBatch(_commandBuilderFactory, _sqlGenerationHelper, _updateSqlGenerator, _valueBufferFactoryFactory, _maxBatchSize);
                        if (!newBatch.AddCommand(prepend))
                            throw new Exception("command could not be added to any batch");
                        yield return batch;
                        batch = newBatch;
                    }

                foreach (var command in ModificationCommands)
                {
                    if (_befores.TryGetValue(command, out var befores))
                        foreach (var before in befores)
                            if (!batch.AddCommand(before))
                            {
                                var newBatch = new SqlServerModificationCommandBatch(_commandBuilderFactory, _sqlGenerationHelper, _updateSqlGenerator, _valueBufferFactoryFactory, _maxBatchSize);
                                if (!newBatch.AddCommand(before))
                                    throw new Exception("command could not be added to any batch");
                                yield return batch;
                                batch = newBatch;
                            }
                    batch.AddCommand(command);
                    if (_afters.TryGetValue(command, out var afters))
                        foreach (var after in afters)
                            if (!batch.AddCommand(after))
                            {
                                var newBatch = new SqlServerModificationCommandBatch(_commandBuilderFactory, _sqlGenerationHelper, _updateSqlGenerator, _valueBufferFactoryFactory, _maxBatchSize);
                                if (!newBatch.AddCommand(after))
                                    throw new Exception("command could not be added to any batch");
                                yield return batch;
                                batch = newBatch;
                            }
                }

                foreach (var append in _appends)
                    if (!batch.AddCommand(append))
                    {
                        var newBatch = new SqlServerModificationCommandBatch(_commandBuilderFactory, _sqlGenerationHelper, _updateSqlGenerator, _valueBufferFactoryFactory, _maxBatchSize);
                        if (!newBatch.AddCommand(append))
                            throw new Exception("command could not be added to any batch");
                        yield return batch;
                        batch = newBatch;
                    }

                yield return batch;
            }

            void ISqlServerModificationCommandBatchAppender.Append(ModificationCommand command)
            {
                _appends.Add(command);
            }

            void ISqlServerModificationCommandBatchAppender.Prepend(ModificationCommand command)
            {
                _prepends.Add(command);
            }

            void ISqlServerModificationCommandBatchAppender.Before(ModificationCommand related, ModificationCommand command)
            {
                if (!_befores.TryGetValue(related, out var befores))
                    _befores[related] = befores = new List<ModificationCommand>();
                befores.Add(command);
            }

            void ISqlServerModificationCommandBatchAppender.After(ModificationCommand related, ModificationCommand command)
            {
                if (!_afters.TryGetValue(related, out var afters))
                    _afters[related] = afters = new List<ModificationCommand>();
                afters.Add(command);
            }
        }
    }
}
