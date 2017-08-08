// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore.TestModels;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore
{
    public class MonsterFixupSqlServerTest : MonsterFixupTestBase, IDisposable
    {
        protected override IServiceProvider CreateServiceProvider()
            => new ServiceCollection().AddEntityFrameworkSqlServer().BuildServiceProvider(validateScopes: true);

        protected override DbContextOptions CreateOptions(string databaseName)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlServer(CreateConnectionString(databaseName), b => b.ApplyConfiguration())
                .EnableSensitiveDataLogging();

            return optionsBuilder.Options;
        }

        private static string CreateConnectionString(string name)
            => new SqlConnectionStringBuilder(TestEnvironment.DefaultConnection)
            {
                MultipleActiveResultSets = true,
                InitialCatalog = name
            }.ConnectionString;

        private SqlServerTestStore _testStore;

        protected override void CreateAndSeedDatabase(string databaseName, Func<MonsterContext> createContext, Action<MonsterContext> seed)
        {
            _testStore = SqlServerTestStore.GetOrCreate(databaseName)
                .InitializeSqlServer(null, createContext, c => seed((MonsterContext)c));
        }

        public virtual void Dispose() => _testStore?.Dispose();

        protected override void OnModelCreating<TMessage, TProductPhoto, TProductReview>(ModelBuilder builder)
        {
            base.OnModelCreating<TMessage, TProductPhoto, TProductReview>(builder);

            builder.Entity<TMessage>().Property(e => e.MessageId).UseSqlServerIdentityColumn();
            builder.Entity<TProductPhoto>().Property(e => e.PhotoId).UseSqlServerIdentityColumn();
            builder.Entity<TProductReview>().Property(e => e.ReviewId).UseSqlServerIdentityColumn();
        }
    }
}
