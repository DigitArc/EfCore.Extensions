using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EfCore.Extensions
{
    public static class DbContextExtensions
    {
        public static object GetDefaultValue(this Type t)
        {
            if (t.IsValueType && Nullable.GetUnderlyingType(t) == null)
                return Activator.CreateInstance(t);
            return null;
        }
        private static IEnumerable<RelationalUpdateConfigurationType> GetCollectionTypes(this EntityEntry entry)
        {
            var collections = entry.Collections;
            foreach (var collectionEntry in collections)
            {
                yield return new RelationalUpdateConfigurationType
                {
                    RemoveOnDatabase = true,
                    Type = collectionEntry.Metadata.ClrType.GetGenericArguments()[0]
                };
            }
        }

        public static ValueTask<int> RelationalUpdateAsync<T>(this DbContext context, T entity) where T : class
        {
            var configuration = new RelationalUpdateConfiguration
            {
                UpdatedTypes = context.Entry(entity).GetCollectionTypes().ToList()
            };
            return RelationalUpdateAsync(context, entity, configuration);
        }

        public static object GetPrimaryKeyValue(this EntityEntry entry)
        {
            return entry.Metadata.FindPrimaryKey().Properties.Select(p => entry.Property(p.Name).CurrentValue)
                .FirstOrDefault();
        }

        public static string GetPrimaryKeyName(this EntityEntry entry)
        {
            return entry.Metadata.FindPrimaryKey().Properties.FirstOrDefault()?.Name;
        }
        public static async ValueTask<int> RelationalUpdateAsync<T>(this DbContext context, T entity, RelationalUpdateConfiguration configuration) where T : class
        {
            var entry = context.Entry(entity);
            var primaryKey = entry.GetPrimaryKeyValue();
            var navigation = entry.Metadata.GetNavigations().ToList();

            foreach (var collectionType in configuration.UpdatedTypes)
            {
                var propertyName = navigation.Where(p => p.ClrType.GetGenericArguments()[0] == collectionType.Type).Select(p => p.Name).FirstOrDefault();
                if (string.IsNullOrEmpty(propertyName)) continue;
                var collection = entry.Collection(propertyName);
                var foreignKey = collection.Metadata.ForeignKey;
                var primaryKeyName = collection.EntityEntry.GetPrimaryKeyName();

                var dynamicList = await collection.CurrentValue.ToDynamicListAsync();
                var primaryKeyList = dynamicList
                    .Select(p => p.GetType().GetProperty(primaryKeyName)?.GetValue(p, null))
                    .Where(p => p != GetDefaultValue(p.GetType()))
                    .ToList();

                var fkName = foreignKey.Properties.FirstOrDefault()?.Name;

                if (!collectionType.RemoveOnDatabase) continue;
                var databaseValues = await collection.Query().Where($"{fkName} == @0", primaryKey).ToDynamicListAsync();

                foreach (dynamic o in databaseValues)
                {
                    var id = o.GetType().GetProperty(primaryKeyName)?.GetValue(o, null);
                    if (primaryKeyList.Contains(id))
                    {
                        context.Entry(o).State = EntityState.Detached;
                    }
                    else
                    {
                        context.Remove(o);
                    }
                }

            }
            if (configuration.TriggerSaveChanges)
            {
                return await context.SaveChangesAsync();
            }

            return 0;
        }
    }
}