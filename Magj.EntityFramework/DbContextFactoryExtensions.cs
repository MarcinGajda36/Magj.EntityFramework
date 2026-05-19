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
    public const QueryTrackingBehavior DefaultReadQueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

    extension<TContext>(IDbContextFactory<TContext> dbContextFactory)
        where TContext : DbContext
    {
        public Task<SavedResult<TResult>> SaveInTransactionAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            IsolationLevel isolationLevel = DefaultIsolationLevel,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (argument, resultFactory, queryTrackingBehavior, isolationLevel),
                static async (context, arguments, cancellationToken) =>
                {
                    context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                    await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                    var result = await arguments.resultFactory(context, arguments.argument, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return new SavedResult<TResult>(result, affectedRows);
                },
                cancellationToken);

        public Task<SavedResult<TResult>> SaveInTransactionAsync<TResult>(
            Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
            IsolationLevel isolationLevel = DefaultIsolationLevel,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (resultFactory, queryTrackingBehavior, isolationLevel),
                static async (context, arguments, cancellationToken) =>
                {
                    context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                    await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                    var result = await arguments.resultFactory(context, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return new SavedResult<TResult>(result, affectedRows);
                },
                cancellationToken);

        public Task<SavedResult<TResult>> SaveAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (argument, resultFactory, queryTrackingBehavior),
                static async (context, arguments, cancellationToken) =>
                {
                    context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                    var result = await arguments.resultFactory(context, arguments.argument, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    return new SavedResult<TResult>(result, affectedRows);
                },
                cancellationToken);

        public Task<SavedResult<TResult>> SaveAsync<TResult>(
            Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
            QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (resultFactory, queryTrackingBehavior),
                static async (context, arguments, cancellationToken) =>
                {
                    context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                    var result = await arguments.resultFactory(context, cancellationToken);
                    var affectedRows = await context.SaveChangesAsync(cancellationToken);
                    return new SavedResult<TResult>(result, affectedRows);
                },
                cancellationToken);

        public Task<TResult> ReadAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
            QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (argument, resultFactory, queryTrackingBehavior),
                static (context, arguments, cancellationToken) =>
                {
                    context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                    return arguments.resultFactory(context, arguments.argument, cancellationToken);
                },
                cancellationToken);

        public Task<TResult> ReadAsync<TResult>(
            Func<TContext, CancellationToken, Task<TResult>> resultFactory,
            QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (resultFactory, queryTrackingBehavior),
                static (context, arguments, cancellationToken) =>
                {
                    context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                    return arguments.resultFactory(context, cancellationToken);
                },
                cancellationToken);

        public Task<TResult> ReadInTransactionAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            IsolationLevel isolationLevel = DefaultIsolationLevel,
            QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (argument, resultFactory, isolationLevel, queryTrackingBehavior),
                static async (context, arguments, cancellationToken) =>
                {
                    context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                    await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                    return await arguments.resultFactory(context, arguments.argument, cancellationToken);
                },
                cancellationToken);

        public Task<TResult> ReadInTransactionAsync<TResult>(
            Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
            IsolationLevel isolationLevel = DefaultIsolationLevel,
            QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
            => ExecuteInStrategyAsync(
                dbContextFactory,
                (resultFactory, isolationLevel, queryTrackingBehavior),
                static async (context, arguments, cancellationToken) =>
                {
                    context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                    await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                    return await arguments.resultFactory(context, cancellationToken);
                },
                cancellationToken);

        public Task<TResult> ExecuteInStrategyAsync<TArgument, TResult>(
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory);
            ArgumentNullException.ThrowIfNull(resultFactory);
            return CoreAsync(dbContextFactory, argument, resultFactory, cancellationToken);

            static async Task<TResult> CoreAsync(
                IDbContextFactory<TContext> dbContextFactory,
                TArgument argument,
                Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
                CancellationToken cancellationToken)
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
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

        public Task<TResult> ExecuteInStrategyAsync<TResult>(
            Func<TContext, CancellationToken, Task<TResult>> resultFactory,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory);
            ArgumentNullException.ThrowIfNull(resultFactory);
            return CoreAsync(dbContextFactory, resultFactory, cancellationToken);

            static async Task<TResult> CoreAsync(
                IDbContextFactory<TContext> dbContextFactory,
                Func<TContext, CancellationToken, Task<TResult>> resultFactory,
                CancellationToken cancellationToken)
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
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
            QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory);
            ArgumentNullException.ThrowIfNull(queryFactory);
            return CoreAsync(dbContextFactory, argument, queryFactory, queryTrackingBehavior, cancellationToken);

            static async IAsyncEnumerable<TResult> CoreAsync(
                IDbContextFactory<TContext> dbContextFactory,
                TArgument argument,
                Func<TContext, TArgument, IQueryable<TResult>> queryFactory,
                QueryTrackingBehavior queryTrackingBehavior,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                context.ChangeTracker.QueryTrackingBehavior = queryTrackingBehavior;
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
            QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory);
            ArgumentNullException.ThrowIfNull(queryFactory);
            return CoreAsync(dbContextFactory, queryFactory, queryTrackingBehavior, cancellationToken);

            static async IAsyncEnumerable<TResult> CoreAsync(
                IDbContextFactory<TContext> dbContextFactory,
                Func<TContext, IQueryable<TResult>> queryFactory,
                QueryTrackingBehavior queryTrackingBehavior,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                context.ChangeTracker.QueryTrackingBehavior = queryTrackingBehavior;
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
