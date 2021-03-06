﻿using System;
using System.Collections.Generic;
using System.Linq;
using NaiveLanguageTools.Common;

namespace Skila.Language.Semantics
{
    public sealed class ErrorManager
    {
        public static ErrorManager Create(bool throwOnError)
        {
            return new ErrorManager(throwOnError);
        }

        private readonly ErrorPriority errorPriority;
        private readonly bool throwOnError;
        private List<Error> errors;
        private readonly HashSet<Tuple<ErrorCode, INode>> errorTranslations;

        public IReadOnlyList<Error> Errors => this.errors;

        private ErrorManager(bool throwOnError)
        {
            this.throwOnError = System.Diagnostics.Debugger.IsAttached && throwOnError;
            this.errors = new List<Error>();
            this.errorPriority = new ErrorPriority();
            this.errorTranslations = new HashSet<Tuple<ErrorCode, INode>>();
        }

        public bool HasError(ErrorCode code, INode node,int count = 1)
        {
            return ErrorCount(code,node)==count;
        }
        public int ErrorCount(ErrorCode code, INode node)
        {
            return this.Errors.Count(it => it.Code == code && it.Node == node);
        }
        /*public bool HasCode(ErrorCode code)
        {
            return this.Errors.Any(it => it.Code == code);
        }
        public bool HasNode(IUniNode node)
        {
            return this.Errors.Any(it => it.Node == node);
        }*/
        public void AddErrorTranslation(ErrorCode fromCode, INode fromNode, ErrorCode toCode, INode toNode)
        {
            this.AddError(toCode, toNode);

            foreach (Error err in this.errors)
                if (err.Code == fromCode && err.Node == fromNode)
                {
                    this.errors.Remove(err);
                    return;
                }

            this.errorTranslations.Add(Tuple.Create(fromCode, fromNode));
        }
        public void AddError(ErrorCode code, INode node)
        {
            AddError(code, node, Enumerable.Empty<INode>());
        }
        public void AddError(ErrorCode code, INode node, IEnumerable<INode> context)
        {
            if (this.throwOnError)
                throw new Exception($"{code} {node}");

            // translation should be trigerred only once, in multiple cases investigate what is going on
            if (errorTranslations.Remove(Tuple.Create(code, node)))
                return;

            Error err = Error.Create(code, node, context);

            HashSet<ErrorCode> lower = this.errorPriority.GetLower(code).ToHashSet();
            if (lower.Any())
                this.errors = this.errors.Where(it => !it.SameNodeInvolved(err) || !lower.Contains(it.Code)).ToList();

            HashSet<ErrorCode> higher = this.errorPriority.GetHigher(code).ToHashSet();
            if (this.errors.Any(it => it.SameNodeInvolved(err) && higher.Contains(it.Code)))
                return;

            this.errors.Add(err);
        }

        public void AddError(ErrorCode code, INode node, INode context)
        {
            if (context == null)
                AddError(code, node, Enumerable.Empty<INode>());
            else
                AddError(code, node, new[] { context });
        }
    }
}
