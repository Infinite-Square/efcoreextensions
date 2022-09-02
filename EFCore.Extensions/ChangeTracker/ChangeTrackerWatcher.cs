using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EFCore.Extensions.ChangeTracker
{
    public class ChangeTrackerWatcher
    {
        private readonly DbContext _dbContext;
        private readonly List<EntrySnapshot> _snapshots;
        private readonly List<EntityEntry> _newEntries;

        public ChangeTrackerWatcher(DbContext dbContext)
        {
            _dbContext = dbContext;
            _snapshots = new List<EntrySnapshot>();
            _newEntries = new List<EntityEntry>();
            //_snapshots = dbContext.ChangeTracker
            //    .Entries()
            //    .Where(e => e.State != EntityState.Detached)
            //    .Select(e => new EntrySnapshot(e))
            //    .ToList();
        }

        public void Start()
        {
            _snapshots.AddRange(_dbContext.ChangeTracker
                .Entries()
                .Where(e => e.State != EntityState.Detached)
                .Select(e => new EntrySnapshot(e)));
        }

        public void End()
        {
            Stop();
        }

        public void Stop()
        {
            var j = 0;
            foreach (var e in _dbContext.ChangeTracker.Entries())
                if (j >= _snapshots.Count)
                    _newEntries.Add(e);
                else if (_snapshots[j].Stop(e))
                    j++;
                else if (e.State != EntityState.Detached)
                    _newEntries.Add(e);
        }

        public void Revert()
        {
            foreach (var ne in _newEntries)
                ne.State = EntityState.Detached;
            foreach (var s in _snapshots)
                s.Revert();
        }

        private class EntrySnapshot
        {
            private readonly ModifiedMap _modifiedMap;

            public EntrySnapshot(EntityEntry entityEntry)
            {
                State = entityEntry.State;
                Values = entityEntry.CurrentValues.Clone();
                Entry = entityEntry;
                _modifiedMap = entityEntry.State == EntityState.Modified
                    ? new ModifiedMap(entityEntry)
                    : null;
            }

            public EntityState State { get; }
            public PropertyValues Values { get; }
            public EntityEntry Entry { get; }

            private Changes _changes;

            public bool Stop(EntityEntry entityEntry)
            {
                if (entityEntry.Entity != Entry.Entity) return false;
                _changes = Changes.Create(this, entityEntry);
                return true;
            }

            private class Changes
            {
                private readonly List<IProperty> _values;

                public IEnumerable<IProperty> Values => _values;

                public bool State { get; }

                private Changes(bool state, List<IProperty> values)
                {
                    State = state;
                    _values = values;
                    //_map = map;
                }

                public static Changes Create(EntrySnapshot start, EntityEntry end)
                {
                    var count = end.CurrentValues.Properties.Count;
                    var state = start.State != end.State;
                    var changes = new List<IProperty>(count);
                    //var modifiedMap = new List<IProperty>(count);

                    if (start.State == EntityState.Modified)
                    {
                        // modifiedMap
                        foreach (var p in end.CurrentValues.Properties)
                        {
                            var s = start.Values[p];
                            var e = end.CurrentValues[p];
                            if (!Equals(s, e))
                                changes.Add(p);
                            //else
                            //{
                            //    var (isModified, isTemporary) = start._modifiedMap[p.GetIndex()];
                            //    var ep = end.Property(p.Name);
                            //    if (ep.IsModified != isModified || ep.IsTemporary != isTemporary)
                            //        modifiedMap.Add(p);
                            //}
                        }
                    }
                    else
                    {
                        foreach (var p in end.CurrentValues.Properties)
                        {
                            var s = start.Values[p];
                            var e = end.CurrentValues[p];
                            if (!Equals(s, e))
                                changes.Add(p);
                        }
                    }

                    if (!state
                        && changes.Count <= 0
                        //&& modifiedMap.Count <= 0
                        )
                        return null;

                    return new Changes(state, changes/*, modifiedMap*/);
                }
            }

            // this: begin, snapshot: end
            public void Revert()
            {
                if (_changes == null) return;

                if (_changes.State && State == EntityState.Modified)
                {
                    Entry.State = EntityState.Modified;
                    foreach (var p in Entry.Properties)
                    {
                        if (_changes.Values.Contains(p.Metadata))
                            p.CurrentValue = Values[p.Metadata];
                        var (isModified, isTemporary) = _modifiedMap[p];
                        p.IsModified = isModified;
                        p.IsTemporary = isTemporary;
                    }
                }
                else
                {
                    foreach (var p in _changes.Values)
                        Entry.Property(p.Name).CurrentValue = Values[p];

                    if (_changes.State)
                        Entry.State = State;
                }
            }

            private class Property
            {
                private readonly PropertyEntry _pe;

                public string Name => _pe.Metadata.Name;
                public bool IsModified { get; }
                public bool IsTemporary { get; }

                public Property(PropertyEntry pe)
                {
                    _pe = pe;
                    IsModified = pe.IsModified;
                    IsTemporary = pe.IsTemporary;
                }
            }

            private class ModifiedMap
            {
                private readonly BitArray _state;

                public ModifiedMap(EntityEntry entityEntry)
                {
                    var pc = entityEntry.Metadata.PropertyCount();
                    _state = new BitArray(pc * 2);

                    foreach (var p in entityEntry.Metadata.GetProperties())
                    {
                        var pe = entityEntry.Property(p.Name);
                        var index = p.GetIndex();
                        _state[index] = pe.IsModified;
                        _state[index + 1] = pe.IsTemporary;
                    }
                }

                public (bool isModified, bool isTemporary) this[int index]
                {
                    get => (_state[index], _state[index + 1]);
                }

                public (bool isModified, bool isTemporary) this[PropertyEntry pe]
                {
                    get => this[pe.Metadata.GetOriginalValueIndex()];
                }
            }
        }
    }
}
