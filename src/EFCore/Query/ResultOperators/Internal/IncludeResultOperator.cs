// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Clauses.StreamedData;

namespace Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class IncludeResultOperator : SequenceTypePreservingResultOperatorBase, IQueryAnnotation
    {
        private List<string> _navigationPropertyPaths;
        private List<INavigation[]> _navigationPaths;
        private IQuerySource _querySource;
        private Type _sourceEntityType;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IncludeResultOperator(
            [NotNull] INavigation[] navigationPath, [NotNull] Expression pathFromQuerySource)
            //: this(navigationPath[0].DeclaringType.ClrType, navigationPath.Select(n => n.Name), pathFromQuerySource)
        {
            _sourceEntityType = navigationPath[0].DeclaringType.ClrType;
            _navigationPaths = new List<INavigation[]> { navigationPath };
            _navigationPropertyPaths = new List<string>(); // ?
            PathFromQuerySource = pathFromQuerySource;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IncludeResultOperator(
            //[NotNull] Type sourceEntityType,
            [NotNull] IEnumerable<string> navigationPropertyPaths,
            [NotNull] Expression pathFromQuerySource)
        {
            //_sourceEntityType = sourceEntityType;
            _navigationPropertyPaths = new List<string>(navigationPropertyPaths);
            PathFromQuerySource = pathFromQuerySource;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IQuerySource QuerySource
        {
            get => _querySource ?? (_querySource = GetQuerySource(PathFromQuerySource));
            set => _querySource = value;
        }

        private static IQuerySource GetQuerySource(Expression expression)
            => expression.TryGetReferencedQuerySource()
               ?? (expression is MemberExpression memberExpression
                   ? GetQuerySource(memberExpression.Expression.RemoveConvert())
                   : null);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual QueryModel QueryModel { get; set; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual Type SourceEntityType { get; set; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IReadOnlyList<string> NavigationPropertyPaths => _navigationPropertyPaths;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual Expression PathFromQuerySource { get; [param: NotNull] set; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual void AppendToNavigationPath([NotNull] IReadOnlyList<PropertyInfo> propertyInfos)
        {
            if (_navigationPropertyPaths == null)
            {
                _navigationPropertyPaths = new List<string>();
            }

            _navigationPropertyPaths.AddRange(propertyInfos.Select(pi => pi.Name));
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual List<INavigation[]> GetNavigationPaths([NotNull] QueryCompilationContext queryCompilationContext)
        {
            if (_navigationPaths == null)
            {
                IEntityType entityType = null;
                if (PathFromQuerySource is QuerySourceReferenceExpression qsre)
                {
                    entityType = queryCompilationContext.FindEntityType(qsre.ReferencedQuerySource);
                }
                if (entityType == null)
                {
                    entityType = queryCompilationContext.Model.FindEntityType(PathFromQuerySource.Type);

                    if (entityType == null)
                    {
                        var pathFromSource = MemberAccessBindingExpressionVisitor.GetPropertyPath(
                            PathFromQuerySource, queryCompilationContext, out qsre);
                        if (pathFromSource.Count > 0
                            && pathFromSource[pathFromSource.Count - 1] is INavigation navigation)
                        {
                            entityType = navigation.GetTargetType();
                        }
                    }
                }

                if (entityType == null)
                {
                    throw new NotSupportedException(
                        CoreStrings.IncludeNotSpecifiedDirectlyOnEntityType(
                            ToString(),
                            NavigationPropertyPaths.FirstOrDefault()));
                }

                _navigationPaths = new List<INavigation[]>();
                var currentNavigationPath = new INavigation[NavigationPropertyPaths.Count];

                //_navigationPath = new INavigation[NavigationPropertyPaths.Count];

                for (var i = 0; i < NavigationPropertyPaths.Count; i++)
                {
                    foreach (var navigation in FindNavigations(entityType, NavigationPropertyPaths[i]))
                    {
                        var navigationPath = (INavigation[])currentNavigationPath.Clone();
                        navigationPath[i] = navigation;

                        // clone navigation, add new one, add to paths
                    }

                    //currentNavigationPath[i] = FindNavigation(entityType, NavigationPropertyPaths[i]);

                    if (currentNavigationPath[i] == null)
                    {
                        throw new InvalidOperationException(
                            CoreStrings.IncludeBadNavigation(NavigationPropertyPaths[i], entityType.DisplayName()));
                    }

                    entityType = currentNavigationPath[i].GetTargetType();
                }

                _navigationPaths.Add(currentNavigationPath);
            }

            return _navigationPaths;
        }

        private List<INavigation> FindNavigations(IEntityType entityType, string name)
        {
            var navigations = new List<INavigation>();

            var navigationPath = entityType.FindNavigation(name);
            if (navigationPath != null)
            {
                navigations.Add(navigationPath);
            }

            foreach (var derived in entityType.GetDirectlyDerivedTypes())
            {
                foreach (var navigationPathOnDerived in FindNavigations(derived, name))
                {
                    navigations.Add(navigationPathOnDerived);
                }
            }

            return navigations;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override string ToString()
            => $@"Include(""{NavigationPropertyPaths.Join(".")}"")";

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual string DisplayString()
            => $"{PathFromQuerySource}.{_navigationPropertyPaths.Join(".")}";

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override ResultOperatorBase Clone(CloneContext cloneContext)
            => new IncludeResultOperator(/*SourceEntityType,*/NavigationPropertyPaths, PathFromQuerySource);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override void TransformExpressions([NotNull] Func<Expression, Expression> transformation)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override StreamedSequence ExecuteInMemory<T>([NotNull] StreamedSequence input) => input;
    }
}
