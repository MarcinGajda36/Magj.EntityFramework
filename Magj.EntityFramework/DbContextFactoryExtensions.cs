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

    public static Task<SavedResult<TResult>> SaveInTransactionAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = DefaultIsolationLevel,
        QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
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
    }

    public static Task<SavedResult<TResult>> SaveInTransactionAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = DefaultIsolationLevel,
        QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => SaveInTransactionAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            isolationLevel,
            queryTrackingBehavior,
            cancellationToken);

    public static Task<SavedResult<TResult>> SaveAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
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
    }

    public static Task<SavedResult<TResult>> SaveAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
        QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => SaveAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            queryTrackingBehavior,
            cancellationToken);

    public static Task<TResult> ReadAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
        QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (argument, resultFactory, queryTrackingBehavior),
            static (context, arguments, cancellationToken) =>
            {
                context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                return arguments.resultFactory(context, arguments.argument, cancellationToken);
            },
            cancellationToken);
    }

    public static Task<TResult> ReadAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, Task<TResult>> resultFactory,
        QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => ReadAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            queryTrackingBehavior,
            cancellationToken);

    public static Task<TResult> ReadInTransactionAsync<TContext, TArgument, TResult, TEntity>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, IQueryable<TEntity>> entitySelector,
        Func<IQueryable<TEntity>, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = DefaultIsolationLevel,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(entitySelector);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (isolationLevel, entitySelector, resultFactory, argument),
            static async (context, arguments, cancellationToken) =>
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var (isolationLevel, entitySelector, resultFactory, argument) = arguments;
                await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                var entities = entitySelector(context);
                return await resultFactory(entities, argument, cancellationToken);
            },
            cancellationToken);
    }

    public static Task<TResult> ExecuteInStrategyAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return Core(dbContextFactory, argument, resultFactory, cancellationToken);

        static async Task<TResult> Core(
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

    public static Task<TResult> ExecuteInStrategyAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return Core(dbContextFactory, resultFactory, cancellationToken);

        static async Task<TResult> Core(
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

    public static IAsyncEnumerable<TResult> StreamAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, IQueryable<TResult>> queryFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(queryFactory);
        return Core(dbContextFactory, argument, queryFactory, cancellationToken);

        static async IAsyncEnumerable<TResult> Core(
            IDbContextFactory<TContext> dbContextFactory,
            TArgument argument,
            Func<TContext, TArgument, IQueryable<TResult>> queryFactory,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            await foreach (var result in queryFactory(context, argument)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                yield return result;
            }
        }
    }

    public static Task<TResult> ReadInTransactionAsync<TContext, TResult, TEntity>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, IQueryable<TEntity>> entitySelector,
        Func<IQueryable<TEntity>, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = DefaultIsolationLevel,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(entitySelector);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (isolationLevel, entitySelector, resultFactory),
            static async (context, arguments, cancellationToken) =>
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var (isolationLevel, entitySelector, resultFactory) = arguments;
                await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                var entities = entitySelector(context);
                return await resultFactory(entities, cancellationToken);
            },
            cancellationToken);
    }

    public static IAsyncEnumerable<TResult> StreamAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, IQueryable<TResult>> queryFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(queryFactory);
        return Core(dbContextFactory, queryFactory, cancellationToken);

        static async IAsyncEnumerable<TResult> Core(
            IDbContextFactory<TContext> dbContextFactory,
            Func<TContext, IQueryable<TResult>> queryFactory,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            await foreach (var result in queryFactory(context)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                yield return result;
            }
        }
    }
}
