using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFCore.Extensions.ChangeTracker
{
    public class ChangeTrackerWatcher
    {
        private readonly DbContext _dbContext;
        private Dictionary<Type, Dictionary<string, EntrySnapshot>> _start;
        private Dictionary<Type, Dictionary<string, EntrySnapshot>> _end;

        public ChangeTrackerWatcher(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Start()
        {
            if (_start != null) throw new InvalidOperationException("start must be called one time only");
            _start = CreateChangesSnapshot(_dbContext);
        }

        public void End()
        {
            if (_start == null) throw new InvalidOperationException("start must be called");
            if (_end != null) throw new InvalidOperationException("end must be called one time only");
            _end = CreateChangesSnapshot(_dbContext);
        }

        public void Revert()
        {
            if (_start == null) throw new InvalidOperationException("start must be called");
            if (_end == null) throw new InvalidOperationException("end must be called");

            var begin = _start;
            var end = _end;
            foreach (var et in end)
            {
                if (begin.TryGetValue(et.Key, out var tb))
                {
                    foreach (var e in et.Value)
                    {
                        if (tb.TryGetValue(e.Key, out var bs))
                        {
                            var es = e.Value;

                            if (bs.State == es.State)
                            {
                                switch (bs.State)
                                {
                                    case EntityState.Detached:
                                    case EntityState.Unchanged:
                                    case EntityState.Deleted:
                                        // there is no changes on the entity
                                        continue;
                                    case EntityState.Modified:
                                    case EntityState.Added:
                                        // we have to check values
                                        if (EntrySnapshot.ValuesAreEquals(bs, es))
                                            continue;
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }

                            if (es.State != es.Entry.State)
                            {
                                // conflict
                                throw new NotImplementedException();
                            }

                            bs.Reset(es);

                            if (bs.State != es.Entry.State)
                            {
                                throw new NotImplementedException();
                            }
                        }
                        else
                        {
                            // entry is not in target, we have to untrack it
                            var entry = e.Value.Entry;
                            entry.State = EntityState.Detached; // maybe unchanged in order to preserve new ref ?
                        }
                    }
                }
                else
                {
                    foreach (var e in et.Value)
                    {
                        e.Value.Entry.State = EntityState.Detached;
                    }
                }
            }
        }

        private static Dictionary<Type, Dictionary<string, EntrySnapshot>> CreateChangesSnapshot(DbContext dbContext)
        {
            return dbContext.ChangeTracker
                .Entries()
                .GroupBy(k => k.Metadata.ClrType)
                .ToDictionary(g => g.Key, g => g.ToDictionary(e => Key(e), e => new EntrySnapshot(e)));

            string Key(EntityEntry entry)
            {
                var keys = entry.Metadata.FindPrimaryKey().Properties.ToDictionary(p => p.Name, p => (value: entry.Property(p.Name).CurrentValue, property: p));
                var rawKey = keys.Count == 1
                    ? keys.Values.First().value.ToString()
                    : keys.Count > 1
                        ? $"{{{string.Join(",", keys.OrderBy(k => k.Key).Select(k => $"\"{k.Key}\":\"{k.Value}\""))}}}"
                        : null;
                return rawKey;
            }
        }

        private class EntrySnapshot
        {
            private readonly Dictionary<string, (bool IsModified, bool IsTemporary)> _modifiedMap;

            public EntrySnapshot(EntityEntry entityEntry)
            {
                State = entityEntry.State;
                Values = entityEntry.CurrentValues.Clone();//.ToObject();
                Entry = entityEntry;
                //if (entityEntry.State == EntityState.Modified)
                    _modifiedMap = entityEntry.Properties.ToDictionary(p => p.Metadata.Name, p => (p.IsModified, p.IsTemporary));
            }

            public EntityState State { get; }
            public PropertyValues Values { get; }
            public EntityEntry Entry { get; }

            public void Reset(EntrySnapshot snapshot)
            {
                var entry = snapshot.Entry;
                switch (State)
                {
                    case EntityState.Deleted:
                    case EntityState.Unchanged:
                        entry.CurrentValues.SetValues(Values);
                        entry.State = State;
                        break;
                    case EntityState.Modified:
                        //entry.CurrentValues.SetValues(Values);
                        entry.State = State;

                        if (_modifiedMap == null) throw new InvalidOperationException();
                        foreach (var p in entry.Properties)
                        {
                            if (_modifiedMap.TryGetValue(p.Metadata.Name, out var ps))
                            {
                                var bv = Values[p.Metadata];
                                var ev = snapshot.Values[p.Metadata];
                                //var cv = entry.CurrentValues[p.Metadata];

                                var comparer = p.Metadata.GetValueComparer();
                                if (comparer != null)
                                {
                                    if (!comparer.Equals(p.CurrentValue, ev))
                                    {
                                        //todo: handle conflict
                                        throw new NotImplementedException();
                                    }

                                    if (!comparer.Equals(ev, bv))
                                    {
                                        //entry.CurrentValues[p.Metadata] = bv;
                                        p.CurrentValue = bv;
                                    }
                                }
                                else
                                {
                                    if (!Equals(p.CurrentValue, ev))
                                    {
                                        //todo: handle conflict
                                        throw new NotImplementedException();
                                    }

                                    if (!Equals(ev, bv))
                                    {
                                        //entry.CurrentValues[p.Metadata] = bv;
                                        p.CurrentValue = bv;
                                    }
                                }

                                p.IsModified = ps.IsModified;
                                p.IsTemporary = ps.IsTemporary;
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                        break;
                    case EntityState.Added:
                        entry.CurrentValues.SetValues(Values);
                        entry.State = State;
                        break;
                    case EntityState.Detached:
                    default:
                        throw new NotImplementedException();
                }
            }

            public static bool ValuesAreEquals(EntrySnapshot a, EntrySnapshot b)
            {
                //var ae = a.Entry;
                //var be = b.Entry;
                var avs = a.Values;
                var bvs = b.Values;

                if (avs.Properties.Count != bvs.Properties.Count) return false;

                foreach (var ap in avs.Properties)
                {
                    var av = avs[ap];
                    var bv = bvs[ap];

                    var comparer = ap.GetValueComparer();
                    if (comparer != null)
                    {
                        if (!comparer.Equals(av, bv))
                            return false;
                    }
                    else
                    {
                        if (!Equals(av, bv))
                            return false;
                    }
                }

                return true;
            }
        }
    }
}
