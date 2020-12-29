﻿using System;

namespace Parlot.Fluent
{
    public sealed class Deferred<T> : Parser<T>
    {
        public IParser<T> Parser { get; set; }

        public Deferred()
        {
        }

        public Deferred(Func<Deferred<T>, IParser<T>> parser)
        {
            Parser = parser(this);
        }

        public override bool Parse(Scanner scanner, ref ParseResult<T> result)
        {
            return Parser.Parse(scanner, ref result);
        }
    }
}
