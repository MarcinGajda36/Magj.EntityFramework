namespace Magj.EntityFramework;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public readonly record struct SavedResult<TResult>(TResult Result, int AffectedRows);

public static class DbContextFactoryExtensions
{
    public const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;
    public const QueryTrackingBehavior DefaultSaveQueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

    internal const QueryTrackingBehavior ReadQueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

    extension<TContext>(IDbContextFactory<TContext> dbContextFactory)
        where TContext : DbContext
    {
        /// <summary>
        /// Executes provided <see cref="resultFactory"/> inside transaction, using default ExecutionStrategy, then saves and commits results.
        /// </summary>
        /// <typeparam name="TArgument">Type of argument used for avoiding lambda captures.</typeparam>
        /// <typeparam name="TResult">Type returned by <see cref="resultFactory"/> function.</typeparam>
        /// <param name="argument">Argument used for avoiding lambda captures.</param>
        /// <param name="resultFactory">Function that either causes changes visible to EF ChangeTracker, or saves data to database itself.</param>
        /// <param name="isolationLevel">Level of transaction isolation.</param>
        /// <param name="queryTrackingBehavior">Behavior of EF ChangeTracker query tracking.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Value returned from <see cref="resultFactory"/> together with count of affected rows.</returns>
        public Task<SavedResult<TResult>> SaveInTransactionAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            IsolationLevel isolationLevel = DefaultIsolationLevel,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (argument, resultFactory, isolationLevel),
                static async (context, arguments, cancellationToken) =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                    var result = await arguments.resultFactory(context, arguments.argument, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return new SavedResult<TResult>(result, affectedRows);
                },
                queryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> inside transaction, using default ExecutionStrategy, then saves and commits results.
        /// </summary>
        /// <typeparam name="TResult">Type returned by <see cref="resultFactory"/> function.</typeparam>
        /// <param name="resultFactory">Function that either causes changes visible to EF ChangeTracker, or saves data to database itself.</param>
        /// <param name="isolationLevel">Level of transaction isolation.</param>
        /// <param name="queryTrackingBehavior">Behavior of EF ChangeTracker query tracking.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Value returned from <see cref="resultFactory"/> together with count of affected rows.</returns>
        public Task<SavedResult<TResult>> SaveInTransactionAsync<TResult>(
            Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
            IsolationLevel isolationLevel = DefaultIsolationLevel,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (resultFactory, isolationLevel),
                static async (context, arguments, cancellationToken) =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                    var result = await arguments.resultFactory(context, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return new SavedResult<TResult>(result, affectedRows);
                },
                queryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> inside transaction, using default ExecutionStrategy, then saves and commits results.
        /// </summary>
        /// <typeparam name="TArgument">Type of argument used for avoiding lambda captures.</typeparam>
        /// <param name="argument">Argument used for avoiding lambda captures.</param>
        /// <param name="resultFactory">Function that either causes changes visible to EF ChangeTracker, or saves data to database itself.</param>
        /// <param name="isolationLevel">Level of transaction isolation.</param>
        /// <param name="queryTrackingBehavior">Behavior of EF ChangeTracker query tracking.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Count of affected rows.</returns>
        public Task<int> SaveInTransactionAsync<TArgument>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, ValueTask> resultFactory,
            IsolationLevel isolationLevel = DefaultIsolationLevel,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (argument, resultFactory, isolationLevel),
                static async (context, arguments, cancellationToken) =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                    await arguments.resultFactory(context, arguments.argument, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return affectedRows;
                },
                queryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> inside transaction, using default ExecutionStrategy, then saves and commits results.
        /// </summary>
        /// <param name="resultFactory">Function that either causes changes visible to EF ChangeTracker, or saves data to database itself.</param>
        /// <param name="isolationLevel">Level of transaction isolation.</param>
        /// <param name="queryTrackingBehavior">Behavior of EF ChangeTracker query tracking.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Count of affected rows.</returns>
        public Task<int> SaveInTransactionAsync(
            Func<TContext, CancellationToken, ValueTask> resultFactory,
            IsolationLevel isolationLevel = DefaultIsolationLevel,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (resultFactory, isolationLevel),
                async (context, arguments, cancellationToken) =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                    await arguments.resultFactory(context, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return affectedRows;
                },
                queryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> using default ExecutionStrategy, then saves results.
        /// </summary>
        /// <typeparam name="TArgument">Type of argument used for avoiding lambda captures.</typeparam>
        /// <typeparam name="TResult">Type returned by <see cref="resultFactory"/> function.</typeparam>
        /// <param name="argument">Argument used for avoiding lambda captures.</param>
        /// <param name="resultFactory">Function that either causes changes visible to EF ChangeTracker, or saves data to database itself.</param>
        /// <param name="queryTrackingBehavior">Behavior of EF ChangeTracker query tracking.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Value returned from <see cref="resultFactory"/> together with count of affected rows.</returns>
        public Task<SavedResult<TResult>> SaveAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (argument, resultFactory),
                static async (context, arguments, cancellationToken) =>
                {
                    var result = await arguments.resultFactory(context, arguments.argument, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    return new SavedResult<TResult>(result, affectedRows);
                },
                queryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> using default ExecutionStrategy, then saves results.
        /// </summary>
        /// <typeparam name="TResult">Type returned by <see cref="resultFactory"/> function.</typeparam>
        /// <param name="resultFactory">Function that either causes changes visible to EF ChangeTracker, or saves data to database itself.</param>
        /// <param name="queryTrackingBehavior">Behavior of EF ChangeTracker query tracking.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Value returned from <see cref="resultFactory"/> together with count of affected rows.</returns>
        public Task<SavedResult<TResult>> SaveAsync<TResult>(
            Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                resultFactory,
                static async (context, resultFactory, cancellationToken) =>
                {
                    var result = await resultFactory(context, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    return new SavedResult<TResult>(result, affectedRows);
                },
                queryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> using default ExecutionStrategy, then saves results.
        /// </summary>
        /// <typeparam name="TArgument">Type of argument used for avoiding lambda captures.</typeparam>
        /// <param name="argument">Argument used for avoiding lambda captures.</param>
        /// <param name="resultFactory">Function that either causes changes visible to EF ChangeTracker, or saves data to database itself.</param>
        /// <param name="queryTrackingBehavior">Behavior of EF ChangeTracker query tracking.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Count of affected rows.</returns>
        public Task<int> SaveAsync<TArgument>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, ValueTask> resultFactory,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (argument, resultFactory),
                static async (context, arguments, cancellationToken) =>
                {
                    await arguments.resultFactory(context, arguments.argument, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    return affectedRows;
                },
                queryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> using default ExecutionStrategy, then saves results.
        /// </summary>
        /// <param name="resultFactory">Function that either causes changes visible to EF ChangeTracker, or saves data to database itself.</param>
        /// <param name="queryTrackingBehavior">Behavior of EF ChangeTracker query tracking.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Count of affected rows.</returns>
        public Task<int> SaveAsync(
            Func<TContext, CancellationToken, ValueTask> resultFactory,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                resultFactory,
                static async (context, resultFactory, cancellationToken) =>
                {
                    await resultFactory(context, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    return affectedRows;
                },
                queryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> using default ExecutionStrategy, then returns results.
        /// </summary>
        /// <typeparam name="TArgument">Type of argument used for avoiding lambda captures.</typeparam>
        /// <typeparam name="TResult">Type returned by <see cref="resultFactory"/> function.</typeparam>
        /// <param name="argument">Argument used for avoiding lambda captures.</param>
        /// <param name="resultFactory">Function that read desired results.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing reads.</param>
        /// <returns>Value returned from <see cref="resultFactory"/>.</returns>
        public Task<TResult> ReadAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                argument,
                resultFactory,
                ReadQueryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> using default ExecutionStrategy, then returns results.
        /// </summary>
        /// <typeparam name="TResult">Type returned by <see cref="resultFactory"/> function.</typeparam>
        /// <param name="resultFactory">Function that read desired results.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing reads.</param>
        /// <returns>Value returned from <see cref="resultFactory"/>.</returns>
        public Task<TResult> ReadAsync<TResult>(
            Func<TContext, CancellationToken, Task<TResult>> resultFactory,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                resultFactory,
                ReadQueryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> inside transaction, using default ExecutionStrategy, then returns results.
        /// </summary>
        /// <typeparam name="TArgument">Type of argument used for avoiding lambda captures.</typeparam>
        /// <typeparam name="TResult">Type returned by <see cref="resultFactory"/> function.</typeparam>
        /// <param name="argument">Argument used for avoiding lambda captures.</param>
        /// <param name="resultFactory">Function that read desired results.</param>
        /// <param name="isolationLevel">Level of transaction isolation.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Value returned from <see cref="resultFactory"/>.</returns>
        public Task<TResult> ReadInTransactionAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            IsolationLevel isolationLevel = DefaultIsolationLevel,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (argument, resultFactory, isolationLevel),
                static async (context, arguments, cancellationToken) =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                    return await arguments.resultFactory(context, arguments.argument, cancellationToken);
                },
                ReadQueryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> inside transaction, using default ExecutionStrategy, then returns results.
        /// </summary>
        /// <typeparam name="TResult">Type returned by <see cref="resultFactory"/> function.</typeparam>
        /// <param name="resultFactory">Function that read desired results.</param>
        /// <param name="isolationLevel">Level of transaction isolation.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Value returned from <see cref="resultFactory"/>.</returns>
        public Task<TResult> ReadInTransactionAsync<TResult>(
            Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
            IsolationLevel isolationLevel = DefaultIsolationLevel,
            QueryTrackingBehavior queryTrackingBehavior = ReadQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (resultFactory, isolationLevel),
                static async (context, arguments, cancellationToken) =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                    return await arguments.resultFactory(context, cancellationToken);
                },
                queryTrackingBehavior,
                cancellationToken);

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> using default ExecutionStrategy, then returns results.
        /// </summary>
        /// <typeparam name="TArgument">Type of argument used for avoiding lambda captures.</typeparam>
        /// <typeparam name="TResult">Type returned by <see cref="resultFactory"/> function.</typeparam>
        /// <param name="argument">Argument used for avoiding lambda captures.</param>
        /// <param name="resultFactory">Any function that interacts with DbContext.</param>
        /// <param name="queryTrackingBehavior">Behavior of EF ChangeTracker query tracking.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Value returned from <see cref="resultFactory"/>.</returns>
        public Task<TResult> ExecuteInStrategyAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory);
            ArgumentNullException.ThrowIfNull(resultFactory);
            return CoreAsync(dbContextFactory, argument, resultFactory, queryTrackingBehavior, cancellationToken);

            static async Task<TResult> CoreAsync(
                IDbContextFactory<TContext> dbContextFactory,
                TArgument argument,
                Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
                QueryTrackingBehavior queryTrackingBehavior,
                CancellationToken cancellationToken)
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                context.ChangeTracker.QueryTrackingBehavior = queryTrackingBehavior;
                var strategy = context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(
                    (argument, resultFactory),
                    static (contextBase, arguments, cancellationToken) =>
                    {
                        contextBase.ChangeTracker.Clear(); // When re-try triggers then ChangeTracker still has changes from previous try iirc.
                        return arguments.resultFactory((TContext)contextBase, arguments.argument, cancellationToken);
                    },
                    null,
                    cancellationToken);
            }
        }

        /// <summary>
        /// Executes provided <see cref="resultFactory"/> using default ExecutionStrategy, then returns results.
        /// </summary>
        /// <typeparam name="TResult">Type returned by <see cref="resultFactory"/> function.</typeparam>
        /// <param name="resultFactory">Any function that interacts with DbContext.</param>
        /// <param name="queryTrackingBehavior">Behavior of EF ChangeTracker query tracking.</param>
        /// <param name="cancellationToken">Token for cancelling ongoing saving.</param>
        /// <returns>Value returned from <see cref="resultFactory"/>.</returns>
        public Task<TResult> ExecuteInStrategyAsync<TResult>(
            Func<TContext, CancellationToken, Task<TResult>> resultFactory,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory);
            ArgumentNullException.ThrowIfNull(resultFactory);
            return CoreAsync(dbContextFactory, resultFactory, queryTrackingBehavior, cancellationToken);

            static async Task<TResult> CoreAsync(
                IDbContextFactory<TContext> dbContextFactory,
                Func<TContext, CancellationToken, Task<TResult>> resultFactory,
                QueryTrackingBehavior queryTrackingBehavior,
                CancellationToken cancellationToken)
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                context.ChangeTracker.QueryTrackingBehavior = queryTrackingBehavior;
                var strategy = context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(
                    resultFactory,
                    static (contextBase, resultFactory, cancellationToken) =>
                    {
                        contextBase.ChangeTracker.Clear(); // When re-try triggers then ChangeTracker still has changes from previous try iirc.
                        return resultFactory((TContext)contextBase, cancellationToken);
                    },
                    null,
                    cancellationToken);
            }
        }

        public IAsyncEnumerable<TResult> StreamReadAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, IQueryable<TResult>> queryFactory,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory);
            ArgumentNullException.ThrowIfNull(queryFactory);
            return CoreAsync(dbContextFactory, argument, queryFactory, cancellationToken);

            static async IAsyncEnumerable<TResult> CoreAsync(
                IDbContextFactory<TContext> dbContextFactory,
                TArgument argument,
                Func<TContext, TArgument, IQueryable<TResult>> queryFactory,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                context.ChangeTracker.QueryTrackingBehavior = ReadQueryTrackingBehavior;
                await foreach (var result in queryFactory(context, argument)
                    .AsAsyncEnumerable()
                    .WithCancellation(cancellationToken))
                {
                    yield return result;
                }
            }
        }

        public IAsyncEnumerable<TResult> StreamReadAsync<TResult>(
            Func<TContext, IQueryable<TResult>> queryFactory,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory);
            ArgumentNullException.ThrowIfNull(queryFactory);
            return CoreAsync(dbContextFactory, queryFactory, cancellationToken);

            static async IAsyncEnumerable<TResult> CoreAsync(
                IDbContextFactory<TContext> dbContextFactory,
                Func<TContext, IQueryable<TResult>> queryFactory,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                context.ChangeTracker.QueryTrackingBehavior = ReadQueryTrackingBehavior;
                await foreach (var result in queryFactory(context)
                    .AsAsyncEnumerable()
                    .WithCancellation(cancellationToken))
                {
                    yield return result;
                }
            }
        }
    }
}
