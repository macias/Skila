using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Extensions;
using Skila.Language.Flow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language
{
    public interface ILambdaTransfer : INode
    {
        void AddClosure(TypeDefinition closure);
    }

}