﻿using FluentValidation;

namespace SayHello;

sealed class Request
{
    [FromHeader]
    public string Prefix { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    internal sealed class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.FirstName).MinimumLength(3);
            RuleFor(x => x.LastName).MinimumLength(5);
        }
    }
}