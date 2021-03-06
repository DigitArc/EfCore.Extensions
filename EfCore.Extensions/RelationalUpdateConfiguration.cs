﻿using System;
using System.Collections.Generic;

namespace EfCore.Extensions
{
    public class RelationalUpdateConfiguration
    {
        public RelationalUpdateConfiguration()
        {
            UpdatedTypes = new List<RelationalUpdateConfigurationType>();
        }
        public bool RemoveDataInDatabase { get; set; }
        public bool TriggerSaveChanges { get; set; }
        public List<RelationalUpdateConfigurationType> UpdatedTypes { get; set; }
        public RelationalUpdateConfiguration AddType(Type type)
        {
            AddType(type, RemoveDataInDatabase);
            return this;
        }
        public RelationalUpdateConfiguration AddType(Type type, bool removeDataInDatabase)
        {
            UpdatedTypes.Add(new RelationalUpdateConfigurationType
            {
                RemoveOnDatabase = removeDataInDatabase,
                Type = type
            });
            return this;
        }

    }
}