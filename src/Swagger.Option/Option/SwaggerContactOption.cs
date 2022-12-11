﻿using System;

namespace GGroupp.Infra;

public sealed record class SwaggerContactOption
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public Uri? Url { get; init; }
}